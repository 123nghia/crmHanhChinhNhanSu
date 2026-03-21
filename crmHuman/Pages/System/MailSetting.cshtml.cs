using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using VS.Human.Business;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.System
{
    [Authorize]
    public class MailSettingModel : BaseModel2
    {
        private readonly IEmailConfigBusiness _emailConfigBusiness;
        private readonly IEmailService _emailService;

        public MailSettingModel(IEmailConfigBusiness emailConfigBusiness, IEmailService emailService)
        {
            _emailConfigBusiness = emailConfigBusiness;
            _emailService = emailService;
            TitlePage = "Mail setting";
            KeyPage = "MailSetting";
        }

        public EmailSetting? Setting { get; set; }
        public List<EmailTemplate> Templates { get; set; } = new List<EmailTemplate>();

        public async Task OnGetAsync()
        {
            GetInfoUser();
            Setting = await _emailConfigBusiness.GetActiveSetting() ?? new EmailSetting();
            Templates = await _emailConfigBusiness.GetTemplates();
        }

        public async Task<IActionResult> OnPostSaveSettingAsync([FromBody] EmailSetting model)
        {
            GetInfoUser();
            if (!(Permision.Edit ?? false) && !(Permision.Add ?? false))
            {
                return new JsonResult(new { success = false, message = "No permission" });
            }

            var result = await _emailConfigBusiness.SaveSetting(model, UserData.UserId);
            return new JsonResult(new { success = result });
        }

        public async Task<IActionResult> OnPostSaveTemplateAsync([FromBody] EmailTemplate model)
        {
            GetInfoUser();
            if (!(Permision.Edit ?? false) && !(Permision.Add ?? false))
            {
                return new JsonResult(new { success = false, message = "No permission" });
            }

            var result = await _emailConfigBusiness.SaveTemplate(model, UserData.UserId);
            return new JsonResult(new { success = result });
        }

        public async Task<IActionResult> OnPostSendTestAsync([FromBody] TestEmailRequest model)
        {
            GetInfoUser();
            if (model == null || string.IsNullOrWhiteSpace(model.ToEmail))
            {
                return new JsonResult(new { success = false, message = "Missing recipient email" });
            }

            var templateCode = string.IsNullOrWhiteSpace(model.TemplateCode) ? "LEAVE_CREATE" : model.TemplateCode.Trim();
            var setting = await _emailConfigBusiness.GetActiveSetting();
            if (setting == null || setting.IsActive <= 0 || string.IsNullOrWhiteSpace(setting.SmtpHost))
            {
                return new JsonResult(new { success = false, message = "SMTP is not configured or inactive" });
            }

            if (setting.SmtpPort <= 0)
            {
                return new JsonResult(new { success = false, message = "SMTP port is invalid" });
            }

            var templates = await _emailConfigBusiness.GetTemplates();
            var template = templates.FirstOrDefault(t => t.Code.Equals(templateCode, StringComparison.OrdinalIgnoreCase) && t.IsActive > 0);
            if (template == null)
            {
                return new JsonResult(new { success = false, message = "Template not found or inactive" });
            }

            var now = DateTime.Now;
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["EmployeeName"] = UserData?.FullName ?? "Test User",
                ["EmployeeEmail"] = model.ToEmail.Trim(),
                ["ManagerName"] = "Test Manager",
                ["LeaveTypeName"] = "Nghi phep",
                ["LeaveType"] = "Nghi phep",
                ["LeaveTypeCode"] = "TEST",
                ["FromDate"] = now.ToString("dd/MM/yyyy"),
                ["ToDate"] = now.ToString("dd/MM/yyyy"),
                ["NumDays"] = "1",
                ["TotalDays"] = "1",
                ["Reason"] = "Test email",
                ["StatusText"] = "Test",
                ["ApproverName"] = UserData?.FullName ?? "Approver",
                ["ApproverRole"] = UserData?.RoleName ?? "Approver",
                ["Comment"] = "Test email",
                ["RejectReason"] = "Test email",
                ["Action"] = "Test",
                ["CreateAt"] = now.ToString("dd/MM/yyyy HH:mm"),
                ["HandoverEmployeeName"] = "Test Handover"
            };

            var sendResult = await _emailService.SendTemplateWithErrorAsync(templateCode, new[] { model.ToEmail.Trim() }, tokens);
            return new JsonResult(new { success = sendResult.Success, message = sendResult.Success ? "Sent" : (sendResult.Error ?? "Send failed") });
        }

        public class TestEmailRequest
        {
            public string? ToEmail { get; set; }
            public string? TemplateCode { get; set; }
        }
    }
}
