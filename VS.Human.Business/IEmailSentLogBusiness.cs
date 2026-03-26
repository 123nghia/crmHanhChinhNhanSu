using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IEmailSentLogBusiness
    {
        Task<BaseList> GetLogs(EmailSentLogRequest request, int currentUserId, string? currentRoleCode);
        Task<EmailSentLog?> GetById(int id, int currentUserId, string? currentRoleCode);
    }
}
