using Microsoft.AspNetCore.Http;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class NotificationBusiness : BaseBusiness, INotificationBusiness
    {
        public NotificationBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
        }

        public async Task<List<AppNotification>> GetByReceiverId(int receiverId, int limit = 20)
        {
            return await _unitOfWork.NotificationRep.GetByReceiverId(receiverId, limit);
        }

        public async Task<int> GetUnreadCount(int receiverId)
        {
            return await _unitOfWork.NotificationRep.GetUnreadCount(receiverId);
        }

        public async Task<bool> MarkAsRead(int id)
        {
            return await _unitOfWork.NotificationRep.MarkAsRead(id);
        }

        public async Task<bool> MarkAllAsRead(int receiverId)
        {
            return await _unitOfWork.NotificationRep.MarkAllAsRead(receiverId);
        }

        public async Task<bool> CreateNotification(int receiverId, string message, string? link = null, string? type = null, int? senderId = null)
        {
            var notification = new AppNotification
            {
                ReceiverId = receiverId,
                SenderId = senderId,
                Message = message,
                Link = link,
                Type = type,
                IsRead = false,
                CreateAt = DateTime.Now
            };
            return await _unitOfWork.NotificationRep.Add(notification);
        }
    }
}
