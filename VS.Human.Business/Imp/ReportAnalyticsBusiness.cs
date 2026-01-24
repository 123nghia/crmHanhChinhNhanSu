using Microsoft.AspNetCore.Http;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class ReportAnalyticsBusiness : BaseBusiness, IReportAnalyticsBusiness
    {
        private readonly IAuditLogBusiness _auditLogBusiness;

        public ReportAnalyticsBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IAuditLogBusiness auditLogBusiness)
            : base(unitOfWork, httpContextAccessor)
        {
            _auditLogBusiness = auditLogBusiness;
        }

        public async Task<ReportAnalyticsSummary> GetSummaryAsync(int expiringDays = 30)
        {
            var summary = await _unitOfWork.ReportAnalyticsRep.GetSummaryAsync(expiringDays);
            var audit = await _auditLogBusiness.GetTodayStatsAsync();
            summary.TotalRequestsToday = audit.TotalRequests;
            summary.TotalVisitsToday = audit.TotalVisits;
            return summary;
        }

        public async Task<List<ContractStatusCount>> GetContractStatusCountsAsync()
        {
            return await _unitOfWork.ReportAnalyticsRep.GetContractStatusCountsAsync();
        }
    }
}
