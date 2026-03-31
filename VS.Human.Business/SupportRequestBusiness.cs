using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Globalization;
using VS.Human.Business.Imp;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class SupportRequestBusiness : BaseBusiness, ISupportRequestBusiness
    {
        private const string SupportRequestCreateTemplateCode = "SUPPORT_REQUEST_CREATE";
        private const string SupportRequestInProcessTemplateCode = "SUPPORT_REQUEST_INPROCESS";
        private const string SupportRequestDoneTemplateCode = "SUPPORT_REQUEST_DONE";
        private const string SupportRequestCancelTemplateCode = "SUPPORT_REQUEST_CANCEL";
        private const string SupportRequestEntityType = "SUPPORT_REQUEST";

        private readonly IEmailConfigBusiness _emailConfigBusiness;
        private readonly IEmailService _emailService;
        private readonly INotificationBusiness _notificationBusiness;
        private readonly ILogger<SupportRequestBusiness> _logger;

        public SupportRequestBusiness(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor contextAccessor,
            IEmailConfigBusiness emailConfigBusiness,
            IEmailService emailService,
            INotificationBusiness notificationBusiness,
            ILogger<SupportRequestBusiness> logger)
            : base(unitOfWork, contextAccessor)
        {
            _emailConfigBusiness = emailConfigBusiness;
            _emailService = emailService;
            _notificationBusiness = notificationBusiness;
            _logger = logger;
        }

        public Task<BaseList> GetAll(SupportRequestListRequest request)
        {
            return _unitOfWork.SupportRequestRep.GetAll(request);
        }

        public Task<SupportRequestIndexModel?> GetById(int id)
        {
            return _unitOfWork.SupportRequestRep.GetById(id);
        }

        public Task<List<SupportRequestHistory>> GetHistory(int requestId)
        {
            return _unitOfWork.SupportRequestRep.GetHistory(requestId);
        }

        public async Task<(bool Success, string Message, int Id)> Create(SupportRequestItem item, int userId)
        {
            if (item == null)
            {
                return (false, "Dữ liệu không hợp lệ.", 0);
            }

            item.Title = (item.Title ?? string.Empty).Trim();
            item.Content = (item.Content ?? string.Empty).Trim();
            item.TargetDepartmentCode = (item.TargetDepartmentCode ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(item.Title))
            {
                return (false, "Vui lòng nhập tiêu đề.", 0);
            }

            if (string.IsNullOrWhiteSpace(item.Content))
            {
                return (false, "Vui lòng nhập nội dung.", 0);
            }

            if (string.IsNullOrWhiteSpace(item.TargetDepartmentCode))
            {
                return (false, "Vui lòng chọn bộ phận xử lý.", 0);
            }

            var requester = await _unitOfWork.EmployeeRep.GetById(userId);
            if (requester == null || requester.Id <= 0)
            {
                return (false, "Không tìm thấy thông tin người tạo.", 0);
            }

            var assignee = await ResolveAssigneeAsync(item.TargetDepartmentCode, requester.Id);
            if (assignee == null || assignee.Id <= 0)
            {
                return (false, "Không tìm thấy nhân sự phụ trách cho bộ phận đã chọn.", 0);
            }

            item.RequesterId = requester.Id;
            item.AssignedToId = assignee.Id;
            item.AssignedAt = DateTime.Now;
            item.Status = 0;
            item.CompletedAt = null;
            item.ProcessorComment = string.Empty;

            var id = await _unitOfWork.SupportRequestRep.Create(item, userId);
            if (id <= 0)
            {
                return (false, "Không thể tạo yêu cầu.", 0);
            }

            await TryNotifyCreateAsync(id, requester, assignee);
            await TrySendSupportRequestEmailAsync(id, requester.Id, "Create", null);

            return (true, "Tạo yêu cầu thành công.", id);
        }

        public async Task<(bool Success, string Message)> UpdateStatus(SupportRequestStatusUpdate model, int userId, string? roleCode)
        {
            if (model == null || model.Id <= 0)
            {
                return (false, "Dữ liệu không hợp lệ.");
            }

            if (model.Status < 1 || model.Status > 3)
            {
                return (false, "Trạng thái không hợp lệ.");
            }

            var request = await _unitOfWork.SupportRequestRep.GetById(model.Id);
            if (request == null || request.Id <= 0)
            {
                return (false, "Không tìm thấy yêu cầu.");
            }

            if (!CanProcess(request, userId, roleCode))
            {
                return (false, "Bạn không có quyền cập nhật yêu cầu này.");
            }

            if (request.Status == 2 || request.Status == 3)
            {
                return (false, "Yêu cầu này đã kết thúc, không thể cập nhật tiếp.");
            }

            var updated = await _unitOfWork.SupportRequestRep.UpdateStatus(model.Id, model.Status, model.Comment, userId);
            if (!updated)
            {
                return (false, "Không thể cập nhật trạng thái.");
            }

            await TryNotifyStatusChangedAsync(model.Id, userId, model.Status);
            await TrySendSupportRequestEmailAsync(model.Id, userId, "StatusChanged", model.Comment);

            return (true, "Cập nhật trạng thái thành công.");
        }

        public async Task<(bool Success, string Message)> Delete(int id, int userId, string? roleCode)
        {
            if (id <= 0)
            {
                return (false, "Id không hợp lệ.");
            }

            var request = await _unitOfWork.SupportRequestRep.GetById(id);
            if (request == null || request.Id <= 0)
            {
                return (false, "Không tìm thấy yêu cầu.");
            }

            var isAdmin = string.Equals(roleCode, "1", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && request.RequesterId != userId)
            {
                return (false, "Bạn không có quyền xóa yêu cầu này.");
            }

            if (request.Status == 2)
            {
                return (false, "Yêu cầu đã hoàn thành, không được xóa.");
            }

            var deleted = await _unitOfWork.SupportRequestRep.Delete(id, userId);
            return deleted
                ? (true, "Xóa yêu cầu thành công.")
                : (false, "Không thể xóa yêu cầu.");
        }

        private async Task<Employee?> ResolveAssigneeAsync(string departmentCode, int requesterId)
        {
            var teamLead = await _unitOfWork.EmployeeRep.GetTeamLeadByDepartmentCode(departmentCode);
            if (teamLead != null && teamLead.Id > 0 && teamLead.Id != requesterId)
            {
                return teamLead;
            }

            var employees = await _unitOfWork.EmployeeRep.GetActiveByDepartmentCode(departmentCode);
            return employees.FirstOrDefault(x => x != null && x.Id > 0 && x.Id != requesterId);
        }

        private async Task TryNotifyCreateAsync(int requestId, Employee requester, Employee assignee)
        {
            try
            {
                var link = $"/SupportRequest/Processing?id={requestId}";
                var message = $"Bạn được giao xử lý một yêu cầu hỗ trợ mới từ {requester.FullName}.";
                await _notificationBusiness.CreateNotification(assignee.Id, message, link, SupportRequestEntityType, requester.Id);

                if (requester.Id != assignee.Id)
                {
                    await _notificationBusiness.CreateNotification(
                        requester.Id,
                        $"Yêu cầu hỗ trợ của bạn đã được giao cho {assignee.FullName}.",
                        "/SupportRequest/Request",
                        SupportRequestEntityType,
                        assignee.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot create support request notifications. RequestId={RequestId}", requestId);
            }
        }

        private async Task TryNotifyStatusChangedAsync(int requestId, int actorId, int status)
        {
            try
            {
                var request = await _unitOfWork.SupportRequestRep.GetById(requestId);
                if (request == null || request.Id <= 0)
                {
                    return;
                }

                var actor = await _unitOfWork.EmployeeRep.GetById(actorId);
                var actorName = actor?.Id > 0 ? actor.FullName : "Người xử lý";
                var statusText = GetStatusText(status);

                await _notificationBusiness.CreateNotification(
                    request.RequesterId,
                    $"Yêu cầu '{request.Title}' đã được cập nhật sang trạng thái {statusText} bởi {actorName}.",
                    "/SupportRequest/Request",
                    SupportRequestEntityType,
                    actorId);

                if (request.AssignedToId.HasValue && request.AssignedToId.Value > 0 && request.AssignedToId.Value != request.RequesterId)
                {
                    await _notificationBusiness.CreateNotification(
                        request.AssignedToId.Value,
                        $"Yêu cầu '{request.Title}' hiện đang ở trạng thái {statusText}.",
                        $"/SupportRequest/Processing?id={requestId}",
                        SupportRequestEntityType,
                        actorId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot create support request status notification. RequestId={RequestId}, Status={Status}", requestId, status);
            }
        }

        private async Task TrySendSupportRequestEmailAsync(int requestId, int actorId, string action, string? comment)
        {
            try
            {
                var request = await _unitOfWork.SupportRequestRep.GetById(requestId);
                if (request == null || request.Id <= 0)
                {
                    return;
                }

                var requester = await _unitOfWork.EmployeeRep.GetById(request.RequesterId);
                if (requester == null || requester.Id <= 0)
                {
                    return;
                }

                var assignee = request.AssignedToId.HasValue && request.AssignedToId.Value > 0
                    ? await _unitOfWork.EmployeeRep.GetById(request.AssignedToId.Value)
                    : null;

                var actor = actorId > 0 ? await _unitOfWork.EmployeeRep.GetById(actorId) : null;

                await _emailConfigBusiness.EnsureDefaultTemplates(actorId > 0 ? actorId : requester.Id);

                var plan = BuildEmailPlan(request, requester, assignee, actor, action, comment);
                if (plan == null)
                {
                    return;
                }

                RemoveDuplicateRecipients(plan.ToEmails, plan.CcEmails);
                if (plan.ToEmails.Count == 0)
                {
                    return;
                }

                var sendResult = await _emailService.SendTemplateWithErrorAsync(
                    plan.TemplateCode,
                    plan.ToEmails,
                    plan.Tokens,
                    plan.CcEmails,
                    null,
                    assignee?.Id,
                    actorId > 0 ? actorId : requester.Id,
                    new EmailSendContext
                    {
                        RelatedEntityType = SupportRequestEntityType,
                        RelatedEntityId = requestId
                    });

                if (!sendResult.Success)
                {
                    _logger.LogWarning(
                        "Support request email failed. RequestId={RequestId}, Template={TemplateCode}, Error={Error}",
                        requestId,
                        plan.TemplateCode,
                        sendResult.Error ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending support request email. RequestId={RequestId}, Action={Action}", requestId, action);
            }
        }

        private SupportRequestEmailPlan? BuildEmailPlan(
            SupportRequestIndexModel request,
            Employee requester,
            Employee? assignee,
            Employee? actor,
            string action,
            string? comment)
        {
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["RequesterName"] = requester.FullName ?? string.Empty,
                ["RequesterEmail"] = GetPreferredEmail(requester) ?? string.Empty,
                ["TargetDepartment"] = request.TargetDepartmentText ?? request.TargetDepartmentCode ?? string.Empty,
                ["AssignedToName"] = assignee?.FullName ?? request.AssignedToName ?? string.Empty,
                ["AssignedToEmail"] = GetPreferredEmail(assignee) ?? string.Empty,
                ["Title"] = request.Title ?? string.Empty,
                ["Content"] = request.Content ?? string.Empty,
                ["Status"] = request.Status.ToString(CultureInfo.InvariantCulture),
                ["StatusText"] = GetStatusText(request.Status),
                ["ProcessorComment"] = comment ?? request.ProcessorComment ?? string.Empty,
                ["ActionBy"] = actor?.FullName ?? string.Empty,
                ["CreatedAt"] = request.CreateAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                ["CompletedAt"] = request.CompletedAt?.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) ?? string.Empty
            };

            var plan = new SupportRequestEmailPlan
            {
                Tokens = tokens
            };

            if (string.Equals(action, "Create", StringComparison.OrdinalIgnoreCase))
            {
                plan.TemplateCode = SupportRequestCreateTemplateCode;
                AddEmailIfPresent(plan.ToEmails, GetPreferredEmail(assignee));
                AddAllEmployeeEmails(plan.CcEmails, requester);
                return plan;
            }

            plan.TemplateCode = request.Status switch
            {
                1 => SupportRequestInProcessTemplateCode,
                2 => SupportRequestDoneTemplateCode,
                3 => SupportRequestCancelTemplateCode,
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(plan.TemplateCode))
            {
                return null;
            }

            AddAllEmployeeEmails(plan.ToEmails, requester);
            AddEmailIfPresent(plan.CcEmails, GetPreferredEmail(assignee));
            return plan;
        }

        private static bool CanProcess(SupportRequestIndexModel request, int userId, string? roleCode)
        {
            if (string.Equals(roleCode, "1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return request.AssignedToId.HasValue && request.AssignedToId.Value == userId;
        }

        private static string GetStatusText(int status)
        {
            return status switch
            {
                0 => "Mới tạo",
                1 => "Đang xử lý",
                2 => "Hoàn thành",
                3 => "Hủy",
                _ => "Không xác định"
            };
        }

        private static string? GetPreferredEmail(Employee? employee)
        {
            if (employee == null || employee.Id <= 0)
            {
                return null;
            }

            var companyEmail = NormalizeEmail(employee.Email);
            if (!string.IsNullOrWhiteSpace(companyEmail))
            {
                return companyEmail;
            }

            return NormalizeEmail(employee.PersonalEmail);
        }

        private static string? NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var trimmed = email.Trim();
            return MailboxAddress.TryParse(trimmed, out var mailbox) ? mailbox.Address : null;
        }

        private static void AddEmailIfPresent(List<string> emails, string? email)
        {
            var normalized = NormalizeEmail(email);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                emails.Add(normalized);
            }
        }

        private static void AddAllEmployeeEmails(List<string> target, Employee? employee)
        {
            if (employee == null || employee.Id <= 0)
            {
                return;
            }

            AddEmailIfPresent(target, employee.Email);
            AddEmailIfPresent(target, employee.PersonalEmail);
        }

        private static void RemoveDuplicateRecipients(List<string> toEmails, List<string> ccEmails)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            toEmails.RemoveAll(email => !seen.Add(email));
            ccEmails.RemoveAll(email => !seen.Add(email));
        }

        private sealed class SupportRequestEmailPlan
        {
            public string TemplateCode { get; set; } = string.Empty;
            public Dictionary<string, string> Tokens { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public List<string> ToEmails { get; } = new List<string>();
            public List<string> CcEmails { get; } = new List<string>();
        }
    }
}
