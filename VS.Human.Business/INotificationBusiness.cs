using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface INotificationBusiness
    {
        Task<List<AppNotification>> GetByReceiverId(int receiverId, int limit = 20);
        Task<AppNotification?> GetById(int id, int receiverId);
        Task<int> GetUnreadCount(int receiverId);
        Task<bool> MarkAsRead(int id, int receiverId);
        Task<bool> MarkAllAsRead(int receiverId);
        Task<bool> CreateNotification(int receiverId, string message, string? link = null, string? type = null, int? senderId = null);
        Task<int> CreateNotificationWithId(AppNotification notification);
    }
}
