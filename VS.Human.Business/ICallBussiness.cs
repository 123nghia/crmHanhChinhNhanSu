using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface ICallBussiness
    {
        Task<bool> MakeCall(string phone, string type, int? IdRel, string chanel, int userId);
        Task<CallActionResult> MakeCallDetailed(string phone, string type, int? IdRel, string chanel, int userId, string? sourcePage = null);
        Task<List<CallLog>> GetCallHistory(string entityType, int entityId, int limit = 50);
        Task<bool> UpdateCallOutcome(CallLogOutcomeUpdate request, int userId);
        Task<TelephonyDashboardSnapshot> GetTelephonyDashboardSnapshot();
    }
}
