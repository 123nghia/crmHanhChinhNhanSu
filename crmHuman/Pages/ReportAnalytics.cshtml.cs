using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class ReportAnalyticsModel : BaseModel2
    {
        private readonly ILogger<ReportAnalyticsModel> _logger;
        private readonly IReportAnalyticsBusiness _reportAnalyticsBusiness;

        public ReportAnalyticsSummary Summary { get; set; }
        public List<ContractStatusCount> ContractStatusCounts { get; set; }
        public int ExpiringDays { get; set; }

        public ReportAnalyticsModel(ILogger<ReportAnalyticsModel> logger, IReportAnalyticsBusiness reportAnalyticsBusiness)
        {
            _logger = logger;
            _reportAnalyticsBusiness = reportAnalyticsBusiness;
            TitlePage = "Bao cao va phan tich";
            KeyPage = "ReportAnalytics";
            Summary = new ReportAnalyticsSummary();
            ContractStatusCounts = new List<ContractStatusCount>();
            ExpiringDays = 30;
        }

        public async Task<IActionResult> OnGet(int days = 30)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (UserData.RoleCode == "2")
            {
                return Redirect("/");
            }

            ExpiringDays = days > 0 ? days : 30;
            Summary = await _reportAnalyticsBusiness.GetSummaryAsync(ExpiringDays);
            ContractStatusCounts = await _reportAnalyticsBusiness.GetContractStatusCountsAsync();
            return Page();
        }
    }
}
