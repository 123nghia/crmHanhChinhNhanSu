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
            TitlePage = "Cấu hình mail";
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
                return new JsonResult(new { success = false, message = "Không có quyền" });
            }

            var result = await _emailConfigBusiness.SaveSetting(model, UserData.UserId);
            return new JsonResult(new { success = result });
        }

        public async Task<IActionResult> OnPostSaveTemplateAsync([FromBody] EmailTemplate model)
        {
            GetInfoUser();
            if (!(Permision.Edit ?? false) && !(Permision.Add ?? false))
            {
                return new JsonResult(new { success = false, message = "Không có quyền" });
            }

            var result = await _emailConfigBusiness.SaveTemplate(model, UserData.UserId);
            return new JsonResult(new { success = result });
        }

        public async Task<IActionResult> OnPostUploadEditorImageAsync(IFormFile? file)
        {
            GetInfoUser();
            if (!(Permision.Edit ?? false) && !(Permision.Add ?? false))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Không có quyền" });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Chưa chọn file tải lên" });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { error = "Dung lượng ảnh phải từ 5MB trở xuống" });
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
                return BadRequest(new { error = "Định dạng file không được hỗ trợ" });
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
                return new JsonResult(new { success = false, message = "Thiếu email người nhận" });
            }

            var templateCode = string.IsNullOrWhiteSpace(model.TemplateCode) ? "LEAVE_CREATE" : model.TemplateCode.Trim();
            await _emailConfigBusiness.EnsureDefaultTemplates(UserData?.UserId ?? 1);
            var setting = await _emailConfigBusiness.GetActiveSetting();
            if (setting == null || setting.IsActive <= 0 || string.IsNullOrWhiteSpace(setting.SmtpHost))
            {
                return new JsonResult(new { success = false, message = "SMTP chưa được cấu hình hoặc đang tắt" });
            }

            if (setting.SmtpPort <= 0)
            {
                return new JsonResult(new { success = false, message = "Cổng SMTP không hợp lệ" });
            }

            var templates = await _emailConfigBusiness.GetTemplates();
            var template = templates.FirstOrDefault(t => t.Code.Equals(templateCode, StringComparison.OrdinalIgnoreCase) && t.IsActive > 0);
            if (template == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy template hoặc template đang tắt" });
            }

            var now = DateTime.Now;
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["EmployeeName"] = UserData?.FullName ?? "Người dùng thử nghiệm",
                ["EmployeeEmail"] = model.ToEmail.Trim(),
                ["ManagerName"] = "Quản lý thử nghiệm",
                ["LeaveTypeName"] = "Nghỉ phép",
                ["LeaveType"] = "Nghỉ phép",
                ["LeaveTypeCode"] = "TEST",
                ["FromDate"] = now.ToString("dd/MM/yyyy"),
                ["ToDate"] = now.ToString("dd/MM/yyyy"),
                ["NumDays"] = "1",
                ["TotalDays"] = "1",
                ["Reason"] = "Email kiểm thử",
                ["StatusText"] = "Test",
                ["ApproverName"] = UserData?.FullName ?? "Người phê duyệt",
                ["ApproverRole"] = UserData?.RoleName ?? "Người phê duyệt",
                ["Comment"] = "Email kiểm thử",
                ["RejectReason"] = "Email kiểm thử",
                ["Action"] = "Test",
                ["CreateAt"] = now.ToString("dd/MM/yyyy HH:mm"),
                ["HandoverEmployeeName"] = "Test Handover",
                ["CandidateName"] = "Test Candidate",
                ["AppliedPosition"] = "Chuyên viên Kinh doanh",
                ["CandidatePosition"] = "Chuyên viên Kinh doanh",
                ["PositionText"] = "Chuyên viên Kinh doanh",
                ["InterviewAction"] = "đã được lên lịch",
                ["InterviewRound"] = "HR",
                ["InterviewMode"] = "Offline",
                ["ScheduleDate"] = now.AddDays(1).ToString("dd/MM/yyyy HH:mm"),
                ["AddressInfo"] = "Tầng 5 - Phòng họp A",
                ["InterviewerName"] = UserData?.FullName ?? "Interviewer",
                ["RoomName"] = "Phòng họp A",
                ["Noted"] = "Mang theo CV bản in"
            };

            var sendResult = await _emailService.SendTemplateWithErrorAsync(
                templateCode,
                new[] { model.ToEmail.Trim() },
                tokens,
                null,
                null,
                null,
                UserData?.UserId);
            return new JsonResult(new { success = sendResult.Success, message = sendResult.Success ? "Đã gửi" : (sendResult.Error ?? "Gửi thất bại") });
        }

        public class TestEmailRequest
        {
            public string? ToEmail { get; set; }
            public string? TemplateCode { get; set; }
        }
    }
}
