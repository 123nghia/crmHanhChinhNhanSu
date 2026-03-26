using Microsoft.AspNetCore.Http;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class EmailSentLogBusiness : BaseBusiness, IEmailSentLogBusiness
    {
        public EmailSentLogBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
        }

        public async Task<BaseList> GetLogs(EmailSentLogRequest request, int currentUserId, string? currentRoleCode)
        {
            request ??= new EmailSentLogRequest();
            request.UserId = currentUserId;
            request.RoleCode = currentRoleCode;
            if (request.Page <= 0)
            {
                request.Page = 1;
            }

            if (request.Limit <= 0)
            {
                request.Limit = 20;
            }

            return await _unitOfWork.EmailSentLogRep.GetLogsAsync(request);
        }

        public async Task<EmailSentLog?> GetById(int id, int currentUserId, string? currentRoleCode)
        {
            if (id <= 0)
            {
                return null;
            }

            return await _unitOfWork.EmailSentLogRep.GetByIdAsync(id, currentUserId, currentRoleCode);
        }
    }
}
