using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class CallLogRep : RepositoryBase<CallLog>, ICallLogRep
    {
        private const string CallLogsTableName = "dbo.CallLogs";

        public CallLogRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<int> InsertAsync(CallLog callLog)
        {
            if (callLog == null
                || string.IsNullOrWhiteSpace(callLog.EntityType)
                || callLog.EntityId <= 0
                || string.IsNullOrWhiteSpace(callLog.PhoneNumber))
            {
                return 0;
            }

            using var con = GetConnection();
            if (!await TableExistsAsync(con, CallLogsTableName))
            {
                return 0;
            }

            const string sql = @"
INSERT INTO dbo.CallLogs
(
    EntityType, EntityId, EmployeeId, EmployeeName, Extension, LineCode, PhoneNumber,
    Direction, ProviderCallId, StartTime, EndTime, Duration, Status, RecordingUrl,
    Outcome, Notes, SourcePage, ProviderRequestJson, ProviderResponseJson, ErrorMessage, NextFollowUpAt,
    Deleted, IsActive, CreatedBy, UpdatedBy, CreateAt, UpdateAt
)
VALUES
(
    @EntityType, @EntityId, @EmployeeId, @EmployeeName, @Extension, @LineCode, @PhoneNumber,
    @Direction, @ProviderCallId, @StartTime, @EndTime, @Duration, @Status, @RecordingUrl,
    @Outcome, @Notes, @SourcePage, @ProviderRequestJson, @ProviderResponseJson, @ErrorMessage, @NextFollowUpAt,
    0, 1, @CreatedBy, @UpdatedBy, GETDATE(), GETDATE()
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await con.ExecuteScalarAsync<int>(sql, callLog, commandType: CommandType.Text);
        }

        public async Task<bool> UpdateProviderResultAsync(int id, string status, string? providerCallId, string? providerResponseJson, string? errorMessage)
        {
            if (id <= 0)
            {
                return false;
            }

            using var con = GetConnection();
            if (!await TableExistsAsync(con, CallLogsTableName))
            {
                return false;
            }

            const string sql = @"
UPDATE dbo.CallLogs
SET Status = @Status,
    ProviderCallId = COALESCE(NULLIF(@ProviderCallId, ''), ProviderCallId),
    ProviderResponseJson = @ProviderResponseJson,
    ErrorMessage = @ErrorMessage,
    EndTime = CASE WHEN @Status IN ('failed', 'completed', 'missed') THEN COALESCE(EndTime, GETDATE()) ELSE EndTime END,
    UpdateAt = GETDATE()
WHERE Id = @Id
  AND ISNULL(Deleted, 0) = 0;";

            var affected = await con.ExecuteAsync(sql, new
            {
                Id = id,
                Status = status,
                ProviderCallId = providerCallId,
                ProviderResponseJson = providerResponseJson,
                ErrorMessage = errorMessage
            }, commandType: CommandType.Text);

            return affected > 0;
        }

        public async Task<bool> UpdateOutcomeAsync(CallLogOutcomeUpdate request, int userId)
        {
            if (request == null || request.Id <= 0)
            {
                return false;
            }

            using var con = GetConnection();
            if (!await TableExistsAsync(con, CallLogsTableName))
            {
                return false;
            }

            const string sql = @"
UPDATE dbo.CallLogs
SET Outcome = @Outcome,
    Notes = @Notes,
    NextFollowUpAt = @NextFollowUpAt,
    UpdatedBy = @UserId,
    UpdateAt = GETDATE()
WHERE Id = @Id
  AND ISNULL(Deleted, 0) = 0;";

            var affected = await con.ExecuteAsync(sql, new
            {
                request.Id,
                Outcome = request.Outcome?.Trim(),
                Notes = request.Notes?.Trim(),
                request.NextFollowUpAt,
                UserId = userId
            }, commandType: CommandType.Text);

            return affected > 0;
        }

        public async Task<List<CallLog>> GetByEntityAsync(string entityType, int entityId, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(entityType) || entityId <= 0)
            {
                return new List<CallLog>();
            }

            using var con = GetConnection();
            if (!await TableExistsAsync(con, CallLogsTableName))
            {
                return new List<CallLog>();
            }

            const string sql = @"
SELECT TOP (@Limit) *
FROM dbo.CallLogs
WHERE ISNULL(Deleted, 0) = 0
  AND EntityType = @EntityType
  AND EntityId = @EntityId
ORDER BY COALESCE(StartTime, CreateAt) DESC, Id DESC;";

            var result = await con.QueryAsync<CallLog>(sql, new
            {
                EntityType = entityType.Trim().ToUpperInvariant(),
                EntityId = entityId,
                Limit = Math.Clamp(limit, 1, 200)
            }, commandType: CommandType.Text);
            return result.ToList();
        }

        public async Task<List<CallLog>> GetRecentAsync(int limit = 10)
        {
            using var con = GetConnection();
            if (!await TableExistsAsync(con, CallLogsTableName))
            {
                return new List<CallLog>();
            }

            const string sql = @"
SELECT TOP (@Limit) *
FROM dbo.CallLogs
WHERE ISNULL(Deleted, 0) = 0
ORDER BY COALESCE(StartTime, CreateAt) DESC, Id DESC;";

            var result = await con.QueryAsync<CallLog>(sql, new { Limit = Math.Clamp(limit, 1, 100) });
            return result.ToList();
        }

        public async Task<TelephonyDashboardSnapshot> GetDashboardSnapshotAsync()
        {
            var snapshot = new TelephonyDashboardSnapshot();
            using var con = GetConnection();
            if (!await TableExistsAsync(con, CallLogsTableName))
            {
                return snapshot;
            }

            var todayStats = await con.QueryFirstOrDefaultAsync<TelephonyDashboardSnapshot>(@"
SELECT
    COUNT(1) AS TotalToday,
    COALESCE(SUM(CASE WHEN Status IN ('connected', 'completed') THEN 1 ELSE 0 END), 0) AS SuccessToday,
    COALESCE(SUM(CASE WHEN Status = 'failed' THEN 1 ELSE 0 END), 0) AS FailedToday,
    COALESCE(SUM(CASE WHEN Status = 'missed' THEN 1 ELSE 0 END), 0) AS MissedToday,
    COALESCE(SUM(CASE WHEN Outcome IN ('Hen goi lai', 'CallBack', 'callback') OR NextFollowUpAt IS NOT NULL THEN 1 ELSE 0 END), 0) AS FollowUpCount
FROM dbo.CallLogs
WHERE ISNULL(Deleted, 0) = 0
  AND CAST(COALESCE(StartTime, CreateAt) AS date) = CAST(GETDATE() AS date);");

            if (todayStats != null)
            {
                snapshot.TotalToday = todayStats.TotalToday;
                snapshot.SuccessToday = todayStats.SuccessToday;
                snapshot.FailedToday = todayStats.FailedToday;
                snapshot.MissedToday = todayStats.MissedToday;
                snapshot.FollowUpCount = todayStats.FollowUpCount;
            }

            snapshot.RecentCalls = await GetRecentAsync(8);
            return snapshot;
        }

        private static async Task<bool> TableExistsAsync(IDbConnection connection, string tableName)
        {
            const string sql = "SELECT CASE WHEN OBJECT_ID(@TableName, 'U') IS NULL THEN 0 ELSE 1 END";
            var exists = await connection.ExecuteScalarAsync<int>(sql, new { TableName = tableName });
            return exists == 1;
        }
    }
}
