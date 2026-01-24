using Microsoft.AspNetCore.Http;
using System;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class LogHistoryBusiness : BaseBusiness, ILogHistoryBusiness
    {
        public LogHistoryBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
        }

        public async Task<int> LogLogin(int userId, string? userName, string? fullName, string? roleCode, string? source = null)
        {
            try
            {
                var record = new LogHistory
                {
                    UserId = userId,
                    UserName = userName,
                    FullName = fullName,
                    RoleCode = roleCode,
                    LoginAt = DateTime.Now,
                    Source = source
                };

                return await _unitOfWork.LogHistoryRep.InsertAsync(record);
            }
            catch
            {
                return 0;
            }
        }

        public async Task<bool> LogLogout(int userId, string? roleCode, int? logId = null)
        {
            try
            {
                return await _unitOfWork.LogHistoryRep.UpdateLogoutAsync(userId, roleCode, logId, DateTime.Now);
            }
            catch
            {
                return false;
            }
        }
    }
}
