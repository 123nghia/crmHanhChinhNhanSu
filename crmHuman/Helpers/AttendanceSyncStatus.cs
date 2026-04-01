using System;

namespace crmHuman.Helpers
{
    public sealed class AttendanceSyncStatus
    {
        public string ModeText { get; set; } = string.Empty;
        public DateTime? LatestLogTime { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public DateTime? AttendanceLastUpdatedAt { get; set; }
        public DateTime? AttendanceThroughDate { get; set; }
        public string? ErrorMessage { get; set; }

        public bool HasData =>
            LatestLogTime.HasValue
            || LastSyncedAt.HasValue
            || AttendanceLastUpdatedAt.HasValue
            || AttendanceThroughDate.HasValue
            || !string.IsNullOrWhiteSpace(ErrorMessage);
    }
}
