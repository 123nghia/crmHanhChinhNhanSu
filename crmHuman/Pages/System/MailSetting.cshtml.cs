using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly IWebHostEnvironment _env;

        public MailSettingModel(IEmailConfigBusiness emailConfigBusiness, IEmailService emailService, IWebHostEnvironment env)
        {
            _emailConfigBusiness = emailConfigBusiness;
            _emailService = emailService;
            _env = env;
            TitlePage = "Mail setting";
            KeyPage = "MailSetting";
        }

        public EmailSetting? Setting { get; set; }
        public List<EmailTemplate> Templates { get; set; } = new List<EmailTemplate>();

        public async Task OnGetAsync()
        {
            GetInfoUser();
            await _emailConfigBusiness.EnsureDefaultTemplates(UserData?.UserId ?? 1);
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

        public async Task<IActionResult> OnPostUploadEditorImageAsync(IFormFile? file)
        {
            GetInfoUser();
            if (!(Permision.Edit ?? false) && !(Permision.Add ?? false))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "No permission" });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { error = "Image size must be 5MB or smaller" });
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".gif",
                ".webp",
                ".bmp"
            };

            if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
            {
                return BadRequest(new { error = "Unsupported file type" });
            }

            var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", "mail-editor", (UserData?.UserId ?? 0).ToString());
            Directory.CreateDirectory(uploadRoot);

            var storedName = $"mail_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadRoot, storedName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var location = $"/uploads/mail-editor/{UserData?.UserId ?? 0}/{storedName}";
            return new JsonResult(new { location });
        }

        public async Task<IActionResult> OnPostSendTestAsync([FromBody] TestEmailRequest model)
        {
            GetInfoUser();
            if (model == null || string.IsNullOrWhiteSpace(model.ToEmail))
            {
                return new JsonResult(new { success = false, message = "Missing recipient email" });
            }

            var templateCode = string.IsNullOrWhiteSpace(model.TemplateCode) ? "LEAVE_CREATE" : model.TemplateCode.Trim();
            await _emailConfigBusiness.EnsureDefaultTemplates(UserData?.UserId ?? 1);
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
                ["HandoverEmployeeName"] = "Test Handover",
                ["CandidateName"] = "Test Candidate",
                ["AppliedPosition"] = "Chuyen vien Kinh doanh",
                ["CandidatePosition"] = "Chuyen vien Kinh doanh",
                ["PositionText"] = "Chuyen vien Kinh doanh",
                ["InterviewAction"] = "da duoc len lich",
                ["InterviewRound"] = "HR",
                ["InterviewMode"] = "Offline",
                ["ScheduleDate"] = now.AddDays(1).ToString("dd/MM/yyyy HH:mm"),
                ["AddressInfo"] = "Tang 5 - Phong hop A",
                ["InterviewerName"] = UserData?.FullName ?? "Interviewer",
                ["RoomName"] = "Phong hop A",
                ["Noted"] = "Mang theo CV ban in"
            };

            var sendResult = await _emailService.SendTemplateWithErrorAsync(
                templateCode,
                new[] { model.ToEmail.Trim() },
                tokens,
                null,
                null,
                null,
                UserData?.UserId);
            return new JsonResult(new { success = sendResult.Success, message = sendResult.Success ? "Sent" : (sendResult.Error ?? "Send failed") });
        }

        public class TestEmailRequest
        {
            public string? ToEmail { get; set; }
            public string? TemplateCode { get; set; }
        }
    }
}
