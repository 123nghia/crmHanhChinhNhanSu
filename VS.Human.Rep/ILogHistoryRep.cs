using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface ILogHistoryRep
    {
        Task<int> InsertAsync(LogHistory record);
        Task<bool> UpdateLogoutAsync(int userId, string? roleCode, int? logId = null, DateTime? logoutAt = null);
    }
}
