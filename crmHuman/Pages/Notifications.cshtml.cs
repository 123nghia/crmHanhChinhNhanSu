using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VS.Human.Business;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    public class NotificationsModel : BaseModel2
    {
        private readonly INotificationBusiness _notificationBusiness;

        public NotificationsModel(INotificationBusiness notificationBusiness)
        {
            _notificationBusiness = notificationBusiness;
            TitlePage = "Thong bao";
        }

        public List<AppNotification> Notifications { get; set; } = new List<AppNotification>();
        public string CurrentFilter { get; set; } = "all";
        public int VisibleTotalCount { get; set; }
        public int VisibleUnreadCount { get; set; }
        public int VisibleActionRequiredCount { get; set; }
        public int VisibleNewsCount { get; set; }
        public int VisibleFeedbackCount { get; set; }

        public async Task<IActionResult> OnGetAsync(string? filter = null)
        {
            GetInfoUser();
            if (UserData?.UserId <= 0)
            {
                return Redirect("/Login");
            }

            await LoadNotificationsAsync(filter);
            return Page();
        }

        public async Task<IActionResult> OnGetOpenAsync(int id, string? filter = null)
        {
            GetInfoUser();
            if (UserData?.UserId <= 0)
            {
                return Redirect("/Login");
            }

            var notification = await _notificationBusiness.GetById(id, UserData.UserId);
            if (notification == null || !CanViewNotification(notification))
            {
                return RedirectToPage(new { filter = NormalizeFilter(filter) });
            }

            if (!notification.IsRead)
            {
                await _notificationBusiness.MarkAsRead(id, UserData.UserId);
            }

            if (!string.IsNullOrWhiteSpace(notification.Link))
            {
                return Redirect(notification.Link);
            }

            return RedirectToPage(new { filter = NormalizeFilter(filter) });
        }

        public async Task<IActionResult> OnPostMarkReadAsync(int id, string? filter = null)
        {
            GetInfoUser();
            if (UserData?.UserId <= 0)
            {
                return Redirect("/Login");
            }

            await _notificationBusiness.MarkAsRead(id, UserData.UserId);
            return RedirectToPage(new { filter = NormalizeFilter(filter) });
        }

        public async Task<IActionResult> OnPostMarkAllReadAsync(string? filter = null)
        {
            GetInfoUser();
            if (UserData?.UserId <= 0)
            {
                return Redirect("/Login");
            }

            await _notificationBusiness.MarkAllAsRead(UserData.UserId);
            return RedirectToPage(new { filter = NormalizeFilter(filter) });
        }

        public bool IsActionRequired(AppNotification notification)
        {
            return string.Equals(notification?.Category, "ACTION_REQUIRED", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsInternalNews(AppNotification notification)
        {
            if (notification == null)
            {
                return false;
            }

            return string.Equals(notification.RelatedEntityType, "INTERNAL_NEWS", StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(notification.Link) && notification.Link.StartsWith("/InternalNews", StringComparison.OrdinalIgnoreCase));
        }

        public bool IsFeedback(AppNotification notification)
        {
            return notification != null && !IsActionRequired(notification) && !IsInternalNews(notification);
        }

        public bool IsOverdue(AppNotification notification)
        {
            return notification?.DueAt.HasValue == true && notification.DueAt.Value < DateTime.Now && !notification.IsRead;
        }

        public string GetCategoryLabel(AppNotification notification)
        {
            if (IsActionRequired(notification))
            {
                return "Can xu ly";
            }

            if (IsInternalNews(notification))
            {
                return "Tin noi bo";
            }

            return "Phan hoi";
        }

        public string GetCategoryBadgeClass(AppNotification notification)
        {
            if (IsActionRequired(notification))
            {
                return "bg-danger";
            }

            if (IsInternalNews(notification))
            {
                return "bg-info text-dark";
            }

            return "bg-secondary";
        }

        private async Task LoadNotificationsAsync(string? filter)
        {
            CurrentFilter = NormalizeFilter(filter);

            var notifications = await _notificationBusiness.GetByReceiverId(UserData.UserId, 200);
            var visibleNotifications = notifications
                .Where(CanViewNotification)
                .ToList();

            VisibleTotalCount = visibleNotifications.Count;
            VisibleUnreadCount = visibleNotifications.Count(x => !x.IsRead);
            VisibleActionRequiredCount = visibleNotifications.Count(IsActionRequired);
            VisibleNewsCount = visibleNotifications.Count(IsInternalNews);
            VisibleFeedbackCount = visibleNotifications.Count(IsFeedback);

            Notifications = visibleNotifications
                .Where(notification => MatchesFilter(notification, CurrentFilter))
                .ToList();
        }

        private bool MatchesFilter(AppNotification notification, string filter)
        {
            return filter switch
            {
                "unread" => !notification.IsRead,
                "action" => IsActionRequired(notification),
                "news" => IsInternalNews(notification),
                "feedback" => IsFeedback(notification),
                _ => true
            };
        }

        private static string NormalizeFilter(string? filter)
        {
            return filter?.Trim().ToLowerInvariant() switch
            {
                "unread" => "unread",
                "action" => "action",
                "news" => "news",
                "feedback" => "feedback",
                _ => "all"
            };
        }
    }
}
