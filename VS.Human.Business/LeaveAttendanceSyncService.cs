using Microsoft.Extensions.Logging;
using VS.Human.Rep;

namespace VS.Human.Business
{
    public sealed class LeaveAttendanceSyncResult
    {
        public bool Success { get; init; }
        public bool Skipped { get; init; }
        public int UpdatedCount { get; init; }
        public int AttemptCount { get; init; }
        public string? Error { get; init; }
    }

    public interface ILeaveAttendanceSyncService
    {
        Task<LeaveAttendanceSyncResult> SyncAsync(LeaveAttendanceImpactResult impact, int userId);
    }

    public sealed class LeaveAttendanceSyncService : ILeaveAttendanceSyncService
    {
        private static readonly TimeSpan[] RetryDelays =
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(3)
        };

        private readonly IAttendanceBusiness _attendanceBusiness;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LeaveAttendanceSyncService> _logger;

        public LeaveAttendanceSyncService(
            IAttendanceBusiness attendanceBusiness,
            IUnitOfWork unitOfWork,
            ILogger<LeaveAttendanceSyncService> logger)
        {
            _attendanceBusiness = attendanceBusiness;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<LeaveAttendanceSyncResult> SyncAsync(LeaveAttendanceImpactResult impact, int userId)
        {
            if (!impact.RequiresAttendanceSync)
            {
                return new LeaveAttendanceSyncResult
                {
                    Success = true,
                    Skipped = true,
                    AttemptCount = 0
                };
            }

            var from = impact.RangeFrom!.Value.Date;
            var to = impact.RangeTo!.Value.Date;
            if (from > to)
            {
                (from, to) = (to, from);
            }

            await _unitOfWork.LeaveRep.UpdateAttendanceSyncStatusAsync(
                impact.LeaveId,
                "Pending",
                from,
                to,
                null,
                null,
                0,
                userId);

            Exception? lastException = null;
            for (var attempt = 1; attempt <= RetryDelays.Length; attempt++)
            {
                var delay = RetryDelays[attempt - 1];
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay);
                }

                try
                {
                    _logger.LogInformation(
                        "[LeaveAttendance] Recalculation started (leaveId={LeaveId}, action={Action}, range={FromDate} -> {ToDate}, attempt={Attempt})",
                        impact.LeaveId,
                        impact.Kind,
                        from.ToString("yyyy-MM-dd"),
                        to.ToString("yyyy-MM-dd"),
                        attempt);

                    var updatedCount = await _attendanceBusiness.EvaluateAttendanceRangeAsync(from, to, userId);
                    await _unitOfWork.LeaveRep.UpdateAttendanceSyncStatusAsync(
                        impact.LeaveId,
                        "Completed",
                        from,
                        to,
                        DateTime.Now,
                        null,
                        attempt,
                        userId);

                    _logger.LogInformation(
                        "[LeaveAttendance] Recalculation completed (leaveId={LeaveId}, action={Action}, range={FromDate} -> {ToDate}, updated={UpdatedCount})",
                        impact.LeaveId,
                        impact.Kind,
                        from.ToString("yyyy-MM-dd"),
                        to.ToString("yyyy-MM-dd"),
                        updatedCount);

                    return new LeaveAttendanceSyncResult
                    {
                        Success = true,
                        UpdatedCount = updatedCount,
                        AttemptCount = attempt
                    };
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(
                        ex,
                        "[LeaveAttendance] Recalculation failed (leaveId={LeaveId}, action={Action}, range={FromDate} -> {ToDate}, attempt={Attempt})",
                        impact.LeaveId,
                        impact.Kind,
                        from.ToString("yyyy-MM-dd"),
                        to.ToString("yyyy-MM-dd"),
                        attempt);
                }
            }

            var error = lastException?.Message ?? "Attendance recalculation failed.";
            await _unitOfWork.LeaveRep.UpdateAttendanceSyncStatusAsync(
                impact.LeaveId,
                "Failed",
                from,
                to,
                null,
                error,
                RetryDelays.Length,
                userId);

            return new LeaveAttendanceSyncResult
            {
                Success = false,
                AttemptCount = RetryDelays.Length,
                Error = error
            };
        }
    }
}
