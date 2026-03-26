using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IEmailSentLogRep
    {
        Task<int> InsertAsync(EmailSentLog log);
        Task<BaseList> GetLogsAsync(EmailSentLogRequest request);
        Task<EmailSentLog?> GetByIdAsync(int id, int userId, string? roleCode);
    }
}
