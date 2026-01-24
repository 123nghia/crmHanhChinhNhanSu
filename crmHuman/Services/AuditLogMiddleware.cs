using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VS.Human.Business;

namespace crmHuman.Services
{
    public class AuditLogMiddleware
    {
        private const int MaxPayloadLength = 4096;
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLogMiddleware> _logger;

        public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogBusiness auditLogBusiness)
        {
            if (!ShouldLog(context.Request))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            string? payload = await ReadPayloadAsync(context.Request);

            Exception? exception = null;
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                await WriteAuditLogAsync(context, auditLogBusiness, payload, stopwatch.ElapsedMilliseconds, exception);
            }
        }

        private async Task WriteAuditLogAsync(
            HttpContext context,
            IAuditLogBusiness auditLogBusiness,
            string? payload,
            long durationMs,
            Exception? exception)
        {
            try
            {
                var identity = context.User?.Identity as ClaimsIdentity;
                var idUser = identity?.Claims.FirstOrDefault(o => o.Type == "userId")?.Value;
                var userName = identity?.Claims.FirstOrDefault(o => o.Type == "UserName")?.Value;
                var roleCode = identity?.Claims.FirstOrDefault(o => o.Type == "RoleCode")?.Value;
                var fullName = identity?.Claims.FirstOrDefault(o => o.Type == "FullName")?.Value;

                int? userId = null;
                if (!string.IsNullOrWhiteSpace(idUser) && int.TryParse(idUser, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                var request = context.Request;
                var statusCode = exception != null ? StatusCodes.Status500InternalServerError : context.Response?.StatusCode;

                var clientIp = context.Connection.RemoteIpAddress?.ToString();
                var userAgent = request.Headers["User-Agent"].ToString();

                await auditLogBusiness.LogAsync(
                    userId,
                    userName,
                    fullName,
                    roleCode,
                    request.Method ?? string.Empty,
                    Truncate(request.Path.HasValue ? request.Path.Value! : string.Empty, 500),
                    request.QueryString.HasValue ? Truncate(request.QueryString.Value, 1000) : null,
                    payload,
                    statusCode,
                    (int)durationMs,
                    string.IsNullOrWhiteSpace(clientIp) ? null : Truncate(clientIp, 50),
                    string.IsNullOrWhiteSpace(userAgent) ? null : Truncate(userAgent, 255)
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit log failed");
            }
        }

        private static bool ShouldLog(HttpRequest request)
        {
            var method = request.Method?.ToUpperInvariant() ?? string.Empty;
            if (method == "HEAD" || method == "OPTIONS")
            {
                return false;
            }

            var path = request.Path.HasValue ? request.Path.Value! : string.Empty;
            if (path.StartsWith("/assets", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static async Task<string?> ReadPayloadAsync(HttpRequest request)
        {
            try
            {
                if (request.ContentLength.HasValue && request.ContentLength.Value > MaxPayloadLength)
                {
                    return $"[Payload omitted: {request.ContentLength.Value} bytes]";
                }

                request.EnableBuffering();

                var contentType = request.ContentType ?? string.Empty;

                if (contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                {
                    request.Body.Position = 0;
                    return "[Payload omitted: multipart]";
                }

                if (request.HasFormContentType)
                {
                    var form = await request.ReadFormAsync();
                    var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var key in form.Keys)
                    {
                        if (IsSensitiveKey(key))
                        {
                            data[key] = "***";
                        }
                        else
                        {
                            data[key] = form[key].ToString();
                        }
                    }

                    request.Body.Position = 0;
                    return JsonSerializer.Serialize(data);
                }

                if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)
                    || contentType.Contains("text/json", StringComparison.OrdinalIgnoreCase)
                    || contentType.Contains("application/xml", StringComparison.OrdinalIgnoreCase)
                    || contentType.Contains("text/plain", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    request.Body.Position = 0;
                    if (string.IsNullOrWhiteSpace(body))
                    {
                        return null;
                    }
                    if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase) && ContainsSensitiveJson(body))
                    {
                        return "[Payload omitted: sensitive]";
                    }
                    return Truncate(body, MaxPayloadLength);
                }

                request.Body.Position = 0;
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsSensitiveKey(string key)
        {
            var normalized = key.ToLowerInvariant();
            return normalized.Contains("password")
                || normalized.Contains("pass")
                || normalized.Contains("pwd")
                || normalized.Contains("token")
                || normalized.Contains("secret");
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength) + "...";
        }

        private static bool ContainsSensitiveJson(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return false;
            }

            return Regex.IsMatch(body, "\"(password|pass|pwd|token|secret)\"\\s*:", RegexOptions.IgnoreCase);
        }
    }
}
