namespace VS.Human.Rep.Model
{
    public class ReportAnalyticsSummary
    {
        public int TotalEmployees { get; set; }
        public int TotalContracts { get; set; }
        public int ExpiringContracts { get; set; }
        public int PendingLeaveRequests { get; set; }
        public int TotalRequestsToday { get; set; }
        public int TotalVisitsToday { get; set; }
    }

    public class ContractStatusCount
    {
        public string? Status { get; set; }
        public int Total { get; set; }
    }
}
