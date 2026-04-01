using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
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
        private DateTime? _lastDailyEvaluationDate;
        private bool _historicalSyncCompleted;

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
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var options = GetOptions();
                    if (!options.UseDirectSqlRealtime &&
                        !options.UseDirectDeviceRealtime &&
                        !OperatingSystem.IsWindows())
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
                        await DelayWithDailyEvaluationAsync(interval, options, stoppingToken);
                        continue;
                    }

                    if (options.UseDirectDeviceRealtime)
                    {
                        if (string.IsNullOrWhiteSpace(options.DeviceIp))
                        {
                            LogRuntimeStateOnce(
                                "missing-device-ip",
                                LogLevel.Warning,
                                "Attendance realtime sync skipped because AttendanceMachine:DeviceIp is empty.");
                            await DelayWithDailyEvaluationAsync(interval, options, stoppingToken);
                            continue;
                        }
                    }
                    else if (options.UseDirectSqlRealtime)
                    {
                        if (string.IsNullOrWhiteSpace(options.DirectSqlConnectionString))
                        {
                            LogRuntimeStateOnce(
                                "missing-direct-sql-connection",
                                LogLevel.Warning,
                                "Attendance realtime sync skipped because AttendanceMachine:DirectSqlConnectionString is empty.");
                            await DelayWithDailyEvaluationAsync(interval, options, stoppingToken);
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
                        await DelayWithDailyEvaluationAsync(interval, options, stoppingToken);
                        continue;
                    }
                    else if (string.IsNullOrWhiteSpace(options.DbPath))
                    {
                        LogRuntimeStateOnce(
                            "missing-db-path",
                            LogLevel.Warning,
                            "Attendance realtime sync skipped because AttendanceMachine:DbPath is empty.");
                        await DelayWithDailyEvaluationAsync(interval, options, stoppingToken);
                        continue;
                    }

                    if (!options.UseDirectDeviceRealtime &&
                        !options.UseDirectSqlRealtime &&
                        !File.Exists(options.DbPath))
                    {
                        LogRuntimeStateOnce(
                            "missing-db-file:" + (options.DbConfiguredSource ?? options.DbPath),
                            LogLevel.Warning,
                            "Attendance realtime sync skipped because the MDB file was not found: {DbPath}",
                            options.DbConfiguredSource ?? options.DbPath);
                        await DelayWithDailyEvaluationAsync(interval, options, stoppingToken);
                        continue;
                    }

                    await LogDeviceConnectivityAsync(options);
                    await RunSyncAsync(options, stoppingToken);
                    await RunHistoricalSyncAsync(options, stoppingToken);
                    await DelayWithDailyEvaluationAsync(interval, options, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
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
                var result = await SyncRangeAsync(options, fromDate, toDate, stoppingToken);

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

                await _attendanceBusiness.EvaluateAttendanceRangeAsync(fromDate, toDate, options.RealtimeSyncUserId);

                if (options.UseDirectDeviceRealtime)
                {
                    LogRuntimeStateOnce(
                        "running-direct-device",
                        LogLevel.Information,
                        "Attendance realtime sync is running directly from device TCP/IP. Device {DeviceIp}:{DevicePort}, interval {IntervalSeconds}s, lookback {LookbackDays} day(s).",
                        options.DeviceIp ?? "n/a",
                        options.DevicePort,
                        options.RealtimeSyncIntervalSeconds,
                        lookbackDays);
                }
                else if (options.UseDirectSqlRealtime)
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

        private async Task<AttendanceImportResult> SyncRangeAsync(
            AttendanceMachineOptions options,
            DateTime fromDate,
            DateTime toDate,
            CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            return options.UseDirectDeviceRealtime
                ? await _attendanceBusiness.SyncFromDeviceAsync(fromDate, toDate, options.RealtimeSyncUserId)
                : options.UseDirectSqlRealtime
                    ? await _attendanceBusiness.SyncFromDirectSqlAsync(fromDate, toDate, options.RealtimeSyncUserId)
                    : await _attendanceBusiness.SyncFromAccessAsync(fromDate, toDate, options.RealtimeSyncUserId);
        }

        private async Task DelayWithDailyEvaluationAsync(
            TimeSpan interval,
            AttendanceMachineOptions options,
            CancellationToken stoppingToken)
        {
            await RunDailyEvaluationAsync(options, stoppingToken);
            await Task.Delay(interval, stoppingToken);
        }

        private async Task RunDailyEvaluationAsync(
            AttendanceMachineOptions options,
            CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            if (!options.DailyEvaluationEnabled)
            {
                return;
            }

            var evaluationHour = options.DailyEvaluationHour;
            if (evaluationHour < 0 || evaluationHour > 23)
            {
                evaluationHour = 21;
            }

            var now = DateTime.Now;
            if (now.Hour < evaluationHour)
            {
                return;
            }

            var targetDate = now.Date;
            if (_lastDailyEvaluationDate.HasValue && _lastDailyEvaluationDate.Value == targetDate)
            {
                return;
            }

            try
            {
                var syncResult = await SyncRangeAsync(
                    options,
                    targetDate,
                    targetDate,
                    stoppingToken);

                if (syncResult.TotalError > 0)
                {
                    var firstError = syncResult.Errors.FirstOrDefault()?.Content ?? "Unknown error";
                    _logger.LogWarning(
                        "Attendance daily sync before finalization returned {ErrorCount} error(s) for {WorkDate:yyyy-MM-dd}. First error: {FirstError}",
                        syncResult.TotalError,
                        targetDate,
                        firstError);
                    return;
                }

                var updatedCount = await _attendanceBusiness.EvaluateAttendanceRangeAsync(
                    targetDate,
                    targetDate,
                    options.RealtimeSyncUserId);
                _lastDailyEvaluationDate = targetDate;

                _logger.LogInformation(
                    "Attendance daily finalization completed {WorkDate:yyyy-MM-dd} at hour {Hour}. Synced {SyncedTotal} day record(s), saved {SyncedSuccess} row(s), updated {UpdatedCount} evaluation row(s).",
                    targetDate,
                    evaluationHour,
                    syncResult.Total,
                    syncResult.TotalSuccess,
                    updatedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Attendance daily evaluation failed for {WorkDate:yyyy-MM-dd}.",
                    targetDate);
            }
        }

        private async Task RunHistoricalSyncAsync(
            AttendanceMachineOptions options,
            CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            if (!options.HistoricalSyncEnabled || _historicalSyncCompleted)
            {
                return;
            }

            var startDate = (options.HistoricalSyncFromDate ?? new DateTime(2000, 1, 1)).Date;
            var endDate = DateTime.Today;
            if (startDate > endDate)
            {
                _historicalSyncCompleted = true;
                return;
            }

            var syncKey = BuildHistoricalSyncKey(options, startDate);
            var syncState = await GetSyncStateAsync(syncKey, stoppingToken);
            if (syncState?.IsCompleted == true)
            {
                _historicalSyncCompleted = true;
                return;
            }

            try
            {
                await SaveSyncStateAsync(
                    syncKey,
                    startDate,
                    endDate,
                    false,
                    "Historical attendance sync is running.",
                    stoppingToken);

                var result = await SyncRangeAsync(options, startDate, endDate, stoppingToken);
                if (result.TotalError > 0)
                {
                    var firstError = result.Errors.FirstOrDefault()?.Content ?? "Unknown error";
                    await SaveSyncStateAsync(
                        syncKey,
                        startDate,
                        endDate,
                        false,
                        "Historical attendance sync failed: " + firstError,
                        stoppingToken);
                    _logger.LogWarning(
                        "Attendance historical sync returned {ErrorCount} error(s). First error: {FirstError}",
                        result.TotalError,
                        firstError);
                    return;
                }

                await SaveSyncStateAsync(
                    syncKey,
                    startDate,
                    endDate,
                    true,
                    $"Historical attendance sync completed. Synced {result.Total} row(s), saved {result.TotalSuccess} row(s).",
                    stoppingToken);

                _historicalSyncCompleted = true;
                _logger.LogInformation(
                    "Attendance historical sync completed for range {FromDate:yyyy-MM-dd} -> {ToDate:yyyy-MM-dd}. Synced {Total} row(s), saved {Success} row(s).",
                    startDate,
                    endDate,
                    result.Total,
                    result.TotalSuccess);
            }
            catch (Exception ex)
            {
                await SaveSyncStateAsync(
                    syncKey,
                    startDate,
                    endDate,
                    false,
                    "Historical attendance sync crashed: " + ex.Message,
                    stoppingToken);
                _logger.LogError(
                    ex,
                    "Attendance historical sync failed for range {FromDate:yyyy-MM-dd} -> {ToDate:yyyy-MM-dd}.",
                    startDate,
                    endDate);
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
            options.DeviceTimeoutSeconds = options.DeviceTimeoutSeconds <= 0
                ? 120
                : options.DeviceTimeoutSeconds;
            options.RealtimeSyncIntervalSeconds = options.RealtimeSyncIntervalSeconds <= 0
                ? 10
                : options.RealtimeSyncIntervalSeconds;
            options.RealtimeSyncLookbackDays = options.RealtimeSyncLookbackDays <= 0
                ? 2
                : options.RealtimeSyncLookbackDays;
            options.DailyEvaluationHour = options.DailyEvaluationHour < 0 || options.DailyEvaluationHour > 23
                ? 21
                : options.DailyEvaluationHour;
            options.HistoricalSyncFromDate = options.HistoricalSyncFromDate?.Date;
            options.DirectSqlConnectionString = string.IsNullOrWhiteSpace(options.DirectSqlConnectionString)
                ? null
                : options.DirectSqlConnectionString.Trim();
            return options;
        }

        private async Task<AttendanceSyncState?> GetSyncStateAsync(string syncKey, CancellationToken stoppingToken)
        {
            var connectionString = GetApplicationConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(stoppingToken);
            await using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT TOP 1
    SyncKey,
    RangeStartDate,
    RangeEndDate,
    IsCompleted,
    LastRunAt,
    LastSuccessAt,
    LastMessage
FROM dbo.AttendanceSyncStates
WHERE SyncKey = @SyncKey;";
            command.Parameters.AddWithValue("@SyncKey", syncKey);

            await using var reader = await command.ExecuteReaderAsync(stoppingToken);
            if (!await reader.ReadAsync(stoppingToken))
            {
                return null;
            }

            return new AttendanceSyncState
            {
                SyncKey = reader["SyncKey"]?.ToString() ?? string.Empty,
                RangeStartDate = reader["RangeStartDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["RangeStartDate"]).Date,
                RangeEndDate = reader["RangeEndDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["RangeEndDate"]).Date,
                IsCompleted = reader["IsCompleted"] != DBNull.Value && Convert.ToBoolean(reader["IsCompleted"]),
                LastRunAt = reader["LastRunAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["LastRunAt"]),
                LastSuccessAt = reader["LastSuccessAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["LastSuccessAt"]),
                LastMessage = reader["LastMessage"]?.ToString()
            };
        }

        private async Task SaveSyncStateAsync(
            string syncKey,
            DateTime startDate,
            DateTime endDate,
            bool isCompleted,
            string message,
            CancellationToken stoppingToken)
        {
            var connectionString = GetApplicationConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(stoppingToken);
            await using var command = connection.CreateCommand();
            command.CommandText = @"
IF EXISTS (SELECT 1 FROM dbo.AttendanceSyncStates WHERE SyncKey = @SyncKey)
BEGIN
    UPDATE dbo.AttendanceSyncStates
    SET RangeStartDate = @RangeStartDate,
        RangeEndDate = @RangeEndDate,
        IsCompleted = @IsCompleted,
        LastRunAt = GETDATE(),
        LastSuccessAt = CASE WHEN @IsCompleted = 1 THEN GETDATE() ELSE LastSuccessAt END,
        LastMessage = @LastMessage,
        UpdateAt = GETDATE()
    WHERE SyncKey = @SyncKey;
END
ELSE
BEGIN
    INSERT INTO dbo.AttendanceSyncStates
    (
        SyncKey,
        RangeStartDate,
        RangeEndDate,
        IsCompleted,
        LastRunAt,
        LastSuccessAt,
        LastMessage,
        CreateAt,
        UpdateAt
    )
    VALUES
    (
        @SyncKey,
        @RangeStartDate,
        @RangeEndDate,
        @IsCompleted,
        GETDATE(),
        CASE WHEN @IsCompleted = 1 THEN GETDATE() ELSE NULL END,
        @LastMessage,
        GETDATE(),
        GETDATE()
    );
END";
            command.Parameters.AddWithValue("@SyncKey", syncKey);
            command.Parameters.AddWithValue("@RangeStartDate", startDate);
            command.Parameters.AddWithValue("@RangeEndDate", endDate);
            command.Parameters.AddWithValue("@IsCompleted", isCompleted);
            command.Parameters.AddWithValue("@LastMessage", message);
            await command.ExecuteNonQueryAsync(stoppingToken);
        }

        private string? GetApplicationConnectionString()
        {
            var connectionString = _configuration.GetConnectionString("stringConnect7");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString.Trim();
            }

            connectionString = _configuration.GetConnectionString("stringConnect");
            return string.IsNullOrWhiteSpace(connectionString) ? null : connectionString.Trim();
        }

        private static string BuildHistoricalSyncKey(AttendanceMachineOptions options, DateTime startDate)
        {
            var sourceKey = options.UseDirectDeviceRealtime
                ? $"device:{options.DeviceIp ?? "unknown"}:{options.DevicePort}"
                : options.UseDirectSqlRealtime
                    ? "direct-sql"
                    : "access";
            return $"attendance-historical-sync:{sourceKey}:{startDate:yyyyMMdd}";
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

        private sealed class AttendanceSyncState
        {
            public string SyncKey { get; set; } = string.Empty;
            public DateTime? RangeStartDate { get; set; }
            public DateTime? RangeEndDate { get; set; }
            public bool IsCompleted { get; set; }
            public DateTime? LastRunAt { get; set; }
            public DateTime? LastSuccessAt { get; set; }
            public string? LastMessage { get; set; }
        }
    }
}
