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
                if (item.Id > 0)
                {
                    const string updateSql = @"
UPDATE InternalNews
SET Title = @Title,
    Content = @Content,
    IsSendMail = @IsSendMail,
    DirectRecipientEmails = @DirectRecipientEmails,
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
                    const string insertSql = @"
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

            result.Attachments = (await con.QueryAsync<InternalNewsAttachment>(attachmentSql, new { NewsId = result.Id })).ToList();
            result.MailGroupIds = (await con.QueryAsync<int>(groupSql, new { NewsId = result.Id })).ToList();
            return result;
        }

        public Task<bool> Delete(int id)
        {
            return DeleteBase(id, tableDelete: tableName);
        }
    }
}
