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
        private const string OldDashboardNoticeText = "CÃ i Ä‘áº·t hiá»ƒn thá»‹ thÃ´ng bÃ¡o má»›i ngay giao diá»‡n mÃ n hÃ¬nh chÃ­nh khi Ä‘Äƒng nháº­p";
        private const string NewDashboardNoticeText = "CÃ i Ä‘áº·t hiá»ƒn thá»‹ thÃ´ng bÃ¡o má»›i ngay trÃªn mÃ n hÃ¬nh chÃ­nh khi Ä‘Äƒng nháº­p";
        private const string InternalNewsEmailEntityType = "INTERNAL_NEWS";

        private readonly IEmailService _emailService;

        public InternalNewsBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IEmailService emailService)
            : base(unitOfWork, httpContextAccessor)
        {
            _emailService = emailService;
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
                return (false, "KhÃ´ng tÃ¬m tháº¥y bÃ i viáº¿t Ä‘á»ƒ gá»­i mail.", 0);
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
                return (false, "ChÆ°a cÃ³ ngÆ°á»i nháº­n há»£p lá»‡ trong nhÃ³m mail hoáº·c danh sÃ¡ch nháº­p tay.", 0);
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
