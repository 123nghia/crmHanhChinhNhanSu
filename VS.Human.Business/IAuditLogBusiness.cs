using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IAuditLogBusiness
    {
        Task<int> LogAsync(int? userId, string? userName, string? fullName, string? roleCode,
            string action, string path, string? queryString, string? payload,
            int? statusCode, int? durationMs, string? clientIp, string? userAgent);

        Task<AuditLogStats> GetTodayStatsAsync(DateTime? date = null);
    }
}
