using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Linq;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class EmailSentLogRep : RepositoryBase<EmailSentLog>, IEmailSentLogRep
    {
        public EmailSentLogRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<int> InsertAsync(EmailSentLog log)
        {
            const string sql = @"
INSERT INTO EmailSentLogs
(
    TemplateId, TemplateCode, SenderType, FromEmail, FromName,
    ToEmails, CcEmails, BccEmails, Subject, BodyHtml,
    SendSuccess, ErrorMessage, TriggeredByUserId, TriggeredByUserName, TriggeredByFullName,
    SenderEmployeeId, ManagerId, MessageId, Deleted, IsActive, CreatedBy, UpdatedBy, CreateAt, UpdateAt
)
VALUES
(
    @TemplateId, @TemplateCode, @SenderType, @FromEmail, @FromName,
    @ToEmails, @CcEmails, @BccEmails, @Subject, @BodyHtml,
    @SendSuccess, @ErrorMessage, @TriggeredByUserId, @TriggeredByUserName, @TriggeredByFullName,
    @SenderEmployeeId, @ManagerId, @MessageId, 0, 1, @CreatedBy, @UpdatedBy, GETDATE(), GETDATE()
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            using var con = GetConnection();
            return await con.ExecuteScalarAsync<int>(sql, log);
        }

        public async Task<BaseList> GetLogsAsync(EmailSentLogRequest request)
        {
            request ??= new EmailSentLogRequest();
            var page = request.Page;
            var limit = request.Limit;
            ProcessInputPaging(ref page, ref limit, out var offset);
            request.Page = page;
            request.Limit = limit;

            var sql = @"
DECLARE @CanViewAll bit = CASE WHEN @RoleCode IN ('1', '8', '9') THEN 1 ELSE 0 END;

WITH Filtered AS
(
    SELECT
        l.Id,
        l.TemplateId,
        l.TemplateCode,
        t.Name AS TemplateName,
        l.SenderType,
        l.FromEmail,
        l.FromName,
        l.ToEmails,
        l.CcEmails,
        l.BccEmails,
        l.Subject,
        l.SendSuccess,
        l.ErrorMessage,
        l.TriggeredByUserId,
        l.TriggeredByUserName,
        l.TriggeredByFullName,
        COALESCE(NULLIF(l.TriggeredByFullName, ''), NULLIF(l.TriggeredByUserName, ''), NULLIF(e.FullName, ''), NULLIF(e.UserName, ''), CAST(l.TriggeredByUserId AS nvarchar(20))) AS TriggeredByDisplay,
        l.SenderEmployeeId,
        l.ManagerId,
        l.MessageId,
        l.CreateAt,
        l.UpdateAt,
        l.CreatedBy,
        l.UpdatedBy,
        l.Deleted,
        l.IsActive
    FROM EmailSentLogs l
    LEFT JOIN EmailTemplates t ON t.Id = l.TemplateId
    LEFT JOIN Employees e ON e.Id = l.TriggeredByUserId
    WHERE ISNULL(l.Deleted, 0) = 0
      AND (
            @CanViewAll = 1
            OR (ISNULL(@UserId, 0) > 0 AND (l.TriggeredByUserId = @UserId OR l.SenderEmployeeId = @UserId))
          )
      AND (
            ISNULL(@Token, '') = ''
            OR ISNULL(l.TemplateCode, '') LIKE @LikeToken
            OR ISNULL(t.Name, '') LIKE @LikeToken
            OR ISNULL(l.Subject, '') LIKE @LikeToken
            OR ISNULL(l.ToEmails, '') LIKE @LikeToken
            OR ISNULL(l.FromEmail, '') LIKE @LikeToken
            OR ISNULL(l.TriggeredByFullName, '') LIKE @LikeToken
            OR ISNULL(l.TriggeredByUserName, '') LIKE @LikeToken
          )
      AND (ISNULL(@TemplateCode, '') = '' OR ISNULL(l.TemplateCode, '') = @TemplateCode)
      AND (
            @SendStatus IS NULL
            OR @SendStatus < 0
            OR CASE WHEN ISNULL(l.SendSuccess, 0) = 1 THEN 1 ELSE 0 END = @SendStatus
          )
      AND (@From IS NULL OR l.CreateAt >= @From)
      AND (@To IS NULL OR l.CreateAt <= @To)
)
SELECT COUNT(1) FROM Filtered;

SELECT *
FROM Filtered
ORDER BY CreateAt DESC, Id DESC
OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;";

            var param = new
            {
                request.UserId,
                request.RoleCode,
                request.Token,
                LikeToken = "%" + (request.Token ?? string.Empty).Trim() + "%",
                TemplateCode = (request.TemplateCode ?? string.Empty).Trim(),
                request.SendStatus,
                request.From,
                request.To,
                Offset = offset,
                request.Limit
            };

            using var con = GetConnection();
            using var multi = await con.QueryMultipleAsync(sql, param, commandType: CommandType.Text);
            var total = await multi.ReadFirstAsync<int>();
            var data = (await multi.ReadAsync<EmailSentLog>()).ToList();

            return new BaseList
            {
                Total = total,
                Data = data
            };
        }

        public async Task<EmailSentLog?> GetByIdAsync(int id, int userId, string? roleCode)
        {
            const string sql = @"
DECLARE @CanViewAll bit = CASE WHEN @RoleCode IN ('1', '8', '9') THEN 1 ELSE 0 END;

SELECT TOP 1
    l.Id,
    l.TemplateId,
    l.TemplateCode,
    t.Name AS TemplateName,
    l.SenderType,
    l.FromEmail,
    l.FromName,
    l.ToEmails,
    l.CcEmails,
    l.BccEmails,
    l.Subject,
    l.BodyHtml,
    l.SendSuccess,
    l.ErrorMessage,
    l.TriggeredByUserId,
    l.TriggeredByUserName,
    l.TriggeredByFullName,
    COALESCE(NULLIF(l.TriggeredByFullName, ''), NULLIF(l.TriggeredByUserName, ''), NULLIF(e.FullName, ''), NULLIF(e.UserName, ''), CAST(l.TriggeredByUserId AS nvarchar(20))) AS TriggeredByDisplay,
    l.SenderEmployeeId,
    l.ManagerId,
    l.MessageId,
    l.CreateAt,
    l.UpdateAt,
    l.CreatedBy,
    l.UpdatedBy,
    l.Deleted,
    l.IsActive
FROM EmailSentLogs l
LEFT JOIN EmailTemplates t ON t.Id = l.TemplateId
LEFT JOIN Employees e ON e.Id = l.TriggeredByUserId
WHERE l.Id = @Id
  AND ISNULL(l.Deleted, 0) = 0
  AND (
        @CanViewAll = 1
        OR (ISNULL(@UserId, 0) > 0 AND (l.TriggeredByUserId = @UserId OR l.SenderEmployeeId = @UserId))
      );";

            using var con = GetConnection();
            return await con.QueryFirstOrDefaultAsync<EmailSentLog>(sql, new
            {
                Id = id,
                UserId = userId,
                RoleCode = roleCode
            });
        }
    }
}
