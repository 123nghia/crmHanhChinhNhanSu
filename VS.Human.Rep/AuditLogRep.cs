using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class AuditLogRep : RepositoryBase<AuditLog>, IAuditLogRep
    {
        public AuditLogRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<int> InsertAsync(AuditLog record)
        {
            var p = new DynamicParameters();
            p.Add("@UserId", record.UserId);
            p.Add("@UserName", record.UserName);
            p.Add("@FullName", record.FullName);
            p.Add("@RoleCode", record.RoleCode);
            p.Add("@Action", record.Action);
            p.Add("@Path", record.Path);
            p.Add("@QueryString", record.QueryString);
            p.Add("@Payload", record.Payload);
            p.Add("@StatusCode", record.StatusCode);
            p.Add("@DurationMs", record.DurationMs);
            p.Add("@ClientIp", record.ClientIp);
            p.Add("@UserAgent", record.UserAgent);

            using (var con = GetConnection())
            {
                return await con.ExecuteScalarAsync<int>(
                    "sp_AuditLog_Insert",
                    p,
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        public async Task<AuditLogStats> GetTodayStatsAsync(DateTime start, DateTime end)
        {
            const string sql = @"
SELECT
    COUNT(1) AS TotalRequests,
    COUNT(DISTINCT CASE
        WHEN UserId IS NOT NULL THEN CONCAT('U', UserId)
        WHEN ClientIp IS NOT NULL THEN CONCAT('I', ClientIp)
        WHEN UserAgent IS NOT NULL THEN CONCAT('A', UserAgent)
        ELSE NULL
    END) AS TotalVisits
FROM AuditLog
WHERE ISNULL(Deleted, 0) = 0
  AND CreateAt >= @Start
  AND CreateAt < @End;
";

            using (var con = GetConnection())
            {
                var result = await con.QuerySingleOrDefaultAsync<AuditLogStats>(sql, new
                {
                    Start = start,
                    End = end
                });

                return result ?? new AuditLogStats();
            }
        }
    }
}
