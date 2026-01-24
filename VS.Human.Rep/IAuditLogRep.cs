using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IAuditLogRep
    {
        Task<int> InsertAsync(AuditLog record);
        Task<AuditLogStats> GetTodayStatsAsync(DateTime start, DateTime end);
    }
}
