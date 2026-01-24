using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class LogHistoryRep : RepositoryBase<LogHistory>, ILogHistoryRep
    {
        public LogHistoryRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<int> InsertAsync(LogHistory record)
        {
            var p = new DynamicParameters();
            p.Add("@UserId", record.UserId);
            p.Add("@UserName", record.UserName);
            p.Add("@FullName", record.FullName);
            p.Add("@RoleCode", record.RoleCode);
            p.Add("@LoginAt", record.LoginAt);
            p.Add("@Source", record.Source);
            p.Add("@ClientIp", record.ClientIp);
            p.Add("@UserAgent", record.UserAgent);

            using (var con = GetConnection())
            {
                return await con.ExecuteScalarAsync<int>(
                    "sp_LogHistory_Insert",
                    p,
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        public async Task<bool> UpdateLogoutAsync(int userId, string? roleCode, int? logId = null, DateTime? logoutAt = null)
        {
            var p = new DynamicParameters();
            p.Add("@LogId", logId);
            p.Add("@UserId", userId);
            p.Add("@RoleCode", roleCode);
            p.Add("@LogoutAt", logoutAt);

            return await ExecuteSQL("sp_LogHistory_update", p, CommandType.StoredProcedure);
        }
    }
}
