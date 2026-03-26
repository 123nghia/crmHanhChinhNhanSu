using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VS.Human.Utility
{
    public sealed class AttendanceMachinePathResolution
    {
        public string ConfiguredSource { get; init; } = string.Empty;
        public string LocalPath { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public bool IsRemote { get; init; }
    }

    public static class AttendanceMachinePathResolver
    {
        private static readonly HttpClient HttpClient = BuildHttpClient();
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> SourceLocks = new(StringComparer.OrdinalIgnoreCase);
        private static readonly TimeSpan RemoteCacheLifetime = TimeSpan.FromSeconds(5);

        public static string? ResolveConfiguredSource(params string?[] configuredSources)
        {
            if (configuredSources == null || configuredSources.Length == 0)
            {
                return null;
            }

            foreach (var source in configuredSources)
            {
                var normalizedSource = NormalizeSource(source);
                if (!string.IsNullOrWhiteSpace(normalizedSource))
                {
                    return normalizedSource;
                }
            }

            return null;
        }

        public static AttendanceMachinePathResolution? Resolve(params string?[] configuredSources)
        {
            return ResolveAsync(CancellationToken.None, configuredSources)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public static Task<AttendanceMachinePathResolution?> ResolveAsync(params string?[] configuredSources)
        {
            return ResolveAsync(CancellationToken.None, configuredSources);
        }

        public static async Task<AttendanceMachinePathResolution?> ResolveAsync(
            CancellationToken cancellationToken,
            params string?[] configuredSources)
        {
            var configuredSource = ResolveConfiguredSource(configuredSources);
            if (string.IsNullOrWhiteSpace(configuredSource))
            {
                return null;
            }

            if (TryResolveUri(configuredSource, out var uri))
            {
                if (uri!.IsFile)
                {
                    var localPath = Path.GetFullPath(uri.LocalPath);
                    return new AttendanceMachinePathResolution
                    {
                        ConfiguredSource = configuredSource,
                        LocalPath = localPath,
                        DisplayName = GetDisplayName(configuredSource, uri, localPath),
                        IsRemote = false
                    };
                }

                var downloadedPath = await DownloadRemoteFileAsync(configuredSource, uri, cancellationToken)
                    .ConfigureAwait(false);
                return new AttendanceMachinePathResolution
                {
                    ConfiguredSource = configuredSource,
                    LocalPath = downloadedPath,
                    DisplayName = GetDisplayName(configuredSource, uri, downloadedPath),
                    IsRemote = true
                };
            }

            var resolvedPath = ResolveLocalPath(configuredSource);
            if (string.IsNullOrWhiteSpace(resolvedPath))
            {
                return null;
            }

            return new AttendanceMachinePathResolution
            {
                ConfiguredSource = configuredSource,
                LocalPath = resolvedPath,
                DisplayName = GetDisplayName(configuredSource, null, resolvedPath),
                IsRemote = false
            };
        }

        public static string? ResolveDbPath(string? configuredPath)
        {
            return Resolve(configuredPath)?.LocalPath;
        }

        private static string? ResolveLocalPath(string? configuredPath)
        {
            var normalizedPath = NormalizeSource(configuredPath);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return null;
            }

            if (Path.IsPathRooted(normalizedPath))
            {
                return Path.GetFullPath(normalizedPath);
            }

            var candidateDirectories = BuildCandidateDirectories();
            foreach (var directory in candidateDirectories)
            {
                var candidatePath = Path.GetFullPath(Path.Combine(directory, normalizedPath));
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), normalizedPath));
        }

        private static string? NormalizeSource(string? configuredSource)
        {
            if (string.IsNullOrWhiteSpace(configuredSource))
            {
                return null;
            }

            var normalizedSource = configuredSource.Trim().Trim('"');
            return string.IsNullOrWhiteSpace(normalizedSource) ? null : normalizedSource;
        }

        private static bool TryResolveUri(string configuredSource, out Uri? uri)
        {
            uri = null;
            if (Path.IsPathRooted(configuredSource))
            {
                return false;
            }

            if (!Uri.TryCreate(configuredSource, UriKind.Absolute, out var parsedUri))
            {
                return false;
            }

            if (parsedUri.IsFile ||
                parsedUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                parsedUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                uri = parsedUri;
                return true;
            }

            if (configuredSource.Contains("://", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Khong ho tro giao thuc URL '{parsedUri.Scheme}' cho file cham cong.");
            }

            return false;
        }

        private static async Task<string> DownloadRemoteFileAsync(
            string configuredSource,
            Uri uri,
            CancellationToken cancellationToken)
        {
            var cacheDirectory = GetRemoteCacheDirectory();
            Directory.CreateDirectory(cacheDirectory);

            var cachePrefix = CreateSourceHash(configuredSource) + "_";
            var cachedPath = GetLatestFreshCachePath(cacheDirectory, cachePrefix);
            if (!string.IsNullOrWhiteSpace(cachedPath))
            {
                return cachedPath;
            }

            var sourceLock = SourceLocks.GetOrAdd(
                configuredSource,
                _ => new SemaphoreSlim(1, 1));

            await sourceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cachedPath = GetLatestFreshCachePath(cacheDirectory, cachePrefix);
                if (!string.IsNullOrWhiteSpace(cachedPath))
                {
                    return cachedPath;
                }

                var fileName = SanitizeFileName(GetFileNameForSource(uri));
                var targetPath = Path.Combine(
                    cacheDirectory,
                    $"{cachePrefix}{DateTime.UtcNow:yyyyMMddHHmmssfff}_{fileName}");
                var tempPath = targetPath + ".download";

                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    using var response = await HttpClient.SendAsync(
                            request,
                            HttpCompletionOption.ResponseHeadersRead,
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(
                            $"Khong the tai file cham cong tu URL '{uri.GetLeftPart(UriPartial.Path)}'. " +
                            $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                    }

                    await using (var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken)
                                       .ConfigureAwait(false))
                    await using (var targetStream = new FileStream(
                                       tempPath,
                                       FileMode.Create,
                                       FileAccess.Write,
                                       FileShare.None))
                    {
                        await sourceStream.CopyToAsync(targetStream, cancellationToken).ConfigureAwait(false);
                    }

                    File.Move(tempPath, targetPath);
                    CleanupStaleCacheFiles(cacheDirectory, cachePrefix);
                    return targetPath;
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new InvalidOperationException(
                        $"Het thoi gian tai file cham cong tu URL '{uri.GetLeftPart(UriPartial.Path)}'.",
                        ex);
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Khong the tai file cham cong tu URL '{uri.GetLeftPart(UriPartial.Path)}'. {ex.Message}",
                        ex);
                }
                finally
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
            }
            finally
            {
                sourceLock.Release();
            }
        }

        private static string GetRemoteCacheDirectory()
        {
            return Path.Combine(Path.GetTempPath(), "crmHuman", "attendance-mdb-cache");
        }

        private static string? GetLatestFreshCachePath(string cacheDirectory, string cachePrefix)
        {
            if (!Directory.Exists(cacheDirectory))
            {
                return null;
            }

            var directory = new DirectoryInfo(cacheDirectory);
            var latestFile = directory
                .GetFiles(cachePrefix + "*")
                .Where(file => !file.Extension.Equals(".download", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latestFile == null)
            {
                return null;
            }

            var age = DateTime.UtcNow - latestFile.LastWriteTimeUtc;
            return age <= RemoteCacheLifetime ? latestFile.FullName : null;
        }

        private static void CleanupStaleCacheFiles(string cacheDirectory, string cachePrefix)
        {
            if (!Directory.Exists(cacheDirectory))
            {
                return;
            }

            var directory = new DirectoryInfo(cacheDirectory);
            var files = directory
                .GetFiles(cachePrefix + "*")
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ToList();

            for (var i = 2; i < files.Count; i++)
            {
                try
                {
                    files[i].Delete();
                }
                catch
                {
                }
            }
        }

        private static string CreateSourceHash(string configuredSource)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(configuredSource));
            return Convert.ToHexString(bytes).ToLowerInvariant()[..16];
        }

        private static string GetFileNameForSource(Uri uri)
        {
            var fileName = uri.IsFile
                ? Path.GetFileName(uri.LocalPath)
                : Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath));

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "WiseEyeOn39.mdb";
            }

            return Path.HasExtension(fileName) ? fileName : fileName + ".mdb";
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(fileName.Length);

            foreach (var ch in fileName)
            {
                builder.Append(invalidChars.Contains(ch) ? '_' : ch);
            }

            return builder.ToString();
        }

        private static string GetDisplayName(string configuredSource, Uri? uri, string localPath)
        {
            if (uri != null)
            {
                var uriFileName = GetFileNameForSource(uri);
                if (!string.IsNullOrWhiteSpace(uriFileName))
                {
                    return uriFileName;
                }
            }

            var pathFileName = Path.GetFileName(localPath);
            return string.IsNullOrWhiteSpace(pathFileName) ? configuredSource : pathFileName;
        }

        private static IEnumerable<string> BuildCandidateDirectories()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var baseDirectory = AppContext.BaseDirectory;

            var rawCandidates = new[]
            {
                currentDirectory,
                baseDirectory,
                Path.Combine(currentDirectory, ".."),
                Path.Combine(baseDirectory, ".."),
                Path.Combine(baseDirectory, "..", ".."),
                Path.Combine(baseDirectory, "..", "..", "..")
            };

            return rawCandidates
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static HttpClient BuildHttpClient()
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }
    }
}
