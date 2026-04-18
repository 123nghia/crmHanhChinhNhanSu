using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface INotificationRep
    {
        Task<List<AppNotification>> GetByReceiverId(int receiverId, int limit = 20);
        Task<int> GetUnreadCount(int receiverId);
        Task<bool> MarkAsRead(int id);
        Task<bool> MarkAllAsRead(int receiverId);
        Task<bool> Add(AppNotification item);
        Task<int> InsertAsync(AppNotification item);
    }
}
