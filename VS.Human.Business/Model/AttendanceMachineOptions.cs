namespace VS.Human.Business.Model
{
    public class AttendanceMachineOptions
    {
        public string? DbPath { get; set; }
        public string? DbUrl { get; set; }
        public string? Password { get; set; }
        public string? Provider { get; set; }
        public bool UseAccessRealtime { get; set; }
        public string? DeviceIp { get; set; }
        public int DevicePort { get; set; } = 4370;
        public bool UseDirectDeviceRealtime { get; set; }
        public int DeviceTimeoutSeconds { get; set; } = 120;
        public bool RealtimeSyncEnabled { get; set; }
        public int RealtimeSyncIntervalSeconds { get; set; } = 60;
        public int RealtimeSyncLookbackDays { get; set; } = 30;
        public string? RealtimeSyncTimes { get; set; } = "12:30,20:30";
        public int SchedulerPollIntervalSeconds { get; set; } = 60;
        public int RealtimeSyncUserId { get; set; }
        public bool DailyEvaluationEnabled { get; set; } = true;
        public int DailyEvaluationHour { get; set; } = 21;
        public string? DailyEvaluationTime { get; set; } = "21:00";
        public bool HistoricalSyncEnabled { get; set; }
        public DateTime? HistoricalSyncFromDate { get; set; }
        public int HistoricalSyncLookbackDays { get; set; } = 30;
        public bool MonthlyResyncEnabled { get; set; } = true;
        public string? MonthlyResyncTime { get; set; } = "01:00";
        public bool UseDirectSqlRealtime { get; set; }
        public string? DirectSqlConnectionString { get; set; }

        public string? LogTable { get; set; }
        public string? LogUserIdColumn { get; set; }
        public string? LogTimeColumn { get; set; }

        public string? UserTable { get; set; }
        public string? UserIdColumn { get; set; }
        public string? UserFingerprintColumn { get; set; }
        public string? UserNameColumn { get; set; }
        public string? UserScheduleColumn { get; set; }

        public string? DbConfiguredSource { get; set; }
        public string? DbSourceName { get; set; }
        public string? DbSourceError { get; set; }
    }
}
