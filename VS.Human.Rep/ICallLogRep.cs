using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface ICallLogRep
    {
        Task<int> InsertAsync(CallLog callLog);
        Task<bool> UpdateProviderResultAsync(int id, string status, string? providerCallId, string? providerResponseJson, string? errorMessage);
        Task<bool> UpdateOutcomeAsync(CallLogOutcomeUpdate request, int userId);
        Task<List<CallLog>> GetByEntityAsync(string entityType, int entityId, int limit = 50);
        Task<List<CallLog>> GetRecentAsync(int limit = 10);
        Task<TelephonyDashboardSnapshot> GetDashboardSnapshotAsync();
    }
}
