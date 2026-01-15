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
                    ORDER BY CreateAt DESC";
                var result = await con.QueryAsync<AppNotification>(sql, new { receiverId, limit });
                return result.ToList();
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

        public async Task<bool> MarkAsRead(int id)
        {
            using (var con = GetConnection())
            {
                var sql = "UPDATE AppNotifications SET IsRead = 1 WHERE Id = @id";
                var affected = await con.ExecuteAsync(sql, new { id });
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
            using (var con = GetConnection())
            {
                var sql = @"
                    INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type, IsRead, CreateAt)
                    VALUES (@ReceiverId, @SenderId, @Message, @Link, @Type, 0, GETDATE())";
                var affected = await con.ExecuteAsync(sql, item);
                return affected > 0;
            }
        }
    }
}
