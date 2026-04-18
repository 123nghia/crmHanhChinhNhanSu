using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IWorkflowTimelineBusiness
    {
        Task<int> TrackAsync(WorkflowTimelineEvent timelineEvent);
        Task<int> TrackLeaveAsync(
            LeaveIndexModel leave,
            string eventCode,
            string eventTitle,
            int? triggeredBy,
            string? triggeredByName = null,
            string? metadataJson = null,
            string? errorMessage = null,
            bool emailSent = false,
            bool emailFailed = false,
            int? emailLogId = null,
            bool notificationCreated = false,
            int? notificationId = null,
            string? attendanceSyncStatus = null);
        Task<List<WorkflowTimelineEvent>> GetByEntityAsync(string entityType, int entityId);
        Task<WorkflowActionCenterSummary> GetActionCenterAsync(int userId, string? roleCode);
    }
}
