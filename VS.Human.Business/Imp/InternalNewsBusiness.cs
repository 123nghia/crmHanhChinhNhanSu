using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class InternalNewsBusiness : BaseBusiness, IInternalNewsBusiness
    {
        private const string OldDashboardNoticeText = "CÃƒÂ i Ã„â€˜Ã¡ÂºÂ·t hiÃ¡Â»Æ’n thÃ¡Â»â€¹ thÃƒÂ´ng bÃƒÂ¡o mÃ¡Â»â€ºi ngay giao diÃ¡Â»â€¡n mÃƒÂ n hÃƒÂ¬nh chÃƒÂ­nh khi Ã„â€˜Ã„Æ’ng nhÃ¡ÂºÂ­p";
        private const string NewDashboardNoticeText = "Cài đặt hiển thị thông báo mới ngay trên màn hình chính khi đăng nhập";
        private const string InternalNewsEmailEntityType = "INTERNAL_NEWS";

        private readonly IEmailService _emailService;
        private readonly INotificationBusiness _notificationBusiness;

        public InternalNewsBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IEmailService emailService, INotificationBusiness notificationBusiness)
            : base(unitOfWork, httpContextAccessor)
        {
            _emailService = emailService;
            _notificationBusiness = notificationBusiness;
        }

        public Task<BaseList> GetAll(InternalNewsRequest request)
        {
            return _unitOfWork.InternalNewsRep.GetAll(request);
        }

        public async Task<InternalNewsItem> GetById(int id)
        {
            var item = await _unitOfWork.InternalNewsRep.GetById(id);
            NormalizeContent(item);
            return item;
        }

        public Task<bool> AddOrUpdate(InternalNewsItem item)
        {
            NormalizeContent(item);
            return _unitOfWork.InternalNewsRep.AddOrUpdate(item);
        }

        public async Task<(bool Success, string? Error, int RecipientCount)> SendNotificationAsync(int newsId, string? detailUrl, int senderEmployeeId)
        {
            var item = await GetById(newsId);
            if (item == null || item.Id <= 0)
            {
                return (false, "Không tìm thấy bài viết để gửi mail.", 0);
            }

            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var groupRecipients = await _unitOfWork.MailGroupRep.ResolveRecipientsAsync(item.MailGroupIds);
            foreach (var email in groupRecipients)
            {
                AddRecipients(recipients, email);
            }

            foreach (var email in ParseEmails(item.DirectRecipientEmails))
            {
                AddRecipients(recipients, email);
            }

            if (recipients.Count == 0)
            {
                return (false, "Chưa có người nhận hợp lệ trong nhóm mail hoặc danh sách nhập tay.", 0);
            }

            var createdAtText = item.CreateAt == default ? DateTime.Now.ToString("dd/MM/yyyy HH:mm") : item.CreateAt.ToString("dd/MM/yyyy HH:mm");
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["NewsTitle"] = item.Title?.Trim() ?? string.Empty,
                ["NewsContent"] = item.Content ?? string.Empty,
                ["NewsAuthor"] = item.AuthorName ?? string.Empty,
                ["NewsCreatedAt"] = createdAtText,
                ["NewsUrl"] = detailUrl ?? string.Empty
            };

            var sendResult = await _emailService.SendTemplateWithErrorAsync(
                "INTERNAL_NEWS_NOTIFY",
                recipients,
                tokens,
                null,
                null,
                null,
                senderEmployeeId > 0 ? senderEmployeeId : item.CreatedBy,
                new EmailSendContext
                {
                    RelatedEntityType = InternalNewsEmailEntityType,
                    RelatedEntityId = item.Id
                });

            await TryCreateAppNotificationsAsync(item, recipients, detailUrl, senderEmployeeId);

            return (sendResult.Success, sendResult.Error, recipients.Count);
        }

        public Task<bool> Delete(int id)
        {
            return _unitOfWork.InternalNewsRep.Delete(id);
        }

        private static IEnumerable<string> ParseEmails(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<string>();
            }

            return input
                .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x));
        }

        private static void AddRecipients(HashSet<string> recipients, string? email)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                recipients.Add(email.Trim());
            }
        }

        private async Task TryCreateAppNotificationsAsync(InternalNewsItem item, IEnumerable<string> recipients, string? detailUrl, int senderEmployeeId)
        {
            if (item == null || item.Id <= 0 || _notificationBusiness == null)
            {
                return;
            }

            var employees = await _unitOfWork.EmployeeRep.GetByEmails(recipients);
            if (employees.Count == 0)
            {
                return;
            }

            var notificationLink = NormalizeNotificationLink(detailUrl);
            var message = string.IsNullOrWhiteSpace(item.Title)
                ? "Co tin noi bo moi."
                : $"Tin noi bo moi: {item.Title.Trim()}";

            foreach (var employee in employees.Where(x => x.Id > 0).GroupBy(x => x.Id).Select(x => x.First()))
            {
                await _notificationBusiness.CreateNotificationWithId(new AppNotification
                {
                    ReceiverId = employee.Id,
                    SenderId = senderEmployeeId > 0 ? senderEmployeeId : item.CreatedBy,
                    Message = message,
                    Link = notificationLink,
                    Type = "InternalNews",
                    Category = "INFO",
                    RelatedEntityType = InternalNewsEmailEntityType,
                    RelatedEntityId = item.Id,
                    EventCode = WorkflowEventCodes.NotificationCreated
                });
            }
        }

        private static string? NormalizeNotificationLink(string? detailUrl)
        {
            if (string.IsNullOrWhiteSpace(detailUrl))
            {
                return "/InternalNews";
            }

            if (detailUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return detailUrl;
            }

            if (Uri.TryCreate(detailUrl, UriKind.Absolute, out var absoluteUri))
            {
                return string.IsNullOrWhiteSpace(absoluteUri.PathAndQuery)
                    ? "/InternalNews"
                    : absoluteUri.PathAndQuery;
            }

            return $"/{detailUrl.TrimStart('/')}";
        }

        private static void NormalizeContent(InternalNewsItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Content))
            {
                return;
            }

            item.Content = item.Content.Replace(OldDashboardNoticeText, NewDashboardNoticeText, StringComparison.Ordinal);
        }
    }
}
