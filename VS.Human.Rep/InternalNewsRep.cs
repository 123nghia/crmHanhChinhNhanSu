using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Linq;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class InternalNewsRep : RepositoryBase<InternalNewsItem>, IInternalNewsRep
    {
        public InternalNewsRep(IConfiguration configuration)
            : base(configuration)
        {
            tableName = "InternalNews";
            sqlGetALl = "sp_InternalNews_getAll";
        }

        public async Task<bool> AddOrUpdate(InternalNewsItem item)
        {
            if (item == null)
            {
                return false;
            }

            using var con = GetConnection();
            using var tran = con.BeginTransaction();
            try
            {
                var hasDirectRecipientEmailsColumn = await ColumnExistsAsync(con, tran, "dbo.InternalNews", "DirectRecipientEmails");
                var hasInternalNewsMailGroupsTable = await TableExistsAsync(con, tran, "dbo.InternalNewsMailGroups");

                if (item.Id > 0)
                {
                    var updateSql = hasDirectRecipientEmailsColumn
                        ? @"
UPDATE InternalNews
SET Title = @Title,
    Content = @Content,
    IsSendMail = @IsSendMail,
    DirectRecipientEmails = @DirectRecipientEmails,
    UpdatedBy = @UpdatedBy,
    UpdateAt = GETDATE()
WHERE Id = @Id
  AND ISNULL(Deleted, 0) = 0;"
                        : @"
UPDATE InternalNews
SET Title = @Title,
    Content = @Content,
    IsSendMail = @IsSendMail,
    UpdatedBy = @UpdatedBy,
    UpdateAt = GETDATE()
WHERE Id = @Id
  AND ISNULL(Deleted, 0) = 0;";

                    var affected = await con.ExecuteAsync(updateSql, new
                    {
                        item.Id,
                        item.Title,
                        item.Content,
                        item.IsSendMail,
                        item.DirectRecipientEmails,
                        item.UpdatedBy
                    }, tran);

                    if (affected <= 0)
                    {
                        tran.Rollback();
                        return false;
                    }
                }
                else
                {
                    var insertSql = hasDirectRecipientEmailsColumn
                        ? @"
INSERT INTO InternalNews
(
    Title,
    Content,
    IsSendMail,
    DirectRecipientEmails,
    Deleted,
    IsActive,
    CreatedBy,
    UpdatedBy,
    CreateAt,
    UpdateAt
)
VALUES
(
    @Title,
    @Content,
    @IsSendMail,
    @DirectRecipientEmails,
    0,
    1,
    @CreatedBy,
    @UpdatedBy,
    GETDATE(),
    GETDATE()
);
SELECT CAST(SCOPE_IDENTITY() AS int);"
                        : @"
INSERT INTO InternalNews
(
    Title,
    Content,
    IsSendMail,
    Deleted,
    IsActive,
    CreatedBy,
    UpdatedBy,
    CreateAt,
    UpdateAt
)
VALUES
(
    @Title,
    @Content,
    @IsSendMail,
    0,
    1,
    @CreatedBy,
    @UpdatedBy,
    GETDATE(),
    GETDATE()
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                    item.Id = await con.ExecuteScalarAsync<int>(insertSql, new
                    {
                        item.Title,
                        item.Content,
                        item.IsSendMail,
                        item.DirectRecipientEmails,
                        item.CreatedBy,
                        item.UpdatedBy
                    }, tran);

                    if (item.Id <= 0)
                    {
                        tran.Rollback();
                        return false;
                    }
                }

                if (item.Attachments != null && item.Attachments.Count > 0)
                {
                    const string attachmentSql = @"
INSERT INTO InternalNewsAttachment
(
    NewsId,
    FileName,
    FilePath,
    FileSize,
    Deleted,
    IsActive,
    CreatedBy,
    UpdatedBy,
    CreateAt,
    UpdateAt
)
VALUES
(
    @NewsId,
    @FileName,
    @FilePath,
    @FileSize,
    0,
    1,
    @CreatedBy,
    @UpdatedBy,
    GETDATE(),
    GETDATE()
);";

                    foreach (var attachment in item.Attachments.Where(x => x != null && !string.IsNullOrWhiteSpace(x.FilePath)))
                    {
                        await con.ExecuteAsync(attachmentSql, new
                        {
                            NewsId = item.Id,
                            attachment.FileName,
                            attachment.FilePath,
                            attachment.FileSize,
                            CreatedBy = item.UpdatedBy > 0 ? item.UpdatedBy : item.CreatedBy,
                            UpdatedBy = item.UpdatedBy > 0 ? item.UpdatedBy : item.CreatedBy
                        }, tran);
                    }
                }

                if (hasInternalNewsMailGroupsTable)
                {
                    await con.ExecuteAsync("DELETE FROM InternalNewsMailGroups WHERE NewsId = @NewsId", new { NewsId = item.Id }, tran);
                    if (item.MailGroupIds != null && item.MailGroupIds.Count > 0)
                    {
                        const string groupSql = @"
INSERT INTO InternalNewsMailGroups
(
    NewsId,
    MailGroupId,
    Deleted,
    IsActive,
    CreatedBy,
    UpdatedBy,
    CreateAt,
    UpdateAt
)
VALUES
(
    @NewsId,
    @MailGroupId,
    0,
    1,
    @CreatedBy,
    @UpdatedBy,
    GETDATE(),
    GETDATE()
);";

                        foreach (var groupId in item.MailGroupIds.Where(x => x > 0).Distinct())
                        {
                            await con.ExecuteAsync(groupSql, new
                            {
                                NewsId = item.Id,
                                MailGroupId = groupId,
                                CreatedBy = item.UpdatedBy > 0 ? item.UpdatedBy : item.CreatedBy,
                                UpdatedBy = item.UpdatedBy > 0 ? item.UpdatedBy : item.CreatedBy
                            }, tran);
                        }
                    }
                }

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                return false;
            }
        }

        public async Task<BaseList> GetAll(InternalNewsRequest request)
        {
            return await GetBaseAll<InternalNewsIndexModel>(request, new
            {
                Token = request.Token ?? string.Empty,
                request.OrderBy,
                request.Page,
                request.Limit,
                request.From,
                request.To
            }, "sp_InternalNews_getAll");
        }

        public async Task<InternalNewsItem> GetById(int id)
        {
            using var con = GetConnection();
            const string sql = @"
SELECT TOP 1
    d.*,
    u.FullName AS AuthorName
FROM InternalNews d
LEFT JOIN Employees u ON d.CreatedBy = u.Id
WHERE d.Id = @Id
  AND ISNULL(d.Deleted, 0) = 0;";

            var result = await con.QueryFirstOrDefaultAsync<InternalNewsItem>(sql, new { Id = id });
            if (result == null)
            {
                return new InternalNewsItem { Id = -1 };
            }

            const string attachmentSql = @"
SELECT *
FROM InternalNewsAttachment
WHERE NewsId = @NewsId
  AND ISNULL(Deleted, 0) = 0
ORDER BY Id;";

            const string groupSql = @"
SELECT MailGroupId
FROM InternalNewsMailGroups
WHERE NewsId = @NewsId
  AND ISNULL(Deleted, 0) = 0;";

            try
            {
                var attachments = await con.QueryAsync<InternalNewsAttachment>(attachmentSql, new { NewsId = result.Id });
                result.Attachments = attachments?.ToList() ?? new List<InternalNewsAttachment>();
            }
            catch (Exception)
            {
                result.Attachments = new List<InternalNewsAttachment>();
            }

            try
            {
                if (await TableExistsAsync(con, null, "dbo.InternalNewsMailGroups"))
                {
                    var mailGroups = await con.QueryAsync<int>(groupSql, new { NewsId = result.Id });
                    result.MailGroupIds = mailGroups?.ToList() ?? new List<int>();
                }
                else
                {
                    result.MailGroupIds = new List<int>();
                }
            }
            catch (Exception)
            {
                result.MailGroupIds = new List<int>();
            }

            return result;
        }

        public Task<bool> Delete(int id)
        {
            return DeleteBase(id, tableDelete: tableName);
        }

        private static async Task<bool> TableExistsAsync(IDbConnection connection, IDbTransaction? transaction, string tableName)
        {
            const string sql = "SELECT CASE WHEN OBJECT_ID(@TableName, 'U') IS NULL THEN 0 ELSE 1 END";
            var exists = await connection.ExecuteScalarAsync<int>(sql, new { TableName = tableName }, transaction);
            return exists == 1;
        }

        private static async Task<bool> ColumnExistsAsync(IDbConnection connection, IDbTransaction? transaction, string tableName, string columnName)
        {
            const string sql = "SELECT CASE WHEN COL_LENGTH(@TableName, @ColumnName) IS NULL THEN 0 ELSE 1 END";
            var exists = await connection.ExecuteScalarAsync<int>(sql, new { TableName = tableName, ColumnName = columnName }, transaction);
            return exists == 1;
        }
    }
}
