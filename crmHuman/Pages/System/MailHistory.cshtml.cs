using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.System
{
    [Authorize]
    public class MailHistoryModel : BaseModel2
    {
        private readonly IEmailSentLogBusiness _emailSentLogBusiness;
        private readonly IEmailConfigBusiness _emailConfigBusiness;

        public MailHistoryModel(IEmailSentLogBusiness emailSentLogBusiness, IEmailConfigBusiness emailConfigBusiness)
        {
            _emailSentLogBusiness = emailSentLogBusiness;
            _emailConfigBusiness = emailConfigBusiness;
            TitlePage = "Mail da gui";
            KeyPage = "MailSetting";
        }

        public BaseList LogData { get; set; } = new BaseList();
        public EmailSentLogRequest RequestSearch { get; set; } = new EmailSentLogRequest();
        public List<EmailTemplate> Templates { get; set; } = new List<EmailTemplate>();

        public int TotalPages
        {
            get
            {
                var limit = RequestSearch?.Limit > 0 ? RequestSearch.Limit : 20;
                return Math.Max(1, (int)Math.Ceiling((double)(LogData?.Total ?? 0) / limit));
            }
        }

        public async Task OnGetAsync(int page = 1, int limit = 20, string? token = null, int? sendStatus = null, string? templateCode = null, DateTime? from = null, DateTime? to = null)
        {
            GetInfoUser();

            RequestSearch = new EmailSentLogRequest
            {
                Page = page <= 0 ? 1 : page,
                Limit = limit <= 0 ? 20 : limit,
                Token = token?.Trim(),
                SendStatus = sendStatus ?? -1,
                TemplateCode = templateCode?.Trim(),
                From = from ?? DateTime.Today.AddDays(-30),
                To = to ?? DateTime.Today
            };

            if (!(Permision.View ?? false))
            {
                LogData = new BaseList
                {
                    Total = 0,
                    Data = new List<object>()
                };
                return;
            }

            Templates = await _emailConfigBusiness.GetTemplates();
            LogData = await _emailSentLogBusiness.GetLogs(RequestSearch, UserData?.UserId ?? 0, UserData?.RoleCode);
        }

        public async Task<IActionResult> OnGetDetailAsync(int id)
        {
            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                return new JsonResult(new { success = false, message = "No permission" });
            }

            var log = await _emailSentLogBusiness.GetById(id, UserData?.UserId ?? 0, UserData?.RoleCode);
            if (log == null || log.Id <= 0)
            {
                return new JsonResult(new { success = false, message = "Khong tim thay mail hoac ban khong co quyen xem" });
            }

            return new JsonResult(new
            {
                success = true,
                data = new
                {
                    log.Id,
                    log.TemplateCode,
                    log.TemplateName,
                    log.FromEmail,
                    log.FromName,
                    log.ToEmails,
                    log.CcEmails,
                    log.BccEmails,
                    log.Subject,
                    log.BodyHtml,
                    log.SendSuccess,
                    log.ErrorMessage,
                    log.TriggeredByDisplay,
                    log.MessageId,
                    CreateAt = log.CreateAt.ToString("dd/MM/yyyy HH:mm:ss")
                }
            });
        }
    }
}
