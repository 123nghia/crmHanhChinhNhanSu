using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using VS.Human.Business.Imp;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class LateEarlyBusiness : BaseBusiness, ILateEarlyBusiness
    {
        private const string RoleHcns = "9";
        private const string RoleBgd = "8";
        private const string RoleAdmin = "1";
        private const string LateEarlyCreateTemplateCode = "LATE_EARLY_CREATE";
        private const string LateEarlyApproveTemplateCode = "LATE_EARLY_APPROVE";
        private const string LateEarlyRejectTemplateCode = "LATE_EARLY_REJECT";
        private const string LateEarlyPendingHcnsTemplateCode = "LATE_EARLY_PENDING_HCNS";
        private const string LateEarlyPendingBgdTemplateCode = "LATE_EARLY_PENDING_BGD";
        private const string LateEarlyPendingAdminTemplateCode = "LATE_EARLY_PENDING_ADMIN";
        private const string LateEarlyEmailEntityType = "LATE_EARLY";

        private readonly IEmailConfigBusiness _emailConfigBusiness;
        private readonly IEmailService _emailService;
        private readonly ILogger<LateEarlyBusiness> _logger;

        public LateEarlyBusiness(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor contextAccessor,
            IEmailConfigBusiness emailConfigBusiness,
            IEmailService emailService,
            ILogger<LateEarlyBusiness> logger)
            : base(unitOfWork, contextAccessor)
        {
            _emailConfigBusiness = emailConfigBusiness;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<BaseList> GetList(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, int page, int limit)
        {
            return await _unitOfWork.LateEarlyRep.GetAll(employeeId, status, fromDate, toDate, page, limit);
        }

        public async Task<LateEarlyIndexModel?> GetById(int id)
        {
            return await _unitOfWork.LateEarlyRep.GetById(id);
        }

        public async Task<int> CreateOrUpdate(LateEarlyAddUpdate model, int userId)
        {
            if (model.StartTime >= model.EndTime)
            {
                return -1;
            }

            if (model.RequestDate.Date != model.StartTime.Date || model.RequestDate.Date != model.EndTime.Date)
            {
                return -2;
            }

            if (model.EmployeeId <= 0)
            {
                return -3;
            }

            var status = 0;
            var employee = await _unitOfWork.EmployeeRep.GetById(model.EmployeeId);
            if (employee == null || employee.Id <= 0)
            {
                return -4;
            }

            if (!employee.ManagerId.HasValue || employee.ManagerId.Value <= 0)
            {
                status = 1;
            }

            var isNew = model.Id <= 0;
            var savedId = await _unitOfWork.LateEarlyRep.Save(model, userId, status);
            if (savedId > 0 && isNew)
            {
                await TrySendLateEarlyEmailAsync(savedId, "Create", userId, string.Empty, null);
            }

            return savedId;
        }

        public async Task<bool> ApproveWorkflow(int id, string action, int approverId, string roleCode, string? comment)
        {
            var result = await _unitOfWork.LateEarlyRep.ApproveWorkflow(id, action, approverId, roleCode, comment);
            if (result)
            {
                await TrySendLateEarlyEmailAsync(id, action, approverId, roleCode, comment);
            }

            return result;
        }

        public async Task<List<LateEarlyHistory>> GetHistory(int requestId)
        {
            return await _unitOfWork.LateEarlyRep.GetHistory(requestId);
        }

        public async Task<bool> Delete(int id, int userId)
        {
            return await _unitOfWork.LateEarlyRep.Delete(id, userId);
        }

        private async Task TrySendLateEarlyEmailAsync(int requestId, string action, int actorId, string roleCode, string? comment)
        {
            try
            {
                var request = await _unitOfWork.LateEarlyRep.GetById(requestId);
                if (request == null || request.Id <= 0)
                {
                    return;
                }

                var employee = await _unitOfWork.EmployeeRep.GetById(request.EmployeeId);
                if (employee == null || employee.Id <= 0)
                {
                    return;
                }

                var manager = await ResolveManagerAsync(employee);

                Employee? actor = null;
                if (actorId > 0)
                {
                    var actorData = await _unitOfWork.EmployeeRep.GetById(actorId);
                    if (actorData != null && actorData.Id > 0)
                    {
                        actor = actorData;
                    }
                }

                await _emailConfigBusiness.EnsureDefaultTemplates(actorId > 0 ? actorId : employee.Id);
                var emailPlan = await BuildLateEarlyEmailPlanAsync(request, employee, manager, actor, action, roleCode, comment);
                if (emailPlan == null)
                {
                    return;
                }

                RemoveDuplicateRecipients(emailPlan.ToEmails, emailPlan.CcEmails);
                if (emailPlan.ToEmails.Count == 0)
                {
                    _logger.LogInformation(
                        "Skip late-early email because no valid direct recipient was resolved. RequestId={RequestId}, Action={Action}, Status={Status}",
                        requestId,
                        action,
                        request.Status);
                    return;
                }

                var sendResult = await _emailService.SendTemplateWithErrorAsync(
                    emailPlan.TemplateCode,
                    emailPlan.ToEmails,
                    emailPlan.Tokens,
                    emailPlan.CcEmails,
                    null,
                    emailPlan.ManagerId,
                    actorId > 0 ? actorId : employee.Id,
                    new EmailSendContext
                    {
                        RelatedEntityType = LateEarlyEmailEntityType,
                        RelatedEntityId = requestId
                    });

                if (!sendResult.Success)
                {
                    _logger.LogWarning(
                        "Late-early email failed. RequestId={RequestId}, Template={TemplateCode}, Action={Action}, Recipients={Recipients}, Cc={Cc}, Error={Error}",
                        requestId,
                        emailPlan.TemplateCode,
                        action,
                        string.Join(", ", emailPlan.ToEmails),
                        string.Join(", ", emailPlan.CcEmails),
                        sendResult.Error ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending late-early email. RequestId={RequestId}, Action={Action}", requestId, action);
            }
        }

        private async Task<LateEarlyEmailPlan?> BuildLateEarlyEmailPlanAsync(LateEarlyIndexModel request, Employee employee, Employee? manager, Employee? actor, string action, string roleCode, string? comment)
        {
            var tokens = BuildLateEarlyTokens(request, employee, manager, actor, action, roleCode, comment);
            var plan = new LateEarlyEmailPlan
            {
                Tokens = tokens
            };

            if (action.Equals("Create", StringComparison.OrdinalIgnoreCase))
            {
                var hcnsEmails = await GetRoleEmailsAsync(RoleHcns);
                AddAllEmployeeEmails(plan.CcEmails, employee);

                if (request.Status == 0)
                {
                    plan.TemplateCode = LateEarlyCreateTemplateCode;
                    AddEmailIfPresent(plan.ToEmails, GetPreferredEmail(manager));
                    AddDistinctEmails(plan.CcEmails, hcnsEmails);
                    tokens["ManagerName"] = manager?.FullName ?? "Anh/Chi phu trach";

                    if (plan.ToEmails.Count == 0)
                    {
                        AddDistinctEmails(plan.ToEmails, hcnsEmails);
                        tokens["ManagerName"] = "Phong HCNS";
                    }

                    return plan;
                }

                if (request.Status == 1)
                {
                    plan.TemplateCode = LateEarlyPendingHcnsTemplateCode;
                    AddDistinctEmails(plan.ToEmails, hcnsEmails);
                    tokens["ManagerName"] = "Phong HCNS";
                    return plan;
                }

                if (request.Status == 2)
                {
                    plan.TemplateCode = LateEarlyPendingBgdTemplateCode;
                    var bgdEmails = await GetRoleEmailsAsync(RoleBgd);
                    AddDistinctEmails(plan.ToEmails, bgdEmails.Count > 0 ? bgdEmails : hcnsEmails);
                    if (bgdEmails.Count > 0)
                    {
                        AddDistinctEmails(plan.CcEmails, hcnsEmails);
                    }

                    tokens["ManagerName"] = bgdEmails.Count > 0 ? "Ban Giam doc" : "Phong HCNS";
                    return plan;
                }

                if (request.Status == 3)
                {
                    plan.TemplateCode = LateEarlyPendingAdminTemplateCode;
                    var adminEmails = await GetRoleEmailsAsync(RoleAdmin);
                    var bgdEmails = await GetRoleEmailsAsync(RoleBgd);
                    AddDistinctEmails(plan.ToEmails, adminEmails.Count > 0 ? adminEmails : bgdEmails);
                    AddDistinctEmails(plan.CcEmails, hcnsEmails);
                    AddDistinctEmails(plan.CcEmails, bgdEmails);
                    tokens["ManagerName"] = "Admin";
                    return plan;
                }

                return null;
            }

            if (action.Equals("Reject", StringComparison.OrdinalIgnoreCase))
            {
                plan.TemplateCode = LateEarlyRejectTemplateCode;
                AddEmailIfPresent(plan.ToEmails, GetPreferredEmail(employee));
                AddAllEmployeeEmails(plan.CcEmails, employee);
                if (IsAdminOrBgdRole(roleCode) || IsHcnsRole(roleCode))
                {
                    AddDistinctEmails(plan.CcEmails, await GetRoleEmailsAsync(RoleHcns));
                }

                return plan;
            }

            if (!action.Equals("Agree", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (request.Status == 1)
            {
                plan.TemplateCode = LateEarlyPendingHcnsTemplateCode;
                AddDistinctEmails(plan.ToEmails, await GetRoleEmailsAsync(RoleHcns));
                AddAllEmployeeEmails(plan.CcEmails, employee);
                return plan;
            }

            if (request.Status == 2)
            {
                var bgdEmails = await GetRoleEmailsAsync(RoleBgd);
                if (bgdEmails.Count == 0)
                {
                    return null;
                }

                plan.TemplateCode = LateEarlyPendingBgdTemplateCode;
                AddDistinctEmails(plan.ToEmails, bgdEmails);
                AddAllEmployeeEmails(plan.CcEmails, employee);
                AddDistinctEmails(plan.CcEmails, await GetRoleEmailsAsync(RoleHcns));
                return plan;
            }

            if (request.Status == 3)
            {
                var adminEmails = await GetRoleEmailsAsync(RoleAdmin);
                if (adminEmails.Count == 0)
                {
                    return null;
                }

                plan.TemplateCode = LateEarlyPendingAdminTemplateCode;
                AddDistinctEmails(plan.ToEmails, adminEmails);
                AddAllEmployeeEmails(plan.CcEmails, employee);
                AddDistinctEmails(plan.CcEmails, await GetRoleEmailsAsync(RoleHcns));
                AddDistinctEmails(plan.CcEmails, await GetRoleEmailsAsync(RoleBgd));
                return plan;
            }

            if (request.Status == 4)
            {
                plan.TemplateCode = LateEarlyApproveTemplateCode;
                AddEmailIfPresent(plan.ToEmails, GetPreferredEmail(employee));
                AddAllEmployeeEmails(plan.CcEmails, employee);
                AddDistinctEmails(plan.CcEmails, await GetRoleEmailsAsync(RoleHcns));
                return plan;
            }

            return null;
        }

        private static Dictionary<string, string> BuildLateEarlyTokens(LateEarlyIndexModel request, Employee employee, Employee? manager, Employee? actor, string action, string roleCode, string? comment)
        {
            var requestTypeName = GetRequestTypeName(request.RequestType);
            var rejectReason = comment ?? request.Comment ?? string.Empty;
            var durationMinutes = Math.Max(0, (int)Math.Round((request.EndTime - request.StartTime).TotalMinutes));

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["EmployeeName"] = request.EmployeeName ?? employee.FullName ?? string.Empty,
                ["EmployeeEmail"] = GetPreferredEmail(employee) ?? string.Empty,
                ["ManagerName"] = manager?.FullName ?? string.Empty,
                ["ManagerEmail"] = GetPreferredEmail(manager) ?? string.Empty,
                ["RequestType"] = request.RequestType ?? string.Empty,
                ["RequestTypeName"] = requestTypeName,
                ["RequestDate"] = request.RequestDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                ["StartTime"] = request.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                ["EndTime"] = request.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                ["TimeRange"] = string.Format(CultureInfo.InvariantCulture, "{0:HH:mm} - {1:HH:mm}", request.StartTime, request.EndTime),
                ["DurationMinutes"] = durationMinutes.ToString(CultureInfo.InvariantCulture),
                ["Reason"] = request.Reason ?? string.Empty,
                ["Status"] = request.Status.ToString(CultureInfo.InvariantCulture),
                ["StatusText"] = GetLateEarlyStatusText(request.Status),
                ["ApproverName"] = actor?.FullName ?? string.Empty,
                ["ApproverRole"] = GetRoleText(roleCode),
                ["Action"] = action ?? string.Empty,
                ["Comment"] = rejectReason,
                ["RejectReason"] = rejectReason,
                ["CreateAt"] = request.CreateAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
            };
        }

        private async Task<Employee?> ResolveManagerAsync(Employee employee)
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

        private static string GetRequestTypeName(string? requestType)
        {
            return string.Equals(requestType, "EARLY", StringComparison.OrdinalIgnoreCase)
                ? "ve som"
                : "di tre";
        }

        private static string GetLateEarlyStatusText(int status)
        {
            return status switch
            {
                0 => "Pending Lead",
                1 => "Pending HCNS",
                2 => "Pending BGD",
                3 => "Pending Admin",
                4 => "Approved",
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
                "ADMIN" => "Admin",
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
                || string.Equals(roleCode, "BGD", StringComparison.OrdinalIgnoreCase)
                || string.Equals(roleCode, "ADMIN", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class LateEarlyEmailPlan
        {
            public string TemplateCode { get; set; } = string.Empty;
            public Dictionary<string, string> Tokens { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public List<string> ToEmails { get; } = new List<string>();
            public List<string> CcEmails { get; } = new List<string>();
            public int? ManagerId { get; set; }
        }
    }
}
