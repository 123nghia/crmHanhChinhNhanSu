using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IReportAnalyticsBusiness
    {
        Task<ReportAnalyticsSummary> GetSummaryAsync(int expiringDays = 30);
        Task<List<ContractStatusCount>> GetContractStatusCountsAsync();
    }
}
