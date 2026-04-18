using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class WorkflowTimelineRep : RepositoryBase<WorkflowTimelineEvent>, IWorkflowTimelineRep
    {
        private const string TimelineTableName = "dbo.WorkflowTimelineEvents";

        public WorkflowTimelineRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<int> AddAsync(WorkflowTimelineEvent timelineEvent)
        {
            if (timelineEvent == null
                || string.IsNullOrWhiteSpace(timelineEvent.EntityType)
                || timelineEvent.EntityId <= 0
                || string.IsNullOrWhiteSpace(timelineEvent.EventCode))
            {
                return 0;
            }

            using var con = GetConnection();
            if (!await TableExistsAsync(con, TimelineTableName))
            {
                return 0;
            }

            const string sql = @"
INSERT INTO dbo.WorkflowTimelineEvents
(
    EntityType, EntityId, EventCode, EventTitle, CurrentStatus, CurrentStatusText,
    TriggeredBy, TriggeredByName, AssignedTo, AssignedToName, OccurredAt, DueAt, SlaHours,
    EmailSent, EmailFailed, EmailLogId, NotificationCreated, NotificationId,
    AttendanceSyncStatus, ErrorMessage, MetadataJson,
    Deleted, IsActive, CreatedBy, UpdatedBy, CreateAt, UpdateAt
)
VALUES
(
    @EntityType, @EntityId, @EventCode, @EventTitle, @CurrentStatus, @CurrentStatusText,
    @TriggeredBy, @TriggeredByName, @AssignedTo, @AssignedToName, ISNULL(@OccurredAt, GETDATE()), @DueAt, @SlaHours,
    @EmailSent, @EmailFailed, @EmailLogId, @NotificationCreated, @NotificationId,
    @AttendanceSyncStatus, @ErrorMessage, @MetadataJson,
    0, 1, @CreatedBy, @UpdatedBy, GETDATE(), GETDATE()
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await con.ExecuteScalarAsync<int>(sql, timelineEvent, commandType: CommandType.Text);
        }

        public async Task<List<WorkflowTimelineEvent>> GetByEntityAsync(string entityType, int entityId)
        {
            if (string.IsNullOrWhiteSpace(entityType) || entityId <= 0)
            {
                return new List<WorkflowTimelineEvent>();
            }

            using var con = GetConnection();
            if (!await TableExistsAsync(con, TimelineTableName))
            {
                return new List<WorkflowTimelineEvent>();
            }

            const string sql = @"
SELECT TOP 200 *
FROM dbo.WorkflowTimelineEvents
WHERE ISNULL(Deleted, 0) = 0
  AND EntityType = @EntityType
  AND EntityId = @EntityId
ORDER BY OccurredAt ASC, Id ASC;";

            var result = await con.QueryAsync<WorkflowTimelineEvent>(sql, new
            {
                EntityType = entityType.Trim().ToUpperInvariant(),
                EntityId = entityId
            });
            return result.ToList();
        }

        public async Task<WorkflowActionCenterSummary> GetActionCenterAsync(int userId, string? roleCode)
        {
            var summary = new WorkflowActionCenterSummary();
            if (userId <= 0)
            {
                return summary;
            }

            using var con = GetConnection();
            var normalizedRole = (roleCode ?? string.Empty).Trim();
            var canViewAll = normalizedRole is "1" or "8" or "9";
            var canApproveLeave = normalizedRole is "1" or "3" or "8" or "9";

            summary.MyTasks = (await con.QueryAsync<WorkflowTaskItem>(@"
WITH LeaveOwners AS
(
    SELECT
        l.Id,
        l.EmployeeId,
        l.Status,
        l.CreateAt,
        l.UpdateAt,
        COALESCE(
            CASE
                WHEN l.Status = 0 AND ISNULL(e.ManagerId, 0) > 0 AND e.ManagerId <> l.EmployeeId
                    THEN e.ManagerId
            END,
            CASE WHEN l.Status = 1 THEN @HcnsVirtualOwner END,
            CASE WHEN l.Status = 2 THEN @BgdVirtualOwner END
        ) AS OwnerId,
        CASE
            WHEN l.Status = 0 THEN COALESCE(NULLIF(managerEmp.FullName, ''), NULLIF(managerEmp.UserName, ''), N'Quan ly truc tiep')
            WHEN l.Status = 1 THEN N'Phong HCNS'
            WHEN l.Status = 2 THEN N'Ban Giam doc'
            ELSE NULL
        END AS OwnerName,
        CASE WHEN l.Status = 0 THEN 24 WHEN l.Status = 1 THEN 12 WHEN l.Status = 2 THEN 24 ELSE NULL END AS SlaHours
    FROM dbo.LeaveRequests l
    INNER JOIN dbo.Employees e ON e.Id = l.EmployeeId
    LEFT JOIN dbo.Employees managerEmp ON managerEmp.Id =
        CASE
            WHEN ISNULL(e.ManagerId, 0) > 0 AND e.ManagerId <> l.EmployeeId
                THEN e.ManagerId
        END
    WHERE ISNULL(l.Deleted, 0) = 0
      AND l.Status IN (0, 1, 2)
)
SELECT TOP 20
    'LEAVE' AS EntityType,
    l.Id AS EntityId,
    CONCAT('LEAVE-', l.Id) AS RequestCode,
    CONCAT(N'Nghi phep: ', COALESCE(NULLIF(e.FullName, ''), e.UserName, CONCAT(N'NV ', e.Id))) AS Title,
    COALESCE(NULLIF(e.FullName, ''), e.UserName, CONCAT(N'NV ', e.Id)) AS EmployeeName,
    l.Status,
    CASE l.Status
        WHEN 0 THEN N'Cho quan ly duyet'
        WHEN 1 THEN N'Cho HCNS duyet'
        WHEN 2 THEN N'Cho BGD duyet'
        ELSE N'Dang cho xu ly'
    END AS StatusText,
    CASE l.Status
        WHEN 0 THEN N'Manager'
        WHEN 1 THEN N'HCNS'
        WHEN 2 THEN N'BGD'
        ELSE N'Unknown'
    END AS CurrentStep,
    owners.OwnerId AS CurrentOwnerId,
    owners.OwnerName AS CurrentOwnerName,
    l.CreateAt AS CreatedAt,
    COALESCE(lastHistory.ActionTime, l.UpdateAt, l.CreateAt) AS StepStartedAt,
    DATEADD(hour, owners.SlaHours, COALESCE(lastHistory.ActionTime, l.UpdateAt, l.CreateAt)) AS DueAt,
    DATEDIFF(hour, COALESCE(lastHistory.ActionTime, l.UpdateAt, l.CreateAt), GETDATE()) AS WaitingHours,
    CASE
        WHEN DATEADD(hour, owners.SlaHours, COALESCE(lastHistory.ActionTime, l.UpdateAt, l.CreateAt)) < GETDATE() THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS IsOverdue,
    CONCAT('/Leave/LeaveApproval?id=', l.Id) AS Url
FROM dbo.LeaveRequests l
INNER JOIN dbo.Employees e ON e.Id = l.EmployeeId
INNER JOIN LeaveOwners owners ON owners.Id = l.Id
OUTER APPLY
(
    SELECT TOP 1 h.ActionTime
    FROM dbo.LeaveHistories h
    WHERE h.LeaveId = l.Id
    ORDER BY h.ActionTime DESC, h.Id DESC
) lastHistory
WHERE ISNULL(l.Deleted, 0) = 0
  AND l.Status IN (0, 1, 2)
  AND l.EmployeeId <> @UserId
  AND (
        (@RoleCode = '1')
        OR (@RoleCode = '3' AND owners.OwnerId = @UserId)
        OR (@RoleCode = '9' AND l.Status = 1)
        OR (@RoleCode = '8' AND l.Status = 2)
      )
ORDER BY IsOverdue DESC, DueAt ASC, l.Id DESC;",
                new
                {
                    UserId = userId,
                    RoleCode = normalizedRole,
                    HcnsVirtualOwner = -9,
                    BgdVirtualOwner = -8
                })).ToList();

            if (canApproveLeave)
            {
                summary.WorkflowHealth = (await con.QueryAsync<WorkflowHealthItem>(@"
WITH Pending AS
(
    SELECT
        CASE Status WHEN 0 THEN 'MANAGER' WHEN 1 THEN 'HCNS' WHEN 2 THEN 'BGD' END AS StepCode,
        CASE Status WHEN 0 THEN N'Manager' WHEN 1 THEN N'HCNS' WHEN 2 THEN N'BGD' END AS StepName,
        CASE Status WHEN 0 THEN 24 WHEN 1 THEN 12 WHEN 2 THEN 24 ELSE 24 END AS SlaHours,
        COALESCE(lastHistory.ActionTime, l.UpdateAt, l.CreateAt) AS StepStartedAt
    FROM dbo.LeaveRequests l
    OUTER APPLY
    (
        SELECT TOP 1 h.ActionTime
        FROM dbo.LeaveHistories h
        WHERE h.LeaveId = l.Id
        ORDER BY h.ActionTime DESC, h.Id DESC
    ) lastHistory
    WHERE ISNULL(l.Deleted, 0) = 0
      AND l.Status IN (0, 1, 2)
)
SELECT
    StepCode,
    StepName,
    COUNT(1) AS PendingCount,
    SUM(CASE WHEN DATEADD(hour, SlaHours, StepStartedAt) < GETDATE() THEN 1 ELSE 0 END) AS OverdueCount
FROM Pending
GROUP BY StepCode, StepName
ORDER BY CASE StepCode WHEN 'MANAGER' THEN 1 WHEN 'HCNS' THEN 2 WHEN 'BGD' THEN 3 ELSE 9 END;")).ToList();
            }

            summary.SystemIssues = (await con.QueryAsync<WorkflowSystemIssue>(@"
SELECT
    'ATTENDANCE_SYNC_FAILED' AS IssueType,
    N'Approved leave attendance sync failed' AS Title,
    COUNT(1) AS Count,
    '/Leave/LeaveApproval?issue=attendance-sync' AS Url,
    'danger' AS Severity
FROM dbo.LeaveRequests
WHERE ISNULL(Deleted, 0) = 0
  AND Status IN (3, 4)
  AND ISNULL(AttendanceSyncStatus, '') = 'Failed'
HAVING COUNT(1) > 0
UNION ALL
SELECT
    'ATTENDANCE_SYNC_PENDING' AS IssueType,
    N'Approved leave waiting attendance sync' AS Title,
    COUNT(1) AS Count,
    '/Leave/LeaveApproval?issue=attendance-sync-pending' AS Url,
    'warning' AS Severity
FROM dbo.LeaveRequests
WHERE ISNULL(Deleted, 0) = 0
  AND Status IN (3, 4)
  AND ISNULL(AttendanceSyncStatus, '') = 'Pending'
HAVING COUNT(1) > 0;")).ToList();

            if (await TableExistsAsync(con, "dbo.EmailSentLogs"))
            {
                var emailIssues = await con.QueryAsync<WorkflowSystemIssue>(@"
SELECT
    'EMAIL_FAILED' AS IssueType,
    N'Email sending failed in last 7 days' AS Title,
    COUNT(1) AS Count,
    '/System/MailGroup/EmailLogs?sendStatus=0' AS Url,
    'warning' AS Severity
FROM dbo.EmailSentLogs
WHERE ISNULL(Deleted, 0) = 0
  AND ISNULL(SendSuccess, 0) = 0
  AND CreateAt >= DATEADD(day, -7, GETDATE())
HAVING COUNT(1) > 0;");
                summary.SystemIssues.AddRange(emailIssues);
            }

            if (await TableExistsAsync(con, TimelineTableName))
            {
                summary.RecentTimeline = (await con.QueryAsync<WorkflowTimelineEvent>(@"
SELECT TOP 15 *
FROM dbo.WorkflowTimelineEvents
WHERE ISNULL(Deleted, 0) = 0
  AND (
        @CanViewAll = 1
        OR TriggeredBy = @UserId
        OR AssignedTo = @UserId
      )
ORDER BY OccurredAt DESC, Id DESC;", new
                {
                    UserId = userId,
                    CanViewAll = canViewAll
                })).ToList();
            }

            return summary;
        }

        private static async Task<bool> TableExistsAsync(IDbConnection connection, string tableName)
        {
            const string sql = "SELECT CASE WHEN OBJECT_ID(@TableName, 'U') IS NULL THEN 0 ELSE 1 END";
            var exists = await connection.ExecuteScalarAsync<int>(sql, new { TableName = tableName });
            return exists == 1;
        }
    }
}
