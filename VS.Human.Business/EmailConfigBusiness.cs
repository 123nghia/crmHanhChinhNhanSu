using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using VS.Human.Business.Helpers;
using VS.Human.Business.Imp;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class EmailConfigBusiness : BaseBusiness, IEmailConfigBusiness
    {
        public EmailConfigBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)
            : base(unitOfWork, contextAccessor)
        {
        }

        public async Task<EmailSetting?> GetActiveSetting()
        {
            var setting = await _unitOfWork.EmailConfigRep.GetActiveSetting();
            return NormalizeSetting(setting);
        }

        public async Task<List<EmailTemplate>> GetTemplates()
        {
            await EnsureDefaultTemplates(1);
            return await _unitOfWork.EmailConfigRep.GetTemplates();
        }

        public async Task EnsureDefaultTemplates(int userId)
        {
            var effectiveUserId = userId > 0 ? userId : 1;
            var templates = await _unitOfWork.EmailConfigRep.GetTemplates();
            var existingCodes = new HashSet<string>(
                templates
                    .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                    .Select(x => x.Code.Trim()),
                StringComparer.OrdinalIgnoreCase);

            foreach (var template in GetDefaultTemplates())
            {
                if (existingCodes.Contains(template.Code))
                {
                    continue;
                }

                template.CreatedBy = effectiveUserId;
                template.UpdatedBy = effectiveUserId;
                await _unitOfWork.EmailConfigRep.SaveTemplate(template);
                existingCodes.Add(template.Code);
            }
        }

        public async Task<bool> SaveSetting(EmailSetting setting, int userId)
        {
            if (setting == null)
            {
                return false;
            }

            if (setting.Id > 0)
            {
                var existing = await _unitOfWork.EmailConfigRep.GetSettingById(setting.Id);
                if (existing != null && string.IsNullOrWhiteSpace(setting.SmtpPassword))
                {
                    setting.SmtpPassword = existing.SmtpPassword;
                }
            }

            setting.HrFromEmail = NormalizeOptionalText(setting.HrFromEmail) ?? NormalizeOptionalText(setting.FromEmail);
            setting.HrFromName = NormalizeOptionalText(setting.HrFromName) ?? NormalizeOptionalText(setting.FromName);
            setting.EmployeeFromEmail = NormalizeOptionalText(setting.EmployeeFromEmail);
            setting.EmployeeFromName = NormalizeOptionalText(setting.EmployeeFromName);
            setting.HrSignature = NormalizeSignature(setting.HrSignature);
            setting.EmployeeSignature = NormalizeSignature(setting.EmployeeSignature);
            setting.FromEmail = setting.HrFromEmail ?? setting.EmployeeFromEmail;
            setting.FromName = setting.HrFromName ?? setting.EmployeeFromName;
            setting.UpdatedBy = userId;
            if (setting.Id <= 0)
            {
                setting.CreatedBy = userId;
            }

            return await _unitOfWork.EmailConfigRep.SaveSetting(setting);
        }

        public async Task<bool> SaveTemplate(EmailTemplate template, int userId)
        {
            if (template == null || string.IsNullOrWhiteSpace(template.Code))
            {
                return false;
            }

            template.SenderType = EmailSenderTypes.Normalize(template.SenderType);
            template.UpdatedBy = userId;
            if (template.Id <= 0)
            {
                template.CreatedBy = userId;
            }

            return await _unitOfWork.EmailConfigRep.SaveTemplate(template);
        }

        private static IEnumerable<EmailTemplate> GetDefaultTemplates()
        {
            yield return CreateTemplate(
                "INTERVIEW_SCHEDULE",
                "Thu moi phong van",
                "Thu moi phong van - {{CandidateName}}",
                EmailSenderTypes.Hr,
                @"
<p>Xin chao {{CandidateName}},</p>
<p>Lich phong van cua ban {{InterviewAction}}.</p>
<p>Thong tin chi tiet:</p>
<ul>
    <li>Vi tri ung tuyen: {{AppliedPosition}}</li>
    <li>Vong phong van: {{InterviewRound}}</li>
    <li>Thoi gian: {{ScheduleDate}}</li>
    <li>Hinh thuc: {{InterviewMode}}</li>
    <li>Dia diem / Link: {{AddressInfo}}</li>
    <li>Nguoi phong van: {{InterviewerName}}</li>
</ul>
<p>{{Noted}}</p>
<p>Tran trong,<br/>He thong quan ly nhan su</p>");

            yield return CreateTemplate(
                "LEAVE_CREATE",
                "Don nghi phep moi",
                "Don xin nghi phep moi tu {{EmployeeName}}",
                EmailSenderTypes.Employee,
                @"
<p>Xin chao {{ManagerName}},</p>
<p>Nhan vien {{EmployeeName}} vua tao mot don xin nghi phep voi thong tin nhu sau:</p>
<ul>
    <li>Loai nghi: {{LeaveType}}</li>
    <li>Tu ngay: {{FromDate}}</li>
    <li>Den ngay: {{ToDate}}</li>
    <li>So ngay nghi: {{TotalDays}}</li>
    <li>Ly do: {{Reason}}</li>
</ul>
<p>Vui long dang nhap he thong de xem va phe duyet don.</p>
<p>Tran trong,<br />He thong quan ly nhan su</p>");

            yield return CreateTemplate(
                "LEAVE_APPROVE",
                "Don nghi phep duoc phe duyet",
                "Don nghi phep cua ban da duoc phe duyet",
                EmailSenderTypes.Hr,
                @"
<p>Xin chao {{EmployeeName}},</p>
<p>Don xin nghi phep cua ban da duoc phe duyet.</p>
<p>Thong tin chi tiet:</p>
<ul>
    <li>Loai nghi: {{LeaveType}}</li>
    <li>Tu ngay: {{FromDate}}</li>
    <li>Den ngay: {{ToDate}}</li>
    <li>So ngay nghi: {{TotalDays}}</li>
</ul>
<p>Chuc ban co thoi gian nghi ngoi hieu qua.</p>
<p>Tran trong,<br />He thong quan ly nhan su</p>");

            yield return CreateTemplate(
                "LEAVE_REJECT",
                "Don nghi phep bi tu choi",
                "Don nghi phep cua ban da bi tu choi",
                EmailSenderTypes.Hr,
                @"
<p>Xin chao {{EmployeeName}},</p>
<p>Rat tiec, don xin nghi phep cua ban da bi tu choi.</p>
<p>Thong tin don:</p>
<ul>
    <li>Loai nghi: {{LeaveType}}</li>
    <li>Tu ngay: {{FromDate}}</li>
    <li>Den ngay: {{ToDate}}</li>
</ul>
<p>Ly do tu choi:<br />{{RejectReason}}</p>
<p>Vui long lien he quan ly de biet them chi tiet.</p>
<p>Tran trong,<br />He thong quan ly nhan su</p>");

            yield return CreateTemplate(
                "LEAVE_PENDING_HCNS",
                "Don nghi phep cho HCNS xu ly",
                "Don nghi phep cua {{EmployeeName}} cho HCNS xu ly",
                EmailSenderTypes.Hr,
                @"
<p>Kinh gui Phong HCNS,</p>
<p>Don nghi phep cua {{EmployeeName}} da duoc Team Lead duyet va dang cho HCNS xu ly.</p>
<ul>
    <li>Loai nghi: {{LeaveType}}</li>
    <li>Tu ngay: {{FromDate}}</li>
    <li>Den ngay: {{ToDate}}</li>
    <li>So ngay nghi: {{TotalDays}}</li>
    <li>Ly do: {{Reason}}</li>
</ul>
<p>Vui long dang nhap he thong de tiep tuc xu ly don.</p>
<p>Tran trong,<br />He thong quan ly nhan su</p>");

            yield return CreateTemplate(
                "LEAVE_PENDING_BGD",
                "Don nghi phep cho BGD phe duyet",
                "Don nghi phep cua {{EmployeeName}} cho BGD phe duyet",
                EmailSenderTypes.Hr,
                @"
<p>Kinh gui Ban Giam doc,</p>
<p>Don nghi phep cua {{EmployeeName}} da duoc HCNS kiem tra va dang cho BGD phe duyet.</p>
<ul>
    <li>Loai nghi: {{LeaveType}}</li>
    <li>Tu ngay: {{FromDate}}</li>
    <li>Den ngay: {{ToDate}}</li>
    <li>So ngay nghi: {{TotalDays}}</li>
    <li>Ly do: {{Reason}}</li>
</ul>
<p>Vui long dang nhap he thong de xem va phe duyet don.</p>
<p>Tran trong,<br />He thong quan ly nhan su</p>");
        }

        private static EmailTemplate CreateTemplate(string code, string name, string subject, string senderType, string body)
        {
            return new EmailTemplate
            {
                Code = code,
                Name = name,
                Subject = subject,
                Body = body,
                SenderType = EmailSenderTypes.Normalize(senderType),
                IsActive = 1,
                CcManager = false
            };
        }

        private static string? NormalizeOptionalText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static string? NormalizeSignature(string? value)
        {
            var normalized = NormalizeOptionalText(value);
            return EmailSignatureHtmlNormalizer.NormalizeSignatureHtml(normalized);
        }

        private static EmailSetting? NormalizeSetting(EmailSetting? setting)
        {
            if (setting == null)
            {
                return null;
            }

            setting.HrSignature = NormalizeSignature(setting.HrSignature);
            setting.EmployeeSignature = NormalizeSignature(setting.EmployeeSignature);
            return setting;
        }
    }
}
