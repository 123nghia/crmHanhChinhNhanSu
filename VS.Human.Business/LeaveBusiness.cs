using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VS.Human.Business.Imp;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class LeaveBusiness : BaseBusiness, ILeaveBusiness
    {
        private const string RoleHcns = "9";
        private const string RoleBgd = "8";
        private const string RoleAdmin = "1";
        private const string LeaveCreateTemplateCode = "LEAVE_CREATE";
        private const string LeaveApproveTemplateCode = "LEAVE_APPROVE";
        private const string LeaveRejectTemplateCode = "LEAVE_REJECT";
        private const string LeavePendingHcnsTemplateCode = "LEAVE_PENDING_HCNS";
        private const string LeavePendingBgdTemplateCode = "LEAVE_PENDING_BGD";
        private const string LeaveEmailEntityType = "LEAVE";

        private readonly IEmailConfigBusiness _emailConfigBusiness;
        private readonly IEmailService _emailService;
        private readonly INotificationBusiness _notificationBusiness;
        private readonly ILogger<LeaveBusiness> _logger;

        public LeaveBusiness(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor contextAccessor,
            IEmailConfigBusiness emailConfigBusiness,
            IEmailService emailService,
            INotificationBusiness notificationBusiness,
            ILogger<LeaveBusiness> logger) : base(unitOfWork, contextAccessor)
        {
            _emailConfigBusiness = emailConfigBusiness;
            _emailService = emailService;
            _notificationBusiness = notificationBusiness;
            _logger = logger;
        }

        public async Task<BaseList> GetLeaveList(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, int page, int limit, int? userId = null)
        {
            return await _unitOfWork.LeaveRep.GetAll(employeeId, status, fromDate, toDate, page, limit, userId);
        }

        public async Task<LeaveIndexModel> GetLeaveById(int id)
        {
            return await _unitOfWork.LeaveRep.GetById(id);
        }

        public async Task<int> CreateOrUpdateLeave(LeaveAddUpdate model, int userId)
        {
            // Business logic: validate dates
            if (model.FromDate > model.ToDate)
            {
                return -1; // Or throw custom exception
            }

            if (string.Equals(model.LeaveTypeCode, "NP", StringComparison.OrdinalIgnoreCase))
            {
                var today = DateTime.Today;
                if (model.FromDate.Date <= today)
                {
                    return -2;
                }
            }

            // Calculate NumDays if not provided or to ensure correctness
            // Basic logic: count days inclusive
            if (model.NumDays <= 0)
            {
                model.NumDays = (decimal)(model.ToDate.Date - model.FromDate.Date).TotalDays + 1;
            }

            var isNew = model.Id <= 0;
            var savedId = await _unitOfWork.LeaveRep.Save(model, userId);

            if (savedId > 0 && isNew)
            {
                await TrySendLeaveEmailAsync(savedId, "Create", userId, string.Empty, null);
                await TryCreateLeaveNotificationsAsync(savedId, "Create", userId, string.Empty, null);
            }

            return savedId;
        }

        public async Task<bool> ApproveLeave(int id, int status, int approverId, string comment)
        {
            // Validate status
            if (status != 1 && status != 2) return false;

            return await _unitOfWork.LeaveRep.Approve(id, status, approverId, comment);
        }

        public async Task<bool> ApproveWorkflow(int id, string action, int approverId, string roleCode, string comment)
        {
            var leave = await _unitOfWork.LeaveRep.GetById(id);
            if (leave == null || leave.Id <= 0)
            {
                return false;
            }

            if (IsAdminOrBgdRole(roleCode) && leave.EmployeeId == approverId)
            {
                return false;
            }

            var result = await _unitOfWork.LeaveRep.ApproveWorkflow(id, action, approverId, roleCode, comment);
            if (result)
            {
                await TrySendLeaveEmailAsync(id, action, approverId, roleCode, comment);
                await TryCreateLeaveNotificationsAsync(id, action, approverId, roleCode, comment);
            }
            return result;
        }

        public async Task<List<LeaveHistory>> GetLeaveHistory(int leaveId)
        {
            return await _unitOfWork.LeaveRep.GetHistory(leaveId);
        }

        public async Task<LeaveBalanceIndexModel> GetEmployeeLeaveBalance(int employeeId)
        {
            return await _unitOfWork.LeaveRep.GetEmployeeLeaveBalance(employeeId);
        }

        public async Task<bool> DeleteLeave(int id, int userId)
        {
            return await _unitOfWork.LeaveRep.Delete(id, userId);
        }

        private async Task TrySendLeaveEmailAsync(int leaveId, string action, int approverId, string roleCode, string? comment)
        {
            try
            {
                var leave = await _unitOfWork.LeaveRep.GetById(leaveId);
                if (leave == null || leave.Id <= 0)
                {
                    return;
                }

                var employee = await _unitOfWork.EmployeeRep.GetById(leave.EmployeeId);
                if (employee == null || employee.Id <= 0)
                {
                    return;
                }

                var manager = await ResolveLeaveManagerAsync(employee);

                Employee? approver = null;
                if (approverId > 0)
                {
                    var approverData = await _unitOfWork.EmployeeRep.GetById(approverId);
                    if (approverData != null && approverData.Id > 0)
                    {
                        approver = approverData;
                    }
                }

                await _emailConfigBusiness.EnsureDefaultTemplates(approverId > 0 ? approverId : employee.Id);
                var emailPlan = await BuildLeaveEmailPlanAsync(leave, employee, manager, approver, action, roleCode, comment);
                if (emailPlan == null)
                {
                    return;
                }

                RemoveDuplicateRecipients(emailPlan.ToEmails, emailPlan.CcEmails);
                if (emailPlan.ToEmails.Count == 0)
                {
                    _logger.LogInformation(
                        "Skip leave email because no valid direct recipient was resolved. LeaveId={LeaveId}, Action={Action}, Status={Status}",
                        leaveId,
                        action,
                        leave.Status);
                    return;
                }

                var sendResult = await _emailService.SendTemplateWithErrorAsync(
                    emailPlan.TemplateCode,
                    emailPlan.ToEmails,
                    emailPlan.Tokens,
                    emailPlan.CcEmails,
                    null,
                    emailPlan.ManagerId,
                    approverId > 0 ? approverId : employee.Id,
                    new EmailSendContext
                    {
                        RelatedEntityType = LeaveEmailEntityType,
                        RelatedEntityId = leaveId
                    });
                if (!sendResult.Success)
                {
                    _logger.LogWarning(
                        "Leave email failed. LeaveId={LeaveId}, Template={TemplateCode}, Action={Action}, Recipients={Recipients}, Cc={Cc}, Error={Error}",
                        leaveId,
                        emailPlan.TemplateCode,
                        action,
                        string.Join(", ", emailPlan.ToEmails),
                        string.Join(", ", emailPlan.CcEmails),
                        sendResult.Error ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending leave email. LeaveId={LeaveId}, Action={Action}", leaveId, action);
            }
        }

        private async Task<LeaveEmailPlan?> BuildLeaveEmailPlanAsync(LeaveIndexModel leave, Employee employee, Employee? manager, Employee? approver, string action, string roleCode, string? comment)
        {
            var tokens = BuildLeaveTokens(leave, employee, manager, approver, action, roleCode, comment);
            var plan = new LeaveEmailPlan
            {
                Tokens = tokens,
                ManagerId = null
            };

            if (action.Equals("Create", StringComparison.OrdinalIgnoreCase))
            {
                plan.TemplateCode = LeaveCreateTemplateCode;
                var hcnsEmails = await GetRoleEmailsAsync(RoleHcns);
                var activeEmailSetting = await _emailConfigBusiness.GetActiveSetting();
                AddAllEmployeeEmails(plan.CcEmails, employee);

                if (leave.Status == 0)
                {
                    var sendsDirectlyToHcns = IsHcnsMailboxApprover(manager, activeEmailSetting);
                    if (sendsDirectlyToHcns)
                    {
                        tokens["ManagerName"] = GetHcnsRecipientDisplayName(activeEmailSetting);
                        plan.ManagerId = null;
                        AddHcnsMailboxRecipients(plan.ToEmails, manager, activeEmailSetting, hcnsEmails);
                    }
                    else
                    {
                        AddEmailIfPresent(plan.ToEmails, GetPreferredEmail(manager));
                    }

                    AddDistinctEmails(plan.CcEmails, hcnsEmails);

                    if (plan.ToEmails.Count == 0)
                    {
                        AddDistinctEmails(plan.ToEmails, hcnsEmails);
                        tokens["ManagerName"] = GetHcnsRecipientDisplayName(activeEmailSetting);
                        plan.ManagerId = null;
                    }
                    else if (!sendsDirectlyToHcns)
                    {
                        tokens["ManagerName"] = manager?.FullName ?? "Anh/Chi phu trach";
                    }

                    return plan;
                }

                if (leave.Status == 1)
                {
                    tokens["ManagerName"] = GetHcnsRecipientDisplayName(activeEmailSetting);
                    plan.ManagerId = null;
                    AddHcnsMailboxRecipients(plan.ToEmails, manager, activeEmailSetting, hcnsEmails);

                    if (plan.ToEmails.Count == 0)
                    {
                        AddDistinctEmails(plan.ToEmails, hcnsEmails);
                    }

                    return plan;
                }

                if (leave.Status == 2)
                {
                    var bgdEmails = await GetRoleEmailsAsync(RoleBgd);
                    if (bgdEmails.Count > 0)
                    {
                        AddDistinctEmails(plan.ToEmails, bgdEmails);
                        AddDistinctEmails(plan.CcEmails, hcnsEmails);
                        tokens["ManagerName"] = "Ban Giam doc";
                    }
                    else
                    {
                        AddDistinctEmails(plan.ToEmails, hcnsEmails);
                        tokens["ManagerName"] = "Phong HCNS";
                    }

                    plan.ManagerId = null;
                    return plan;
                }

                return null;
            }

            if (action.Equals("Reject", StringComparison.OrdinalIgnoreCase))
            {
                plan.TemplateCode = LeaveRejectTemplateCode;
                AddEmailIfPresent(plan.ToEmails, GetPreferredEmail(employee));
                plan.ManagerId = null;
                await AddFinalDecisionCcRecipientsAsync(plan.CcEmails, leave, employee, manager, approver, roleCode, includeEmployee: true);
                return plan;
            }

            if (action.Equals("Acting", StringComparison.OrdinalIgnoreCase) && leave.Status == 4)
            {
                plan.TemplateCode = LeaveApproveTemplateCode;
                AddEmailIfPresent(plan.ToEmails, GetPreferredEmail(employee));
                plan.ManagerId = null;
                await AddFinalDecisionCcRecipientsAsync(plan.CcEmails, leave, employee, manager, approver, roleCode, includeEmployee: true);
                return plan;
            }

            if (!action.Equals("Agree", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (leave.Status == 1)
            {
                plan.TemplateCode = LeavePendingHcnsTemplateCode;
                AddDistinctEmails(plan.ToEmails, await GetRoleEmailsAsync(RoleHcns));
                AddLeaveFlowCcRecipients(plan.CcEmails, employee, includeEmployee: true);
                tokens["ApproverQueueName"] = "Phong HCNS";
                tokens["ApproverQueueRole"] = "HCNS";
                plan.ManagerId = null;
                return plan;
            }

            if (leave.Status == 2)
            {
                var bgdEmails = await GetRoleEmailsAsync(RoleBgd);
                if (bgdEmails.Count == 0)
                {
                    return null;
                }

                plan.TemplateCode = LeavePendingBgdTemplateCode;
                AddDistinctEmails(plan.ToEmails, bgdEmails);
                AddLeaveFlowCcRecipients(plan.CcEmails, employee, includeEmployee: true);
                tokens["ApproverQueueName"] = "Ban Giam doc";
                tokens["ApproverQueueRole"] = "BGD";
                plan.ManagerId = null;
                return plan;
            }

            if (leave.Status == 3)
            {
                plan.TemplateCode = LeaveApproveTemplateCode;
                AddEmailIfPresent(plan.ToEmails, GetPreferredEmail(employee));
                plan.ManagerId = null;
                await AddFinalDecisionCcRecipientsAsync(plan.CcEmails, leave, employee, manager, approver, roleCode, includeEmployee: true);
                return plan;
            }

            return null;
        }

        private async Task TryCreateLeaveNotificationsAsync(int leaveId, string action, int actorId, string roleCode, string? comment)
        {
            try
            {
                var leave = await _unitOfWork.LeaveRep.GetById(leaveId);
                if (leave == null || leave.Id <= 0)
                {
                    return;
                }

                var employee = await _unitOfWork.EmployeeRep.GetById(leave.EmployeeId);
                if (employee == null || employee.Id <= 0)
                {
                    return;
                }

                var manager = await ResolveLeaveManagerAsync(employee);

                Employee? actor = null;
                if (actorId > 0)
                {
                    var actorData = await _unitOfWork.EmployeeRep.GetById(actorId);
                    if (actorData != null && actorData.Id > 0)
                    {
                        actor = actorData;
                    }
                }

                var isCreate = action.Equals("Create", StringComparison.OrdinalIgnoreCase);
                var isReject = action.Equals("Reject", StringComparison.OrdinalIgnoreCase);
                var isActing = action.Equals("Acting", StringComparison.OrdinalIgnoreCase);
                var isApprove = action.Equals("Agree", StringComparison.OrdinalIgnoreCase);
                var employeeName = leave.EmployeeName ?? employee.FullName ?? "Nhan vien";
                var actorName = actor?.FullName ?? "He thong";

                if (isCreate)
                {
                    await CreateLeaveCreateNotificationsAsync(leave, employee, manager, actorId, employeeName);
                    return;
                }

                if (isReject)
                {
                    await NotifyUsersAsync(
                        new[] { employee.Id },
                        $"Don xin nghi phep cua ban da bi tu choi boi {actorName}. Ly do: {comment ?? leave.Comment ?? string.Empty}",
                        "/Leave/LeaveRequest",
                        actorId);
                    return;
                }

                if (isActing && leave.Status == 4)
                {
                    await NotifyUsersAsync(
                        new[] { employee.Id },
                        "Don xin nghi phep cua ban da duoc HCNS duyet thay Ban Giam doc.",
                        "/Leave/LeaveRequest",
                        actorId);
                    return;
                }

                if (!isApprove)
                {
                    return;
                }

                if (leave.Status == 1)
                {
                    var hcnsIds = await GetRoleUserIdsAsync(RoleHcns);
                    await NotifyUsersAsync(
                        hcnsIds,
                        $"Co don nghi phep cua {employeeName} da duoc Team Lead duyet, cho HCNS xu ly.",
                        "/Leave/LeaveApproval",
                        actorId);
                    return;
                }

                if (leave.Status == 2)
                {
                    var bgdIds = await GetRoleUserIdsAsync(RoleBgd);
                    if (bgdIds.Count == 0)
                    {
                        return;
                    }

                    await NotifyUsersAsync(
                        bgdIds,
                        $"Co don nghi phep cua {employeeName} cho BGD duyet.",
                        "/Leave/LeaveApproval",
                        actorId);

                    await NotifyUsersAsync(
                        new[] { employee.Id },
                        $"Don xin nghi phep cua ban da duoc HCNS duyet va chuyen BGD phe duyet.",
                        "/Leave/LeaveRequest",
                        actorId);
                    return;
                }

                if (leave.Status == 3)
                {
                    await NotifyUsersAsync(
                        new[] { employee.Id },
                        "Don xin nghi phep cua ban da duoc phe duyet.",
                        "/Leave/LeaveRequest",
                        actorId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating leave notifications. LeaveId={LeaveId}, Action={Action}", leaveId, action);
            }
        }

        private async Task CreateLeaveCreateNotificationsAsync(LeaveIndexModel leave, Employee employee, Employee? manager, int senderId, string employeeName)
        {
            var requesterId = employee.Id;
            var hcnsIds = await GetRoleUserIdsAsync(RoleHcns);
            if (leave.Status == 0)
            {
                await NotifyUsersAsync(
                    new[] { requesterId },
                    "Don xin nghi phep cua ban da duoc tao va gui toi Team Lead.",
                    "/Leave/LeaveRequest",
                    senderId);

                if (manager != null && manager.Id > 0)
                {
                    await NotifyUsersAsync(
                        new[] { manager.Id },
                        $"Co don nghi phep cua {employeeName} cho ban duyet.",
                        "/Leave/LeaveApproval",
                        senderId);
                }

                await NotifyUsersAsync(
                    hcnsIds,
                    $"Co don nghi phep cua {employeeName} de theo doi.",
                    "/Leave/LeaveApproval",
                    senderId,
                    requesterId,
                    manager?.Id);

                return;
            }

            if (leave.Status == 1)
            {
                await NotifyUsersAsync(
                    new[] { requesterId },
                    "Don xin nghi phep cua ban da duoc tao va gui toi Phong HCNS.",
                    "/Leave/LeaveRequest",
                    senderId);

                await NotifyUsersAsync(
                    hcnsIds,
                    $"Co don nghi phep cua {employeeName} cho phong HCNS xu ly.",
                    "/Leave/LeaveApproval",
                    senderId,
                    requesterId);

                return;
            }

            var bgdIds = await GetRoleUserIdsAsync(RoleBgd);
            var approverIds = bgdIds.Count > 0 ? bgdIds : hcnsIds;
            var approvalRoleName = bgdIds.Count > 0 ? "BGD" : "HCNS";

            if (approverIds.Count == 0)
            {
                return;
            }

            await NotifyUsersAsync(
                new[] { requesterId },
                $"Don xin nghi phep cua ban da duoc tao va gui toi {approvalRoleName}.",
                "/Leave/LeaveRequest",
                senderId);

            await NotifyUsersAsync(
                approverIds,
                $"Co don nghi phep cua {employeeName} cho ban duyet.",
                "/Leave/LeaveApproval",
                senderId,
                requesterId);

            if (bgdIds.Count > 0)
            {
                await NotifyUsersAsync(
                    hcnsIds,
                    $"Co don nghi phep cua {employeeName} de theo doi.",
                    "/Leave/LeaveApproval",
                    senderId,
                    requesterId);
            }
        }

        private async Task NotifyUsersAsync(IEnumerable<int> receiverIds, string message, string link, int? senderId, params int?[] excludedIds)
        {
            var excluded = new HashSet<int>(excludedIds.Where(x => x.HasValue && x.Value > 0).Select(x => x!.Value));
            var distinctReceiverIds = receiverIds
                .Where(id => id > 0 && !excluded.Contains(id))
                .Distinct()
                .ToList();

            foreach (var receiverId in distinctReceiverIds)
            {
                await _notificationBusiness.CreateNotification(receiverId, message, link, "LeaveRequest", senderId);
            }
        }

        private async Task<List<int>> GetRoleUserIdsAsync(params string[] roleCodes)
        {
            var employees = await _unitOfWork.EmployeeRep.GetByRoleCodes(roleCodes);
            return employees
                .Where(employee => employee.Id > 0)
                .Select(employee => employee.Id)
                .Distinct()
                .ToList();
        }

        private static Dictionary<string, string> BuildLeaveTokens(LeaveIndexModel leave, Employee employee, Employee? manager, Employee? approver, string action, string roleCode, string? comment)
        {
            var leaveType = leave.LeaveTypeName ?? leave.LeaveTypeCode ?? string.Empty;
            var totalDays = leave.NumDays?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;
            var rejectReason = comment ?? leave.Comment ?? string.Empty;
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["EmployeeName"] = leave.EmployeeName ?? employee.FullName ?? string.Empty,
                ["EmployeeEmail"] = GetPreferredEmail(employee) ?? string.Empty,
                ["ManagerName"] = manager?.FullName ?? string.Empty,
                ["ManagerEmail"] = GetPreferredEmail(manager) ?? string.Empty,
                ["LeaveTypeCode"] = leave.LeaveTypeCode ?? string.Empty,
                ["LeaveTypeName"] = leaveType,
                ["LeaveType"] = leaveType,
                ["FromDate"] = FormatDate(leave.FromDate),
                ["ToDate"] = FormatDate(leave.ToDate),
                ["NumDays"] = totalDays,
                ["TotalDays"] = totalDays,
                ["Reason"] = leave.Reason ?? string.Empty,
                ["Status"] = leave.Status.ToString(CultureInfo.InvariantCulture),
                ["StatusText"] = GetLeaveStatusText(leave.Status),
                ["ApproverName"] = approver?.FullName ?? string.Empty,
                ["ApproverRole"] = GetRoleText(roleCode),
                ["Action"] = action ?? string.Empty,
                ["Comment"] = rejectReason,
                ["RejectReason"] = rejectReason,
                ["CreateAt"] = leave.CreateAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                ["HandoverEmployeeName"] = leave.HandoverEmployeeName ?? string.Empty
            };

            return tokens;
        }

        private static void AddLeaveFlowCcRecipients(List<string> ccEmails, Employee employee, bool includeEmployee)
        {
            if (includeEmployee)
            {
                AddAllEmployeeEmails(ccEmails, employee);
            }
        }

        private async Task AddFinalDecisionCcRecipientsAsync(List<string> ccEmails, LeaveIndexModel leave, Employee employee, Employee? manager, Employee? approver, string? roleCode, bool includeEmployee)
        {
            if (includeEmployee)
            {
                AddAllEmployeeEmails(ccEmails, employee);
            }

            if (IsAdminOrBgdRole(roleCode) || leave.IsActingApproval || !string.IsNullOrWhiteSpace(leave.HCNSApproverName))
            {
                AddDistinctEmails(ccEmails, await GetRoleEmailsAsync(RoleHcns));
            }
        }

        private static string FormatDate(DateTime date)
        {
            return date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        private static string GetLeaveStatusText(int status)
        {
            return status switch
            {
                0 => "Pending Lead",
                1 => "Pending HCNS",
                2 => "Pending BGD",
                3 => "Approved",
                4 => "Approved (Acting)",
                5 => "Rejected",
                6 => "Cancelled",
                _ => status.ToString(CultureInfo.InvariantCulture)
            };
        }

        private static string GetRoleText(string? roleCode)
        {
            return roleCode switch
            {
                "1" => "Admin",
                "2" => "TC",
                "9" => "HCNS",
                "3" => "Lead",
                "8" => "BGD",
                "TL" => "Lead",
                "HCNS" => "HCNS",
                "BGD" => "BGD",
                _ => roleCode ?? string.Empty
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

            var personalEmail = NormalizeEmail(employee.PersonalEmail);
            if (!string.IsNullOrWhiteSpace(personalEmail))
            {
                return personalEmail;
            }

            return null;
        }

        private static void AddEmailIfPresent(List<string> emails, string? email)
        {
            var normalized = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            emails.Add(normalized);
        }

        private static void AddDistinctEmails(List<string> target, IEnumerable<string> emails)
        {
            foreach (var email in emails)
            {
                AddEmailIfPresent(target, email);
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

        private static string? NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var trimmed = email.Trim();
            return MailboxAddress.TryParse(trimmed, out var mailbox) ? mailbox.Address : null;
        }

        private static void RemoveDuplicateRecipients(List<string> toEmails, List<string> ccEmails)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            toEmails.RemoveAll(email => !seen.Add(email));
            ccEmails.RemoveAll(email => !seen.Add(email));
        }

        private static bool IsHcnsRole(string? roleCode)
        {
            return string.Equals(roleCode, RoleHcns, StringComparison.OrdinalIgnoreCase)
                || string.Equals(roleCode, "HCNS", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAdminOrBgdRole(string? roleCode)
        {
            return string.Equals(roleCode, RoleAdmin, StringComparison.OrdinalIgnoreCase)
                || string.Equals(roleCode, RoleBgd, StringComparison.OrdinalIgnoreCase)
                || string.Equals(roleCode, "BGD", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHcnsMailboxApprover(Employee? manager, EmailSetting? activeEmailSetting)
        {
            if (manager == null || manager.Id <= 0)
            {
                return false;
            }

            if (IsHcnsRole(manager.RoleCode))
            {
                return true;
            }

            var managerEmail = GetPreferredEmail(manager);
            if (string.IsNullOrWhiteSpace(managerEmail))
            {
                return false;
            }

            var configuredHrEmails = new[]
            {
                NormalizeEmail(activeEmailSetting?.EmployeeFromEmail),
                NormalizeEmail(activeEmailSetting?.HrFromEmail)
            };

            return configuredHrEmails.Any(email =>
                !string.IsNullOrWhiteSpace(email)
                && string.Equals(email, managerEmail, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetHcnsRecipientDisplayName(EmailSetting? activeEmailSetting)
        {
            return activeEmailSetting?.EmployeeFromName
                ?? activeEmailSetting?.HrFromName
                ?? "Phong HCNS";
        }

        private static void AddHcnsMailboxRecipients(List<string> target, Employee? manager, EmailSetting? activeEmailSetting, IEnumerable<string> fallbackEmails)
        {
            var configuredMailbox = NormalizeEmail(activeEmailSetting?.EmployeeFromEmail)
                ?? NormalizeEmail(activeEmailSetting?.HrFromEmail);

            AddEmailIfPresent(target, configuredMailbox);

            if (target.Count == 0)
            {
                AddEmailIfPresent(target, GetPreferredEmail(manager));
            }

            if (target.Count == 0)
            {
                AddDistinctEmails(target, fallbackEmails);
            }
        }

        private async Task<Employee?> ResolveLeaveManagerAsync(Employee employee)
        {
            if (employee == null || employee.Id <= 0)
            {
                return null;
            }

            if (employee.GroupId.HasValue && employee.GroupId.Value > 0)
            {
                var group = await _unitOfWork.GroupRep.GetById(employee.GroupId.Value);
                if (group != null
                    && !string.IsNullOrWhiteSpace(group.ManagerId)
                    && int.TryParse(group.ManagerId, out var groupManagerId)
                    && groupManagerId > 0
                    && groupManagerId != employee.Id)
                {
                    var groupManager = await _unitOfWork.EmployeeRep.GetById(groupManagerId);
                    if (groupManager != null && groupManager.Id > 0)
                    {
                        return groupManager;
                    }
                }
            }

            if (employee.ManagerId.HasValue
                && employee.ManagerId.Value > 0
                && employee.ManagerId.Value != employee.Id)
            {
                var directManager = await _unitOfWork.EmployeeRep.GetById(employee.ManagerId.Value);
                if (directManager != null && directManager.Id > 0)
                {
                    return directManager;
                }
            }

            return null;
        }

        private async Task<List<string>> GetRoleEmailsAsync(params string[] roleCodes)
        {
            var employees = await _unitOfWork.EmployeeRep.GetByRoleCodes(roleCodes);
            return employees
                .Select(GetPreferredEmail)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Select(email => email!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private sealed class LeaveEmailPlan
        {
            public string TemplateCode { get; set; } = string.Empty;
            public Dictionary<string, string> Tokens { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public List<string> ToEmails { get; } = new List<string>();
            public List<string> CcEmails { get; } = new List<string>();
            public int? ManagerId { get; set; }
        }
    }
}
