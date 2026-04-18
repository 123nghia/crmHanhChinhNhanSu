using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        private readonly SemaphoreSlim _scheduledSyncLock = new(1, 1);
        private readonly SemaphoreSlim _dailyFinalizationLock = new(1, 1);
        private readonly SemaphoreSlim _monthlyResyncLock = new(1, 1);
        private string? _lastRuntimeState;
        private bool? _lastDeviceReachable;
        private bool _hasLoggedSuccessfulSync;
        private DateTime? _lastDailyEvaluationDate;
        private DateTime? _lastScheduledSyncSlot;
        private DateTime? _lastMonthlyResyncMonth;
        private int _consecutiveSyncFailureCount;
        private bool _historicalSyncCompleted;
        private static readonly TimeZoneInfo AttendanceTimeZone = ResolveAttendanceTimeZone();
        private static readonly TimeSpan[] SyncRetryDelays =
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30)
        };
        private const int DefaultLookbackDays = 30;
        private const int DefaultPollIntervalSeconds = 60;
        private const string DefaultSyncTimes = "12:30,20:30";
        private const string DefaultDailyEvaluationTime = "21:00";
        private const string DefaultMonthlyResyncTime = "01:00";

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
                    var pollInterval = TimeSpan.FromSeconds(Math.Max(options.SchedulerPollIntervalSeconds, 30));

                    if (!options.RealtimeSyncEnabled)
                    {
                        LogRuntimeStateOnce(
                            "disabled",
                            LogLevel.Information,
                            "Attendance realtime sync is disabled in configuration.");
                        await DelayWithDailyEvaluationAsync(pollInterval, options, stoppingToken);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(options.DeviceIp))
                    {
                        LogRuntimeStateOnce(
                            "missing-device-ip",
                            LogLevel.Warning,
                            "Attendance realtime sync skipped because AttendanceMachine:DeviceIp is empty.");
                        await DelayWithDailyEvaluationAsync(pollInterval, options, stoppingToken);
                        continue;
                    }

                    await LogDeviceConnectivityAsync(options);
                    await RunHistoricalSyncAsync(options, stoppingToken);
                    await RunMonthlyResyncAsync(options, stoppingToken);
                    await RunScheduledSyncAsync(options, stoppingToken);
                    await DelayWithDailyEvaluationAsync(pollInterval, options, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }

        private async Task<bool> RunSyncAsync(AttendanceMachineOptions options, CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            try
            {
                var now = GetAttendanceNow();
                var lookbackDays = Math.Max(options.RealtimeSyncLookbackDays, 1);
                var fromDate = now.Date.AddDays(1 - lookbackDays);
                var toDate = now.Date;
                var result = await SyncRangeWithRetryAsync(
                    options,
                    fromDate,
                    toDate,
                    stoppingToken,
                    evaluateAttendance: false);

                if (result.TotalError > 0)
                {
                    var firstError = result.Errors.FirstOrDefault()?.Content ?? "Unknown error";
                    LogRuntimeStateOnce(
                        "sync-error:" + firstError,
                        LogLevel.Warning,
                        "Attendance realtime sync returned {ErrorCount} error(s). First error: {FirstError}",
                        result.TotalError,
                        firstError);
                    return false;
                }

                await RunDirtyDateRecalculationsAsync(result.ChangedWorkDates, options, stoppingToken);

                LogRuntimeStateOnce(
                    "running-direct-device",
                    LogLevel.Information,
                    "Attendance realtime sync is running directly from device TCP/IP. Device {DeviceIp}:{DevicePort}, schedule {SyncTimes}, lookback {LookbackDays} day(s).",
                    options.DeviceIp ?? "n/a",
                    options.DevicePort,
                    options.RealtimeSyncTimes,
                    lookbackDays);

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

                return true;
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
                return false;
            }
        }

        private async Task RunScheduledSyncAsync(
            AttendanceMachineOptions options,
            CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var now = GetAttendanceNow();
            var scheduledSlot = GetLatestDueScheduledSyncSlot(options, now);
            if (!scheduledSlot.HasValue)
            {
                return;
            }

            if (_lastScheduledSyncSlot.HasValue && _lastScheduledSyncSlot.Value == scheduledSlot.Value)
            {
                return;
            }

            if (!await TryEnterJobLockAsync(_scheduledSyncLock, "Scheduled sync", stoppingToken))
            {
                return;
            }

            AttendanceDistributedLock? distributedLock = null;
            try
            {
                distributedLock = await TryAcquireDistributedJobLockAsync("attendance:scheduled-sync", stoppingToken);
                if (distributedLock == null)
                {
                    return;
                }

                if (_lastScheduledSyncSlot.HasValue && _lastScheduledSyncSlot.Value == scheduledSlot.Value)
                {
                    return;
                }

                var syncKey = BuildScheduledSyncKey(options, scheduledSlot.Value);
                var syncState = await GetSyncStateAsync(syncKey, stoppingToken);
                if (syncState?.IsCompleted == true)
                {
                    _lastScheduledSyncSlot = scheduledSlot.Value;
                    return;
                }

                var lookbackDays = Math.Max(options.RealtimeSyncLookbackDays, 1);
                var fromDate = now.Date.AddDays(1 - lookbackDays);
                var toDate = now.Date;

                await SaveSyncStateAsync(
                    syncKey,
                    fromDate,
                    toDate,
                    false,
                    $"Scheduled attendance sync is running for slot {scheduledSlot.Value:yyyy-MM-dd HH:mm}.",
                    stoppingToken);

                _logger.LogInformation(
                    "[Attendance] Scheduled sync started (range={FromDate} -> {ToDate})",
                    fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

                var stopwatch = Stopwatch.StartNew();
                var jobLogId = await BeginJobLogAsync("ScheduledSync", fromDate, toDate, stoppingToken);
                var completed = await RunSyncAsync(options, stoppingToken);
                if (!completed)
                {
                    stopwatch.Stop();
                    await CompleteJobLogAsync(jobLogId, "Failed", stopwatch.ElapsedMilliseconds, "Scheduled sync failed.", stoppingToken);
                    await SaveSyncStateAsync(
                        syncKey,
                        fromDate,
                        toDate,
                        false,
                        $"Scheduled attendance sync failed for slot {scheduledSlot.Value:yyyy-MM-dd HH:mm}.",
                        stoppingToken);
                    return;
                }

                stopwatch.Stop();
                await SaveSyncStateAsync(
                    syncKey,
                    fromDate,
                    toDate,
                    true,
                    $"Scheduled attendance sync completed for slot {scheduledSlot.Value:yyyy-MM-dd HH:mm}.",
                    stoppingToken);
                _lastScheduledSyncSlot = scheduledSlot.Value;
                await CompleteJobLogAsync(jobLogId, "Completed", stopwatch.ElapsedMilliseconds, "Scheduled sync completed.", stoppingToken);

                _logger.LogInformation(
                    "[Attendance] Scheduled sync completed");
            }
            finally
            {
                if (distributedLock != null)
                {
                    await distributedLock.DisposeAsync();
                }

                _scheduledSyncLock.Release();
            }
        }

        private async Task<AttendanceImportResult> SyncRangeAsync(
            AttendanceMachineOptions options,
            DateTime fromDate,
            DateTime toDate,
            CancellationToken stoppingToken,
            bool evaluateAttendance = true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            return await _attendanceBusiness.SyncFromDeviceAsync(
                fromDate,
                toDate,
                options.RealtimeSyncUserId,
                evaluateAttendance);
        }

        private async Task<AttendanceImportResult> SyncRangeWithRetryAsync(
            AttendanceMachineOptions options,
            DateTime fromDate,
            DateTime toDate,
            CancellationToken stoppingToken,
            bool evaluateAttendance = true)
        {
            AttendanceImportResult? lastResult = null;
            Exception? lastException = null;
            var maxAttempts = SyncRetryDelays.Length + 1;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    var result = await SyncRangeAsync(
                        options,
                        fromDate,
                        toDate,
                        stoppingToken,
                        evaluateAttendance);

                    if (result.TotalError == 0)
                    {
                        _consecutiveSyncFailureCount = 0;
                        return result;
                    }

                    lastResult = result;
                    lastException = null;
                    var firstError = result.Errors.FirstOrDefault()?.Content ?? "Unknown error";
                    _logger.LogWarning(
                        "[Attendance] Sync attempt {Attempt}/{MaxAttempts} failed for range {FromDate} -> {ToDate}. First error: {FirstError}",
                        attempt,
                        maxAttempts,
                        fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        firstError);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(
                        ex,
                        "[Attendance] Sync attempt {Attempt}/{MaxAttempts} crashed for range {FromDate} -> {ToDate}.",
                        attempt,
                        maxAttempts,
                        fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }

                if (attempt <= SyncRetryDelays.Length)
                {
                    await Task.Delay(SyncRetryDelays[attempt - 1], stoppingToken);
                }
            }

            _consecutiveSyncFailureCount++;
            _logger.LogWarning(
                "[Attendance] Sync skipped after {Attempts} failed attempt(s). Consecutive failure count={FailureCount}.",
                maxAttempts,
                _consecutiveSyncFailureCount);

            if (lastResult != null)
            {
                return lastResult;
            }

            var errorResult = new AttendanceImportResult
            {
                TotalError = 1
            };
            errorResult.Errors.Add(new AttendanceImportError
            {
                Row = 0,
                Content = lastException?.Message ?? "Unknown sync failure."
            });
            return errorResult;
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

            var now = GetAttendanceNow();
            var evaluationTime = GetDailyEvaluationTime(options);
            if (now.TimeOfDay < evaluationTime)
            {
                return;
            }

            var targetDate = now.Date;
            if (_lastDailyEvaluationDate.HasValue && _lastDailyEvaluationDate.Value == targetDate)
            {
                return;
            }

            if (await IsAttendanceDateLockedAsync(targetDate, stoppingToken))
            {
                _logger.LogInformation(
                    "[Attendance] Daily finalization skipped because date is locked (date={Date})",
                    targetDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                _lastDailyEvaluationDate = targetDate;
                return;
            }

            if (!await TryEnterJobLockAsync(_dailyFinalizationLock, "Daily finalization", stoppingToken))
            {
                return;
            }

            AttendanceDistributedLock? distributedLock = null;
            try
            {
                distributedLock = await TryAcquireDistributedJobLockAsync("attendance:daily-finalization", stoppingToken);
                if (distributedLock == null)
                {
                    return;
                }

                if (_lastDailyEvaluationDate.HasValue && _lastDailyEvaluationDate.Value == targetDate)
                {
                    return;
                }

                int? jobLogId = null;
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    _logger.LogInformation(
                        "[Attendance] Daily finalization started (date={Date})",
                        targetDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

                    jobLogId = await BeginJobLogAsync("DailyFinalization", targetDate, targetDate, stoppingToken);
                    var syncResult = await SyncRangeWithRetryAsync(
                        options,
                        targetDate,
                        targetDate,
                        stoppingToken,
                        evaluateAttendance: false);

                    if (syncResult.TotalError > 0)
                    {
                        stopwatch.Stop();
                        await CompleteJobLogAsync(jobLogId, "Failed", stopwatch.ElapsedMilliseconds, "Daily sync before finalization failed.", stoppingToken);
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

                    await LogMissingPunchWarningsAsync(targetDate, stoppingToken);
                    stopwatch.Stop();
                    await CompleteJobLogAsync(jobLogId, "Completed", stopwatch.ElapsedMilliseconds, "Daily finalization completed.", stoppingToken);

                    _logger.LogInformation(
                        "[Attendance] Daily finalization completed (date={Date}, time={EvaluationTime}, synced={SyncedTotal}, saved={SyncedSuccess}, updated={UpdatedCount})",
                        targetDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        evaluationTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
                        syncResult.Total,
                        syncResult.TotalSuccess,
                        updatedCount);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    await CompleteJobLogAsync(jobLogId, "Failed", stopwatch.ElapsedMilliseconds, ex.Message, stoppingToken);
                    _logger.LogError(
                        ex,
                        "Attendance daily evaluation failed for {WorkDate:yyyy-MM-dd}.",
                        targetDate);
                }
            }
            finally
            {
                if (distributedLock != null)
                {
                    await distributedLock.DisposeAsync();
                }

                _dailyFinalizationLock.Release();
            }
        }

        private async Task RunMonthlyResyncAsync(
            AttendanceMachineOptions options,
            CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            if (!options.MonthlyResyncEnabled)
            {
                return;
            }

            var now = GetAttendanceNow();
            if (now.Day != 1)
            {
                return;
            }

            var monthlyResyncTime = GetMonthlyResyncTime(options);
            if (now.TimeOfDay < monthlyResyncTime)
            {
                return;
            }

            var currentMonth = new DateTime(now.Year, now.Month, 1);
            var targetMonth = currentMonth.AddMonths(-1);
            if (_lastMonthlyResyncMonth.HasValue && _lastMonthlyResyncMonth.Value == targetMonth)
            {
                return;
            }

            if (!await TryEnterJobLockAsync(_monthlyResyncLock, "Monthly resync", stoppingToken))
            {
                return;
            }

            AttendanceDistributedLock? distributedLock = null;
            try
            {
                distributedLock = await TryAcquireDistributedJobLockAsync("attendance:monthly-resync", stoppingToken);
                if (distributedLock == null)
                {
                    return;
                }

                if (_lastMonthlyResyncMonth.HasValue && _lastMonthlyResyncMonth.Value == targetMonth)
                {
                    return;
                }

                var fromDate = targetMonth;
                var toDate = currentMonth.AddDays(-1);
                var syncKey = BuildMonthlyResyncKey(options, targetMonth);
                var syncState = await GetSyncStateAsync(syncKey, stoppingToken);
                if (syncState?.IsCompleted == true)
                {
                    _lastMonthlyResyncMonth = targetMonth;
                    return;
                }

                await SaveSyncStateAsync(
                    syncKey,
                    fromDate,
                    toDate,
                    false,
                    $"Monthly attendance resync is running for {targetMonth:yyyy-MM}.",
                    stoppingToken);

                var monthText = targetMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                _logger.LogInformation(
                    "[Attendance] Monthly resync started (month={Month}, range={FromDate} \u2192 {ToDate})",
                    monthText,
                    fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

                var stopwatch = Stopwatch.StartNew();
                var jobLogId = await BeginJobLogAsync("MonthlyResync", fromDate, toDate, stoppingToken);
                try
                {
                    var result = await SyncRangeWithRetryAsync(
                        options,
                        fromDate,
                        toDate,
                        stoppingToken,
                        evaluateAttendance: false);

                    if (result.TotalError > 0)
                    {
                        stopwatch.Stop();
                        await CompleteJobLogAsync(jobLogId, "Failed", stopwatch.ElapsedMilliseconds, "Monthly resync failed.", stoppingToken);
                        var firstError = result.Errors.FirstOrDefault()?.Content ?? "Unknown error";
                        await SaveSyncStateAsync(
                            syncKey,
                            fromDate,
                            toDate,
                            false,
                            "Monthly attendance resync failed: " + firstError,
                            stoppingToken);
                        _logger.LogWarning(
                            "Attendance monthly resync returned {ErrorCount} error(s) for {Month}. First error: {FirstError}",
                            result.TotalError,
                            monthText,
                            firstError);
                        return;
                    }

                    await RunDirtyDateRecalculationsAsync(result.ChangedWorkDates, options, stoppingToken);

                    stopwatch.Stop();
                    await SaveSyncStateAsync(
                        syncKey,
                        fromDate,
                        toDate,
                        true,
                        $"Monthly attendance resync completed. Synced {result.Total} row(s), saved {result.TotalSuccess} row(s).",
                        stoppingToken);

                    _lastMonthlyResyncMonth = targetMonth;
                    await CompleteJobLogAsync(jobLogId, "Completed", stopwatch.ElapsedMilliseconds, "Monthly resync completed.", stoppingToken);
                    _logger.LogInformation(
                        "[Attendance] Monthly resync completed (month={Month})",
                        monthText);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    await CompleteJobLogAsync(jobLogId, "Failed", stopwatch.ElapsedMilliseconds, ex.Message, stoppingToken);
                    await SaveSyncStateAsync(
                        syncKey,
                        fromDate,
                        toDate,
                        false,
                        "Monthly attendance resync crashed: " + ex.Message,
                        stoppingToken);
                    _logger.LogError(
                        ex,
                        "Attendance monthly resync failed for {Month}.",
                        monthText);
                }
            }
            finally
            {
                if (distributedLock != null)
                {
                    await distributedLock.DisposeAsync();
                }

                _monthlyResyncLock.Release();
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

            var today = GetAttendanceNow().Date;
            var startDate = GetHistoricalSyncStartDate(options, today);
            var endDate = today;
            if (startDate > endDate)
            {
                _historicalSyncCompleted = true;
                return;
            }

            var syncKey = BuildHistoricalSyncKey(options, startDate);

            int? jobLogId = null;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                jobLogId = await BeginJobLogAsync("HistoricalSync", startDate, endDate, stoppingToken);
                await SaveSyncStateAsync(
                    syncKey,
                    startDate,
                    endDate,
                    false,
                    "Historical attendance sync is running.",
                    stoppingToken);

                var result = await SyncRangeWithRetryAsync(options, startDate, endDate, stoppingToken, evaluateAttendance: false);
                if (result.TotalError > 0)
                {
                    stopwatch.Stop();
                    await CompleteJobLogAsync(jobLogId, "Failed", stopwatch.ElapsedMilliseconds, "Historical sync failed.", stoppingToken);
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

                await RunDirtyDateRecalculationsAsync(result.ChangedWorkDates, options, stoppingToken);

                stopwatch.Stop();
                await SaveSyncStateAsync(
                    syncKey,
                    startDate,
                    endDate,
                    true,
                    $"Historical attendance sync completed. Synced {result.Total} row(s), saved {result.TotalSuccess} row(s).",
                    stoppingToken);

                _historicalSyncCompleted = true;
                await CompleteJobLogAsync(jobLogId, "Completed", stopwatch.ElapsedMilliseconds, "Historical sync completed.", stoppingToken);
                _logger.LogInformation(
                    "Attendance historical sync completed for range {FromDate:yyyy-MM-dd} -> {ToDate:yyyy-MM-dd}. Synced {Total} row(s), saved {Success} row(s).",
                    startDate,
                    endDate,
                    result.Total,
                    result.TotalSuccess);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await CompleteJobLogAsync(jobLogId, "Failed", stopwatch.ElapsedMilliseconds, ex.Message, stoppingToken);
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
            options.UseAccessRealtime = false;
            options.UseDirectSqlRealtime = false;
            options.UseDirectDeviceRealtime = true;
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
                ? 60
                : options.RealtimeSyncIntervalSeconds;
            options.RealtimeSyncLookbackDays = options.RealtimeSyncLookbackDays <= 0
                ? DefaultLookbackDays
                : options.RealtimeSyncLookbackDays;
            options.RealtimeSyncTimes = string.IsNullOrWhiteSpace(options.RealtimeSyncTimes)
                ? DefaultSyncTimes
                : options.RealtimeSyncTimes.Trim();
            options.SchedulerPollIntervalSeconds = options.SchedulerPollIntervalSeconds <= 0
                ? DefaultPollIntervalSeconds
                : options.SchedulerPollIntervalSeconds;
            options.DailyEvaluationHour = options.DailyEvaluationHour < 0 || options.DailyEvaluationHour > 23
                ? 21
                : options.DailyEvaluationHour;
            options.DailyEvaluationTime = string.IsNullOrWhiteSpace(options.DailyEvaluationTime)
                ? DefaultDailyEvaluationTime
                : options.DailyEvaluationTime.Trim();
            options.HistoricalSyncLookbackDays = options.HistoricalSyncLookbackDays < 0
                ? DefaultLookbackDays
                : options.HistoricalSyncLookbackDays;
            options.MonthlyResyncTime = string.IsNullOrWhiteSpace(options.MonthlyResyncTime)
                ? DefaultMonthlyResyncTime
                : options.MonthlyResyncTime.Trim();
            options.HistoricalSyncFromDate = options.HistoricalSyncFromDate?.Date;
            options.DirectSqlConnectionString = string.IsNullOrWhiteSpace(options.DirectSqlConnectionString)
                ? null
                : options.DirectSqlConnectionString.Trim();
            return options;
        }

        private static DateTime? GetLatestDueScheduledSyncSlot(AttendanceMachineOptions options, DateTime now)
        {
            var syncTimes = GetRealtimeSyncTimes(options);
            DateTime? latestDueSlot = null;
            foreach (var syncTime in syncTimes)
            {
                var slot = now.Date.Add(syncTime);
                if (now >= slot)
                {
                    latestDueSlot = slot;
                }
            }

            return latestDueSlot;
        }

        private static IReadOnlyList<TimeSpan> GetRealtimeSyncTimes(AttendanceMachineOptions options)
        {
            var rawValue = string.IsNullOrWhiteSpace(options.RealtimeSyncTimes)
                ? DefaultSyncTimes
                : options.RealtimeSyncTimes;
            var values = rawValue.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            var syncTimes = new List<TimeSpan>();

            foreach (var value in values)
            {
                if (TryParseClockTime(value, out var syncTime) && !syncTimes.Contains(syncTime))
                {
                    syncTimes.Add(syncTime);
                }
            }

            if (syncTimes.Count == 0)
            {
                syncTimes.Add(new TimeSpan(12, 30, 0));
                syncTimes.Add(new TimeSpan(20, 30, 0));
            }

            syncTimes.Sort();
            return syncTimes;
        }

        private static TimeSpan GetDailyEvaluationTime(AttendanceMachineOptions options)
        {
            if (TryParseClockTime(options.DailyEvaluationTime, out var evaluationTime))
            {
                return evaluationTime;
            }

            var evaluationHour = options.DailyEvaluationHour;
            if (evaluationHour < 0 || evaluationHour > 23)
            {
                evaluationHour = 21;
            }

            return new TimeSpan(evaluationHour, 0, 0);
        }

        private static TimeSpan GetMonthlyResyncTime(AttendanceMachineOptions options)
        {
            return TryParseClockTime(options.MonthlyResyncTime, out var monthlyResyncTime)
                ? monthlyResyncTime
                : new TimeSpan(1, 0, 0);
        }

        private static DateTime GetHistoricalSyncStartDate(AttendanceMachineOptions options, DateTime today)
        {
            if (options.HistoricalSyncLookbackDays > 0)
            {
                return today.Date.AddDays(1 - options.HistoricalSyncLookbackDays);
            }

            return (options.HistoricalSyncFromDate ?? new DateTime(2000, 1, 1)).Date;
        }

        private static bool TryParseClockTime(string? value, out TimeSpan time)
        {
            time = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var parts = value.Trim().Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || parts.Length > 3)
            {
                return false;
            }

            if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hour) ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minute))
            {
                return false;
            }

            var second = 0;
            if (parts.Length == 3 &&
                !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out second))
            {
                return false;
            }

            if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            {
                return false;
            }

            time = new TimeSpan(hour, minute, second);
            return true;
        }

        private async Task RunDirtyDateRecalculationsAsync(
            IEnumerable<DateTime> changedWorkDates,
            AttendanceMachineOptions options,
            CancellationToken stoppingToken)
        {
            var today = GetAttendanceNow().Date;
            var dates = changedWorkDates
                .Select(item => item.Date)
                .Where(date => date < today)
                .Distinct()
                .OrderBy(date => date)
                .ToList();

            foreach (var date in dates)
            {
                stoppingToken.ThrowIfCancellationRequested();

                if (await IsAttendanceDateLockedAsync(date, stoppingToken))
                {
                    _logger.LogInformation(
                        "[Attendance] Recalculation skipped because date is locked (date={Date})",
                        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    continue;
                }

                _logger.LogInformation(
                    "[Attendance] Recalculation triggered (date={Date})",
                    date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                await RunRecalculationForDateAsync(date, options, stoppingToken);
            }
        }

        private async Task RunRecalculationForDateAsync(
            DateTime targetDate,
            AttendanceMachineOptions options,
            CancellationToken stoppingToken)
        {
            if (!await TryEnterJobLockAsync(_dailyFinalizationLock, "Daily finalization", stoppingToken))
            {
                return;
            }

            AttendanceDistributedLock? distributedLock = null;
            int? jobLogId = null;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                distributedLock = await TryAcquireDistributedJobLockAsync(
                    $"attendance:recalculation:{targetDate:yyyyMMdd}",
                    stoppingToken);
                if (distributedLock == null)
                {
                    return;
                }

                if (await IsAttendanceDateLockedAsync(targetDate, stoppingToken))
                {
                    return;
                }

                jobLogId = await BeginJobLogAsync("Recalculation", targetDate, targetDate, stoppingToken);
                _logger.LogInformation(
                    "[Attendance] Daily finalization started (date={Date})",
                    targetDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

                var updatedCount = await _attendanceBusiness.EvaluateAttendanceRangeAsync(
                    targetDate,
                    targetDate,
                    options.RealtimeSyncUserId);
                await LogMissingPunchWarningsAsync(targetDate, stoppingToken);

                stopwatch.Stop();
                await CompleteJobLogAsync(
                    jobLogId,
                    "Completed",
                    stopwatch.ElapsedMilliseconds,
                    $"Recalculation completed. Updated {updatedCount} row(s).",
                    stoppingToken);

                _logger.LogInformation(
                    "[Attendance] Daily finalization completed (date={Date}, updated={UpdatedCount})",
                    targetDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    updatedCount);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await CompleteJobLogAsync(jobLogId, "Failed", stopwatch.ElapsedMilliseconds, ex.Message, stoppingToken);
                _logger.LogError(
                    ex,
                    "Attendance recalculation failed for {WorkDate:yyyy-MM-dd}.",
                    targetDate);
            }
            finally
            {
                if (distributedLock != null)
                {
                    await distributedLock.DisposeAsync();
                }

                _dailyFinalizationLock.Release();
            }
        }

        private async Task<bool> IsAttendanceDateLockedAsync(DateTime workDate, CancellationToken stoppingToken)
        {
            var connectionString = GetApplicationConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(stoppingToken);
                await using var command = connection.CreateCommand();
                command.CommandText = @"
SELECT CASE WHEN EXISTS
(
    SELECT 1
    FROM dbo.AttendanceLocks
    WHERE IsLocked = 1
      AND ISNULL(Deleted, 0) = 0
      AND @WorkDate >= RangeFrom
      AND @WorkDate <= RangeTo
)
OR EXISTS
(
    SELECT 1
    FROM dbo.AttendanceRecords
    WHERE WorkDate = @WorkDate
      AND ISNULL(IsLocked, 0) = 1
)
THEN 1 ELSE 0 END;";
                command.Parameters.AddWithValue("@WorkDate", workDate.Date);
                var result = await command.ExecuteScalarAsync(stoppingToken);
                return result != null && result != DBNull.Value && Convert.ToInt32(result, CultureInfo.InvariantCulture) == 1;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Could not check attendance lock for {WorkDate:yyyy-MM-dd}. Continuing as unlocked.",
                    workDate);
                return false;
            }
        }

        private async Task LogMissingPunchWarningsAsync(DateTime workDate, CancellationToken stoppingToken)
        {
            var connectionString = GetApplicationConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(stoppingToken);
                await using var command = connection.CreateCommand();
                command.CommandText = @"
SELECT COUNT(1)
FROM dbo.AttendanceRecords
WHERE WorkDate = @WorkDate
  AND ISNULL(IsLocked, 0) = 0
  AND (
        (CheckIn IS NULL AND CheckOut IS NOT NULL)
        OR (CheckIn IS NOT NULL AND CheckOut IS NULL)
      );";
                command.Parameters.AddWithValue("@WorkDate", workDate.Date);
                var count = Convert.ToInt32(await command.ExecuteScalarAsync(stoppingToken), CultureInfo.InvariantCulture);
                if (count > 0)
                {
                    _logger.LogWarning(
                        "[Attendance] Abnormal data detected: {Count} record(s) missing check-in or check-out (date={Date})",
                        count,
                        workDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Could not inspect attendance abnormal data for {WorkDate:yyyy-MM-dd}.",
                    workDate);
            }
        }

        private static DateTime GetAttendanceNow()
        {
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, AttendanceTimeZone).DateTime;
        }

        private static TimeZoneInfo ResolveAttendanceTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
            }
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

        private async Task<int?> BeginJobLogAsync(
            string jobType,
            DateTime? rangeFrom,
            DateTime? rangeTo,
            CancellationToken stoppingToken)
        {
            var connectionString = GetApplicationConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(stoppingToken);
                await using var command = connection.CreateCommand();
                command.CommandText = @"
INSERT INTO dbo.AttendanceJobLogs
(
    JobType,
    RunTime,
    Status,
    RangeFrom,
    RangeTo
)
OUTPUT INSERTED.Id
VALUES
(
    @JobType,
    @RunTime,
    @Status,
    @RangeFrom,
    @RangeTo
);";
                command.Parameters.AddWithValue("@JobType", jobType);
                command.Parameters.AddWithValue("@RunTime", GetAttendanceNow());
                command.Parameters.AddWithValue("@Status", "Running");
                command.Parameters.AddWithValue("@RangeFrom", (object?)rangeFrom?.Date ?? DBNull.Value);
                command.Parameters.AddWithValue("@RangeTo", (object?)rangeTo?.Date ?? DBNull.Value);
                var result = await command.ExecuteScalarAsync(stoppingToken);
                return result == null || result == DBNull.Value
                    ? null
                    : Convert.ToInt32(result, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not write attendance job log start for {JobType}.", jobType);
                return null;
            }
        }

        private async Task CompleteJobLogAsync(
            int? jobLogId,
            string status,
            long durationMs,
            string? message,
            CancellationToken stoppingToken)
        {
            if (!jobLogId.HasValue)
            {
                return;
            }

            var connectionString = GetApplicationConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(stoppingToken);
                await using var command = connection.CreateCommand();
                command.CommandText = @"
UPDATE dbo.AttendanceJobLogs
SET Status = @Status,
    DurationMs = @DurationMs,
    Message = @Message
WHERE Id = @Id;";
                command.Parameters.AddWithValue("@Id", jobLogId.Value);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@DurationMs", durationMs);
                command.Parameters.AddWithValue("@Message", (object?)message ?? DBNull.Value);
                await command.ExecuteNonQueryAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not update attendance job log {JobLogId}.", jobLogId.Value);
            }
        }

        private async Task<AttendanceDistributedLock?> TryAcquireDistributedJobLockAsync(
            string resource,
            CancellationToken stoppingToken)
        {
            var connectionString = GetApplicationConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            var connection = new SqlConnection(connectionString);
            try
            {
                await connection.OpenAsync(stoppingToken);
                await using var command = connection.CreateCommand();
                command.CommandText = @"
DECLARE @LockResult int;
EXEC @LockResult = sp_getapplock
    @Resource = @Resource,
    @LockMode = 'Exclusive',
    @LockOwner = 'Session',
    @LockTimeout = 0;
SELECT @LockResult;";
                command.Parameters.AddWithValue("@Resource", resource);
                var result = await command.ExecuteScalarAsync(stoppingToken);
                var lockResult = result == null || result == DBNull.Value
                    ? -999
                    : Convert.ToInt32(result, CultureInfo.InvariantCulture);

                if (lockResult < 0)
                {
                    await connection.DisposeAsync();
                    _logger.LogWarning(
                        "[Attendance] Distributed lock skipped because resource is busy (resource={Resource}, result={Result})",
                        resource,
                        lockResult);
                    return null;
                }

                return new AttendanceDistributedLock(connection, resource);
            }
            catch (Exception ex)
            {
                await connection.DisposeAsync();
                _logger.LogWarning(
                    ex,
                    "[Attendance] Could not acquire distributed lock (resource={Resource}).",
                    resource);
                return null;
            }
        }

        private static string BuildHistoricalSyncKey(AttendanceMachineOptions options, DateTime startDate)
        {
            var sourceKey = BuildSourceKey(options);
            return $"attendance-historical-sync:{sourceKey}:{startDate:yyyyMMdd}";
        }

        private static string BuildScheduledSyncKey(AttendanceMachineOptions options, DateTime scheduledSlot)
        {
            var sourceKey = BuildSourceKey(options);
            return $"attendance-scheduled-sync:{sourceKey}:{scheduledSlot:yyyyMMddHHmm}";
        }

        private static string BuildMonthlyResyncKey(AttendanceMachineOptions options, DateTime targetMonth)
        {
            var sourceKey = BuildSourceKey(options);
            return $"attendance-monthly-resync:{sourceKey}:{targetMonth:yyyyMM}";
        }

        private static string BuildSourceKey(AttendanceMachineOptions options)
        {
            return $"device:{options.DeviceIp ?? "unknown"}:{options.DevicePort}";
        }

        private async Task<bool> TryEnterJobLockAsync(
            SemaphoreSlim jobLock,
            string jobName,
            CancellationToken stoppingToken)
        {
            var acquired = await jobLock.WaitAsync(0, stoppingToken);
            if (!acquired)
            {
                _logger.LogWarning(
                    "[Attendance] {JobName} skipped because another execution is still running.",
                    jobName);
            }

            return acquired;
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

        private sealed class AttendanceDistributedLock : IAsyncDisposable
        {
            private readonly SqlConnection _connection;
            private readonly string _resource;
            private bool _disposed;

            public AttendanceDistributedLock(SqlConnection connection, string resource)
            {
                _connection = connection;
                _resource = resource;
            }

            public async ValueTask DisposeAsync()
            {
                if (_disposed)
                {
                    return;
                }

                try
                {
                    await using var command = _connection.CreateCommand();
                    command.CommandText = @"
EXEC sp_releaseapplock
    @Resource = @Resource,
    @LockOwner = 'Session';";
                    command.Parameters.AddWithValue("@Resource", _resource);
                    await command.ExecuteNonQueryAsync();
                }
                finally
                {
                    await _connection.DisposeAsync();
                    _disposed = true;
                }
            }
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
