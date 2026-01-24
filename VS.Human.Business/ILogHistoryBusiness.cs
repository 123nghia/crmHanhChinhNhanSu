namespace VS.Human.Business
{
    public interface ILogHistoryBusiness
    {
        Task<int> LogLogin(int userId, string? userName, string? fullName, string? roleCode, string? source = null);
        Task<bool> LogLogout(int userId, string? roleCode, int? logId = null);
    }
}
