using System.Data;
using System.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class ReportAnalyticsRep : IReportAnalyticsRep
    {
        private readonly IConfiguration _configuration;

        public ReportAnalyticsRep(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IDbConnection GetConnection()
        {
            var con = new SqlConnection(_configuration.GetConnectionString("stringConnect7"));
            con.Open();
            return con;
        }

        public async Task<ReportAnalyticsSummary> GetSummaryAsync(int expiringDays)
        {
            const string sql = @"
SELECT
    (SELECT COUNT(1) FROM Employees WHERE ISNULL(Deleted, 0) = 0) AS TotalEmployees,
    (SELECT COUNT(1) FROM Contracts WHERE ISNULL(Deleted, 0) = 0) AS TotalContracts,
    (SELECT COUNT(1) FROM Contracts
        WHERE ISNULL(Deleted, 0) = 0
          AND EndDate IS NOT NULL
          AND EndDate >= CAST(GETDATE() AS DATE)
          AND EndDate < DATEADD(DAY, @Days, CAST(GETDATE() AS DATE))
    ) AS ExpiringContracts,
    (SELECT COUNT(1) FROM LeaveRequests
        WHERE ISNULL(Deleted, 0) = 0
          AND Status IN (0, 1, 2)
    ) AS PendingLeaveRequests;
";

            using (var con = GetConnection())
            {
                var summary = await con.QuerySingleOrDefaultAsync<ReportAnalyticsSummary>(sql, new { Days = expiringDays });
                return summary ?? new ReportAnalyticsSummary();
            }
        }

        public async Task<List<ContractStatusCount>> GetContractStatusCountsAsync()
        {
            const string sql = @"
SELECT ISNULL(Status, '') AS Status, COUNT(1) AS Total
FROM Contracts
WHERE ISNULL(Deleted, 0) = 0
GROUP BY Status
ORDER BY Status;
";

            using (var con = GetConnection())
            {
                var result = await con.QueryAsync<ContractStatusCount>(sql);
                return result.ToList();
            }
        }
    }
}
