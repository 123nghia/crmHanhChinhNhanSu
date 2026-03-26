using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Utility;

namespace crmHuman.Services
{
    public sealed class AttendanceRealtimeSyncService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IAttendanceBusiness _attendanceBusiness;
        private readonly ILogger<AttendanceRealtimeSyncService> _logger;
        private string? _lastRuntimeState;
        private bool? _lastDeviceReachable;
        private bool _hasLoggedSuccessfulSync;

        public AttendanceRealtimeSyncService(
            IConfiguration configuration,
            IAttendanceBusiness attendanceBusiness,
            ILogger<AttendanceRealtimeSyncService> logger)
        {
            _configuration = configuration;
            _attendanceBusiness = attendanceBusiness;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var options = GetOptions();
                if (!options.UseDirectSqlRealtime && !OperatingSystem.IsWindows())
                {
                    _logger.LogWarning("Attendance realtime sync only runs on Windows when the source is an Access database.");
                    return;
                }

                var interval = TimeSpan.FromSeconds(Math.Max(options.RealtimeSyncIntervalSeconds, 10));

                if (!options.RealtimeSyncEnabled)
                {
                    LogRuntimeStateOnce(
                        "disabled",
                        LogLevel.Information,
                        "Attendance realtime sync is disabled in configuration.");
                    await Task.Delay(interval, stoppingToken);
                    continue;
                }

                if (options.UseDirectSqlRealtime)
                {
                    if (string.IsNullOrWhiteSpace(options.DirectSqlConnectionString))
                    {
                        LogRuntimeStateOnce(
                            "missing-direct-sql-connection",
                            LogLevel.Warning,
                            "Attendance realtime sync skipped because AttendanceMachine:DirectSqlConnectionString is empty.");
                        await Task.Delay(interval, stoppingToken);
                        continue;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(options.DbSourceError))
                {
                    LogRuntimeStateOnce(
                        "mdb-source-error:" + options.DbSourceError,
                        LogLevel.Warning,
                        "Attendance realtime sync skipped because the MDB source could not be prepared. {Message}",
                        options.DbSourceError);
                    await Task.Delay(interval, stoppingToken);
                    continue;
                }
                else if (string.IsNullOrWhiteSpace(options.DbPath))
                {
                    LogRuntimeStateOnce(
                        "missing-db-path",
                        LogLevel.Warning,
                        "Attendance realtime sync skipped because AttendanceMachine:DbPath is empty.");
                    await Task.Delay(interval, stoppingToken);
                    continue;
                }

                if (!options.UseDirectSqlRealtime && !File.Exists(options.DbPath))
                {
                    LogRuntimeStateOnce(
                        "missing-db-file:" + (options.DbConfiguredSource ?? options.DbPath),
                        LogLevel.Warning,
                        "Attendance realtime sync skipped because the MDB file was not found: {DbPath}",
                        options.DbConfiguredSource ?? options.DbPath);
                    await Task.Delay(interval, stoppingToken);
                    continue;
                }

                await LogDeviceConnectivityAsync(options);
                await RunSyncAsync(options, stoppingToken);
                await Task.Delay(interval, stoppingToken);
            }
        }

        private async Task RunSyncAsync(AttendanceMachineOptions options, CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            try
            {
                var lookbackDays = Math.Max(options.RealtimeSyncLookbackDays, 1);
                var fromDate = DateTime.Today.AddDays(1 - lookbackDays);
                var toDate = DateTime.Today;
                var result = options.UseDirectSqlRealtime
                    ? await _attendanceBusiness.SyncFromDirectSqlAsync(fromDate, toDate, options.RealtimeSyncUserId)
                    : await _attendanceBusiness.SyncFromAccessAsync(fromDate, toDate, options.RealtimeSyncUserId);

                if (result.TotalError > 0)
                {
                    var firstError = result.Errors.FirstOrDefault()?.Content ?? "Unknown error";
                    LogRuntimeStateOnce(
                        "sync-error:" + firstError,
                        LogLevel.Warning,
                        "Attendance realtime sync returned {ErrorCount} error(s). First error: {FirstError}",
                        result.TotalError,
                        firstError);
                    return;
                }

                if (options.UseDirectSqlRealtime)
                {
                    LogRuntimeStateOnce(
                        "running-direct-sql",
                        LogLevel.Information,
                        "Attendance realtime sync is running from direct SQL. Device {DeviceIp}:{DevicePort}, interval {IntervalSeconds}s, lookback {LookbackDays} day(s).",
                        options.DeviceIp ?? "n/a",
                        options.DevicePort,
                        options.RealtimeSyncIntervalSeconds,
                        lookbackDays);
                }
                else
                {
                    LogRuntimeStateOnce(
                        "running-access",
                        LogLevel.Information,
                        "Attendance realtime sync is running from Access. Device {DeviceIp}:{DevicePort}, MDB {DbPath}, interval {IntervalSeconds}s, lookback {LookbackDays} day(s).",
                        options.DeviceIp ?? "n/a",
                        options.DevicePort,
                        options.DbConfiguredSource ?? options.DbSourceName ?? options.DbPath,
                        options.RealtimeSyncIntervalSeconds,
                        lookbackDays);
                }

                if (!_hasLoggedSuccessfulSync)
                {
                    _logger.LogInformation(
                        "Attendance realtime sync completed initial pass: processed {Total} attendance day(s), saved {Success} row(s).",
                        result.Total,
                        result.TotalSuccess);
                    _hasLoggedSuccessfulSync = true;
                }
                else
                {
                    _logger.LogDebug(
                        "Attendance realtime sync pass finished: processed {Total} attendance day(s), saved {Success} row(s).",
                        result.Total,
                        result.TotalSuccess);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogRuntimeStateOnce(
                    "sync-exception:" + ex.GetType().FullName,
                    LogLevel.Error,
                    "Attendance realtime sync crashed for the current cycle. {Message}",
                    ex.Message);
                _logger.LogError(ex, "Attendance realtime sync exception details.");
            }
        }

        private async Task LogDeviceConnectivityAsync(AttendanceMachineOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.DeviceIp))
            {
                return;
            }

            var isReachable = false;
            string message;

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(options.DeviceIp, 1000);
                isReachable = reply.Status == IPStatus.Success;
                message = isReachable
                    ? $"Attendance device {options.DeviceIp} responded to ping in {reply.RoundtripTime}ms."
                    : $"Attendance device {options.DeviceIp} did not respond to ping. Status={reply.Status}.";
            }
            catch (Exception ex)
            {
                message = $"Attendance device {options.DeviceIp} connectivity check failed: {ex.Message}";
            }

            if (_lastDeviceReachable == isReachable)
            {
                return;
            }

            _lastDeviceReachable = isReachable;
            if (isReachable)
            {
                _logger.LogInformation("{Message}", message);
            }
            else
            {
                _logger.LogWarning("{Message}", message);
            }
        }

        private AttendanceMachineOptions GetOptions()
        {
            var options = new AttendanceMachineOptions();
            _configuration.GetSection("AttendanceMachine").Bind(options);
            try
            {
                var resolvedSource = AttendanceMachinePathResolver.Resolve(options.DbUrl, options.DbPath);
                options.DbConfiguredSource = resolvedSource?.ConfiguredSource;
                options.DbSourceName = resolvedSource?.DisplayName;
                options.DbPath = resolvedSource?.LocalPath;
            }
            catch (Exception ex)
            {
                options.DbConfiguredSource = AttendanceMachinePathResolver.ResolveConfiguredSource(
                    options.DbUrl,
                    options.DbPath);
                options.DbSourceError = ex.Message;
                options.DbPath = null;
            }

            options.DbUrl = string.IsNullOrWhiteSpace(options.DbUrl)
                ? null
                : options.DbUrl.Trim();
            options.DeviceIp = string.IsNullOrWhiteSpace(options.DeviceIp)
                ? null
                : options.DeviceIp.Trim();
            options.DevicePort = options.DevicePort <= 0 ? 4370 : options.DevicePort;
            options.RealtimeSyncIntervalSeconds = options.RealtimeSyncIntervalSeconds <= 0
                ? 10
                : options.RealtimeSyncIntervalSeconds;
            options.RealtimeSyncLookbackDays = options.RealtimeSyncLookbackDays <= 0
                ? 2
                : options.RealtimeSyncLookbackDays;
            options.DirectSqlConnectionString = string.IsNullOrWhiteSpace(options.DirectSqlConnectionString)
                ? null
                : options.DirectSqlConnectionString.Trim();
            return options;
        }

        private void LogRuntimeStateOnce(string stateKey, LogLevel logLevel, string message, params object?[] args)
        {
            if (string.Equals(_lastRuntimeState, stateKey, StringComparison.Ordinal))
            {
                return;
            }

            _lastRuntimeState = stateKey;
            _logger.Log(logLevel, message, args);
        }
    }
}
