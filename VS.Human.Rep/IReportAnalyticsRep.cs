using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IReportAnalyticsRep
    {
        Task<ReportAnalyticsSummary> GetSummaryAsync(int expiringDays);
        Task<List<ContractStatusCount>> GetContractStatusCountsAsync();
    }
}
