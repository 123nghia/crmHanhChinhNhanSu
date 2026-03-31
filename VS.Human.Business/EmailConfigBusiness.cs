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
            setting.CandidateFromEmail = NormalizeOptionalText(setting.CandidateFromEmail) ?? setting.HrFromEmail ?? NormalizeOptionalText(setting.FromEmail);
            setting.CandidateFromName = NormalizeOptionalText(setting.CandidateFromName) ?? setting.HrFromName ?? NormalizeOptionalText(setting.FromName);
            setting.HrSignature = NormalizeSignature(setting.HrSignature);
            setting.EmployeeSignature = NormalizeSignature(setting.EmployeeSignature);
            setting.CandidateSignature = NormalizeSignature(setting.CandidateSignature) ?? setting.HrSignature;
            setting.FromEmail = setting.HrFromEmail ?? setting.CandidateFromEmail ?? setting.EmployeeFromEmail;
            setting.FromName = setting.HrFromName ?? setting.CandidateFromName ?? setting.EmployeeFromName;
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
                "Thư mời phỏng vấn",
                "Thư mời phỏng vấn - {{CandidateName}}",
                EmailSenderTypes.Candidate,
                @"
<p>Xin chao {{CandidateName}},</p>
<p>Lịch phỏng vấn của bạn {{InterviewAction}}.</p>
<p>Thông tin chi tiết:</p>
<ul>
    <li>Vị trí ứng tuyển: {{AppliedPosition}}</li>
    <li>Vòng phỏng vấn: {{InterviewRound}}</li>
    <li>Thời gian: {{ScheduleDate}}</li>
    <li>Hình thức: {{InterviewMode}}</li>
    <li>Địa điểm / Link: {{AddressInfo}}</li>
    <li>Người phỏng vấn: {{InterviewerName}}</li>
</ul>
<p>{{Noted}}</p>
<p>Trân trọng,<br/>Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "LEAVE_CREATE",
                "Đơn nghỉ phép mới",
                "Đơn xin nghỉ phép mới từ {{EmployeeName}}",
                EmailSenderTypes.Employee,
                @"
<p>Xin chào {{ManagerName}},</p>
<p>Nhân viên {{EmployeeName}} vừa tạo một đơn xin nghỉ phép với thông tin như sau:</p>
<ul>
    <li>Loại nghỉ: {{LeaveType}}</li>
    <li>Từ ngày: {{FromDate}}</li>
    <li>Đến ngày: {{ToDate}}</li>
    <li>Số ngày nghỉ: {{TotalDays}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để xem và phê duyệt đơn.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "LEAVE_APPROVE",
                "Đơn nghỉ phép được phê duyệt",
                "Đơn nghỉ phép của bạn đã được phê duyệt",
                EmailSenderTypes.Employee,
                @"
<p>Xin chào {{EmployeeName}},</p>
<p>Đơn xin nghỉ phép của bạn đã được phê duyệt.</p>
<p>Thông tin chi tiết:</p>
<ul>
    <li>Loại nghỉ: {{LeaveType}}</li>
    <li>Từ ngày: {{FromDate}}</li>
    <li>Đến ngày: {{ToDate}}</li>
    <li>Số ngày nghỉ: {{TotalDays}}</li>
</ul>
<p>Chúc bạn có thời gian nghỉ ngơi hiệu quả.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "LEAVE_REJECT",
                "Đơn nghỉ phép bị từ chối",
                "Đơn nghỉ phép của bạn đã bị từ chối",
                EmailSenderTypes.Employee,
                @"
<p>Xin chào {{EmployeeName}},</p>
<p>Rất tiếc, đơn xin nghỉ phép của bạn đã bị từ chối.</p>
<p>Thông tin đơn:</p>
<ul>
    <li>Loại nghỉ: {{LeaveType}}</li>
    <li>Từ ngày: {{FromDate}}</li>
    <li>Đến ngày: {{ToDate}}</li>
</ul>
<p>Lý do từ chối:<br />{{RejectReason}}</p>
<p>Vui lòng liên hệ quản lý để biết thêm chi tiết.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "LEAVE_PENDING_HCNS",
                "Đơn nghỉ phép chờ HCNS xử lý",
                "Đơn nghỉ phép của {{EmployeeName}} chờ HCNS xử lý",
                EmailSenderTypes.Employee,
                @"
<p>Kính gửi Phòng HCNS,</p>
<p>Đơn nghỉ phép của {{EmployeeName}} đã được Team Lead duyệt và đang chờ HCNS xử lý.</p>
<ul>
    <li>Loại nghỉ: {{LeaveType}}</li>
    <li>Từ ngày: {{FromDate}}</li>
    <li>Đến ngày: {{ToDate}}</li>
    <li>Số ngày nghỉ: {{TotalDays}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để tiếp tục xử lý đơn.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "LEAVE_PENDING_BGD",
                "Đơn nghỉ phép chờ BGĐ phê duyệt",
                "Đơn nghỉ phép của {{EmployeeName}} chờ BGĐ phê duyệt",
                EmailSenderTypes.Employee,
                @"
<p>Kính gửi Ban Giám đốc,</p>
<p>Đơn nghỉ phép của {{EmployeeName}} đã được HCNS kiểm tra và đang chờ BGĐ phê duyệt.</p>
<ul>
    <li>Loại nghỉ: {{LeaveType}}</li>
    <li>Từ ngày: {{FromDate}}</li>
    <li>Đến ngày: {{ToDate}}</li>
    <li>Số ngày nghỉ: {{TotalDays}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để xem và phê duyệt đơn.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "LATE_EARLY_CREATE",
                "Đơn đi trễ về sớm mới",
                "Đơn {{RequestTypeName}} mới từ {{EmployeeName}}",
                EmailSenderTypes.Employee,
                @"
<p>Xin chào {{ManagerName}},</p>
<p>{{EmployeeName}} vừa tạo một yêu cầu {{RequestTypeName}} với thông tin như sau:</p>
<ul>
    <li>Ngày áp dụng: {{RequestDate}}</li>
    <li>Khung giờ: {{TimeRange}}</li>
    <li>Số phút: {{DurationMinutes}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để xem và phê duyệt yêu cầu.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "LATE_EARLY_APPROVE",
                "Đơn đi trễ về sớm được phê duyệt",
                "Đơn {{RequestTypeName}} của bạn đã được phê duyệt",
                EmailSenderTypes.Employee,
                @"
<p>Xin chào {{EmployeeName}},</p>
<p>Yêu cầu {{RequestTypeName}} của bạn đã được phê duyệt.</p>
<ul>
    <li>Ngày áp dụng: {{RequestDate}}</li>
    <li>Khung giờ: {{TimeRange}}</li>
    <li>Số phút: {{DurationMinutes}}</li>
</ul>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "LATE_EARLY_REJECT",
                "Đơn đi trễ về sớm bị từ chối",
                "Đơn {{RequestTypeName}} của bạn đã bị từ chối",
                EmailSenderTypes.Employee,
                @"
<p>Xin chào {{EmployeeName}},</p>
<p>Rất tiếc, yêu cầu {{RequestTypeName}} của bạn đã bị từ chối.</p>
<ul>
    <li>Ngày áp dụng: {{RequestDate}}</li>
    <li>Khung giờ: {{TimeRange}}</li>
</ul>
<p>Lý do từ chối:<br />{{RejectReason}}</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "LATE_EARLY_PENDING_HCNS",
                "Đơn đi trễ về sớm chờ HCNS xử lý",
                "Đơn {{RequestTypeName}} của {{EmployeeName}} chờ HCNS xử lý",
                EmailSenderTypes.Employee,
                @"
<p>Kính gửi Phòng HCNS,</p>
<p>Yêu cầu {{RequestTypeName}} của {{EmployeeName}} đã được Team Lead duyệt và đang chờ HCNS xử lý.</p>
<ul>
    <li>Ngày áp dụng: {{RequestDate}}</li>
    <li>Khung giờ: {{TimeRange}}</li>
    <li>Số phút: {{DurationMinutes}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để tiếp tục xử lý.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "LATE_EARLY_PENDING_BGD",
                "Đơn đi trễ về sớm chờ BGĐ phê duyệt",
                "Đơn {{RequestTypeName}} của {{EmployeeName}} chờ BGĐ phê duyệt",
                EmailSenderTypes.Employee,
                @"
<p>Kính gửi Ban Giám đốc,</p>
<p>Yêu cầu {{RequestTypeName}} của {{EmployeeName}} đã được HCNS kiểm tra và đang chờ BGĐ phê duyệt.</p>
<ul>
    <li>Ngày áp dụng: {{RequestDate}}</li>
    <li>Khung giờ: {{TimeRange}}</li>
    <li>Số phút: {{DurationMinutes}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để xem và phê duyệt yêu cầu.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "LATE_EARLY_PENDING_ADMIN",
                "Đơn đi trễ về sớm chờ Admin phê duyệt",
                "Đơn {{RequestTypeName}} của {{EmployeeName}} chờ Admin phê duyệt",
                EmailSenderTypes.Employee,
                @"
<p>Kinh gui Admin,</p>
<p>Yêu cầu {{RequestTypeName}} của {{EmployeeName}} đã được BGĐ duyệt và đang chờ Admin xác nhận cuối.</p>
<ul>
    <li>Ngày áp dụng: {{RequestDate}}</li>
    <li>Khung giờ: {{TimeRange}}</li>
    <li>Số phút: {{DurationMinutes}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để tiếp tục xử lý.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "SUPPORT_REQUEST_CREATE",
                "Yêu cầu hỗ trợ mới",
                "Yêu cầu hỗ trợ mới: {{Title}}",
                EmailSenderTypes.Employee,
                @"
<p>Xin chao {{AssignedToName}},</p>
<p>Bạn vừa được giao một yêu cầu hỗ trợ mới.</p>
<ul>
    <li>Người tạo: {{RequesterName}}</li>
    <li>Bộ phận cần xử lý: {{TargetDepartment}}</li>
    <li>Tiêu đề: {{Title}}</li>
    <li>Nội dung: {{Content}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để tiếp nhận và cập nhật tiến độ.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "SUPPORT_REQUEST_INPROCESS",
                "Yêu cầu hỗ trợ đang được xử lý",
                "Yêu cầu hỗ trợ '{{Title}}' đang được xử lý",
                EmailSenderTypes.Employee,
                @"
<p>Xin chao {{RequesterName}},</p>
<p>Yêu cầu hỗ trợ của bạn đã được tiếp nhận và đang xử lý.</p>
<ul>
    <li>Bộ phận xử lý: {{TargetDepartment}}</li>
    <li>Người phụ trách: {{AssignedToName}}</li>
    <li>Tiêu đề: {{Title}}</li>
    <li>Ghi chú xử lý: {{ProcessorComment}}</li>
</ul>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "SUPPORT_REQUEST_DONE",
                "Yêu cầu hỗ trợ đã hoàn thành",
                "Yêu cầu hỗ trợ '{{Title}}' đã hoàn thành",
                EmailSenderTypes.Employee,
                @"
<p>Xin chao {{RequesterName}},</p>
<p>Yêu cầu hỗ trợ của bạn đã được xử lý xong.</p>
<ul>
    <li>Bộ phận xử lý: {{TargetDepartment}}</li>
    <li>Người phụ trách: {{AssignedToName}}</li>
    <li>Tiêu đề: {{Title}}</li>
    <li>Kết quả: {{ProcessorComment}}</li>
</ul>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");

            yield return CreateTemplate(
                "SUPPORT_REQUEST_CANCEL",
                "Yêu cầu hỗ trợ đã bị hủy",
                "Yêu cầu hỗ trợ '{{Title}}' đã bị hủy",
                EmailSenderTypes.Employee,
                @"
<p>Xin chao {{RequesterName}},</p>
<p>Yêu cầu hỗ trợ của bạn đã được cập nhật sang trạng thái hủy.</p>
<ul>
    <li>Bộ phận xử lý: {{TargetDepartment}}</li>
    <li>Người phụ trách: {{AssignedToName}}</li>
    <li>Tiêu đề: {{Title}}</li>
    <li>Lý do / ghi chú: {{ProcessorComment}}</li>
</ul>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>");
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
            setting.CandidateSignature = NormalizeSignature(setting.CandidateSignature);
            return setting;
        }
    }
}
