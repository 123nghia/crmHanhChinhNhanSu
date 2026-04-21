using Dapper;
using Microsoft.Extensions.Configuration;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class NotificationRep : RepositoryBase<AppNotification>, INotificationRep
    {
        public NotificationRep(IConfiguration configuration)
            : base(configuration)
        {
            tableName = "AppNotifications";
        }

        public async Task<List<AppNotification>> GetByReceiverId(int receiverId, int limit = 20)
        {
            using (var con = GetConnection())
            {
                var sql = @"
                    SELECT TOP (@limit) *
                    FROM AppNotifications
                    WHERE ReceiverId = @receiverId
                    ORDER BY
                        CASE WHEN ISNULL(IsRead, 0) = 0 THEN 0 ELSE 1 END,
                        CASE WHEN ISNULL(Category, '') = 'ACTION_REQUIRED' THEN 0 ELSE 1 END,
                        CASE WHEN DueAt IS NULL THEN 1 ELSE 0 END,
                        DueAt ASC,
                        CreateAt DESC";
                var result = await con.QueryAsync<AppNotification>(sql, new { receiverId, limit });
                return result.ToList();
            }
        }

        public async Task<AppNotification?> GetById(int id, int receiverId)
        {
            using (var con = GetConnection())
            {
                var sql = @"
                    SELECT TOP 1 *
                    FROM AppNotifications
                    WHERE Id = @id AND ReceiverId = @receiverId";
                return await con.QueryFirstOrDefaultAsync<AppNotification>(sql, new { id, receiverId });
            }
        }

        public async Task<int> GetUnreadCount(int receiverId)
        {
            using (var con = GetConnection())
            {
                var sql = "SELECT COUNT(*) FROM AppNotifications WHERE ReceiverId = @receiverId AND ISNULL(IsRead, 0) = 0";
                return await con.ExecuteScalarAsync<int>(sql, new { receiverId });
            }
        }

        public async Task<bool> MarkAsRead(int id, int receiverId)
        {
            using (var con = GetConnection())
            {
                var sql = "UPDATE AppNotifications SET IsRead = 1 WHERE Id = @id AND ReceiverId = @receiverId";
                var affected = await con.ExecuteAsync(sql, new { id, receiverId });
                return affected > 0;
            }
        }

        public async Task<bool> MarkAllAsRead(int receiverId)
        {
            using (var con = GetConnection())
            {
                var sql = "UPDATE AppNotifications SET IsRead = 1 WHERE ReceiverId = @receiverId AND ISNULL(IsRead, 0) = 0";
                await con.ExecuteAsync(sql, new { receiverId });
                return true;
            }
        }

        public async Task<bool> Add(AppNotification item)
        {
            return await InsertAsync(item) > 0;
        }

        public async Task<int> InsertAsync(AppNotification item)
        {
            using (var con = GetConnection())
            {
                var sql = @"
DECLARE @HasCategory bit = CASE WHEN COL_LENGTH('dbo.AppNotifications', 'Category') IS NULL THEN 0 ELSE 1 END;

IF @HasCategory = 1
BEGIN
    INSERT INTO AppNotifications
    (
        ReceiverId, SenderId, Message, Link, Type, Category,
        RelatedEntityType, RelatedEntityId, EventCode, DueAt,
        IsRead, CreateAt
    )
    VALUES
    (
        @ReceiverId, @SenderId, @Message, @Link, @Type, @Category,
        @RelatedEntityType, @RelatedEntityId, @EventCode, @DueAt,
        0, GETDATE()
    );
END
ELSE
BEGIN
    INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type, IsRead, CreateAt)
    VALUES (@ReceiverId, @SenderId, @Message, @Link, @Type, 0, GETDATE());
END

SELECT CAST(SCOPE_IDENTITY() AS int);";
                return await con.ExecuteScalarAsync<int>(sql, item);
            }
        }
    }
}
