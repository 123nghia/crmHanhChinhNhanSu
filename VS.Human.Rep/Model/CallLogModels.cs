namespace VS.Human.Rep.Model
{
    public static class CallDirections
    {
        public const string Outbound = "outbound";
        public const string Inbound = "inbound";
    }

    public static class CallStatuses
    {
        public const string Initiated = "initiated";
        public const string Ringing = "ringing";
        public const string Connected = "connected";
        public const string Failed = "failed";
        public const string Missed = "missed";
        public const string Completed = "completed";
    }

    public class CallLog : BaseModel
    {
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? Extension { get; set; }
        public string? LineCode { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Direction { get; set; } = CallDirections.Outbound;
        public string? ProviderCallId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Duration { get; set; }
        public string Status { get; set; } = CallStatuses.Initiated;
        public string? RecordingUrl { get; set; }
        public string? Outcome { get; set; }
        public string? Notes { get; set; }
        public string? SourcePage { get; set; }
        public string? ProviderRequestJson { get; set; }
        public string? ProviderResponseJson { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? NextFollowUpAt { get; set; }
    }

    public class CallLogOutcomeUpdate
    {
        public int Id { get; set; }
        public string? Outcome { get; set; }
        public string? Notes { get; set; }
        public DateTime? NextFollowUpAt { get; set; }
    }

    public class TelephonyDashboardSnapshot
    {
        public int TotalToday { get; set; }
        public int SuccessToday { get; set; }
        public int FailedToday { get; set; }
        public int MissedToday { get; set; }
        public int ExtensionOnline { get; set; }
        public int AgentBusy { get; set; }
        public int FollowUpCount { get; set; }
        public bool PbxHealthy { get; set; }
        public DateTime? LastHealthCheckAt { get; set; }
        public string? HealthMessage { get; set; }
        public List<CallLog> RecentCalls { get; set; } = new List<CallLog>();
    }

    public class CallActionResult
    {
        public bool Success { get; set; }
        public int CallLogId { get; set; }
        public string? ProviderCallId { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }
}
