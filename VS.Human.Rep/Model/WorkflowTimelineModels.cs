namespace VS.Human.Rep.Model
{
    public static class WorkflowEntityTypes
    {
        public const string Leave = "LEAVE";
        public const string Candidate = "CANDIDATE";
        public const string Employee = "EMPLOYEE";
        public const string Order = "ORDER";
        public const string Call = "CALL";
    }

    public static class WorkflowEventCodes
    {
        public const string Created = "CREATED";
        public const string Updated = "UPDATED";
        public const string Approved = "APPROVED";
        public const string Rejected = "REJECTED";
        public const string Cancelled = "CANCELLED";
        public const string EmailSent = "EMAIL_SENT";
        public const string EmailFailed = "EMAIL_FAILED";
        public const string NotificationCreated = "NOTIFICATION_CREATED";
        public const string AttendanceSyncPending = "ATTENDANCE_SYNC_PENDING";
        public const string AttendanceSyncCompleted = "ATTENDANCE_SYNC_COMPLETED";
        public const string AttendanceSyncFailed = "ATTENDANCE_SYNC_FAILED";
        public const string CallStarted = "CALL_STARTED";
        public const string CallFailed = "CALL_FAILED";
        public const string CallCompleted = "CALL_COMPLETED";
    }

    public class WorkflowTimelineEvent : BaseModel
    {
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string EventCode { get; set; } = string.Empty;
        public string? EventTitle { get; set; }
        public int? CurrentStatus { get; set; }
        public string? CurrentStatusText { get; set; }
        public int? TriggeredBy { get; set; }
        public string? TriggeredByName { get; set; }
        public int? AssignedTo { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime OccurredAt { get; set; }
        public DateTime? DueAt { get; set; }
        public int? SlaHours { get; set; }
        public bool EmailSent { get; set; }
        public bool EmailFailed { get; set; }
        public int? EmailLogId { get; set; }
        public bool NotificationCreated { get; set; }
        public int? NotificationId { get; set; }
        public string? AttendanceSyncStatus { get; set; }
        public string? ErrorMessage { get; set; }
        public string? MetadataJson { get; set; }
    }

    public class WorkflowActionCenterSummary
    {
        public List<WorkflowTaskItem> MyTasks { get; set; } = new List<WorkflowTaskItem>();
        public List<WorkflowHealthItem> WorkflowHealth { get; set; } = new List<WorkflowHealthItem>();
        public List<WorkflowSystemIssue> SystemIssues { get; set; } = new List<WorkflowSystemIssue>();
        public List<WorkflowTimelineEvent> RecentTimeline { get; set; } = new List<WorkflowTimelineEvent>();
    }

    public class WorkflowTaskItem
    {
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public int Status { get; set; }
        public string? StatusText { get; set; }
        public string? CurrentStep { get; set; }
        public int? CurrentOwnerId { get; set; }
        public string? CurrentOwnerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime StepStartedAt { get; set; }
        public DateTime? DueAt { get; set; }
        public int WaitingHours { get; set; }
        public bool IsOverdue { get; set; }
        public string? Url { get; set; }
    }

    public class WorkflowHealthItem
    {
        public string StepCode { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
    }

    public class WorkflowSystemIssue
    {
        public string IssueType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Count { get; set; }
        public string? Url { get; set; }
        public string Severity { get; set; } = "info";
    }
}
