using System;
using Microsoft.AspNetCore.Http;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class AuditLogBusiness : BaseBusiness, IAuditLogBusiness
    {
        public AuditLogBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
        }

        public async Task<int> LogAsync(int? userId, string? userName, string? fullName, string? roleCode,
            string action, string path, string? queryString, string? payload,
            int? statusCode, int? durationMs, string? clientIp, string? userAgent)
        {
            try
            {
                var record = new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    FullName = fullName,
                    RoleCode = roleCode,
                    Action = action ?? string.Empty,
                    Path = path ?? string.Empty,
                    QueryString = queryString,
                    Payload = payload,
                    StatusCode = statusCode,
                    DurationMs = durationMs,
                    ClientIp = clientIp,
                    UserAgent = userAgent
                };

                return await _unitOfWork.AuditLogRep.InsertAsync(record);
            }
            catch
            {
                return 0;
            }
        }

        public async Task<AuditLogStats> GetTodayStatsAsync(DateTime? date = null)
        {
            try
            {
                var start = (date ?? DateTime.Today).Date;
                var end = start.AddDays(1);
                return await _unitOfWork.AuditLogRep.GetTodayStatsAsync(start, end);
            }
            catch
            {
                return new AuditLogStats();
            }
        }

        public async Task<IReadOnlyList<AuditLog>> GetCandidateActivityAsync(int candidateId, int top = 20)
        {
            try
            {
                if (candidateId <= 0)
                {
                    return Array.Empty<AuditLog>();
                }

                return await _unitOfWork.AuditLogRep.GetCandidateActivityAsync(candidateId, top);
            }
            catch
            {
                return Array.Empty<AuditLog>();
            }
        }
    }
}
