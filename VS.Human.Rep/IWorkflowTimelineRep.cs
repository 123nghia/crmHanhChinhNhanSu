using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IWorkflowTimelineRep
    {
        Task<int> AddAsync(WorkflowTimelineEvent timelineEvent);
        Task<List<WorkflowTimelineEvent>> GetByEntityAsync(string entityType, int entityId);
        Task<WorkflowActionCenterSummary> GetActionCenterAsync(int userId, string? roleCode);
    }
}
