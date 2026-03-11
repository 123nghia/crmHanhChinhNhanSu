using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using VS.Human.Business.Imp;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class LeaveBusiness : BaseBusiness, ILeaveBusiness
    {
        private readonly IEmailService _emailService;

        public LeaveBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor, IEmailService emailService) : base(unitOfWork, contextAccessor)
        {
            _emailService = emailService;
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
                await TrySendLeaveEmailAsync(savedId, "LEAVE_CREATE", "Create", userId, string.Empty, true, null);
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
            var result = await _unitOfWork.LeaveRep.ApproveWorkflow(id, action, approverId, roleCode, comment);
            if (result)
            {
                var templateCode = action.Equals("Reject", StringComparison.OrdinalIgnoreCase) ? "LEAVE_REJECT" : "LEAVE_APPROVE";
                await TrySendLeaveEmailAsync(id, templateCode, action, approverId, roleCode, false, comment);
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

        private async Task TrySendLeaveEmailAsync(int leaveId, string templateCode, string action, int approverId, string roleCode, bool sendToManager, string? comment)
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

                Employee? manager = null;
                if (employee.ManagerId.HasValue && employee.ManagerId.Value > 0)
                {
                    var managerData = await _unitOfWork.EmployeeRep.GetById(employee.ManagerId.Value);
                    if (managerData != null && managerData.Id > 0)
                    {
                        manager = managerData;
                    }
                }

                Employee? approver = null;
                if (approverId > 0)
                {
                    var approverData = await _unitOfWork.EmployeeRep.GetById(approverId);
                    if (approverData != null && approverData.Id > 0)
                    {
                        approver = approverData;
                    }
                }

                var toEmail = sendToManager ? GetPreferredEmail(manager) : GetPreferredEmail(employee);
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    return;
                }

                var tokens = BuildLeaveTokens(leave, employee, manager, approver, action, roleCode, comment);
                await _emailService.SendTemplateAsync(templateCode, toEmail, tokens, manager?.Id);
            }
            catch
            {
            }
        }

        private static Dictionary<string, string> BuildLeaveTokens(LeaveIndexModel leave, Employee employee, Employee? manager, Employee? approver, string action, string roleCode, string? comment)
        {
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["EmployeeName"] = leave.EmployeeName ?? employee.FullName ?? string.Empty,
                ["EmployeeEmail"] = GetPreferredEmail(employee) ?? string.Empty,
                ["ManagerName"] = manager?.FullName ?? string.Empty,
                ["ManagerEmail"] = GetPreferredEmail(manager) ?? string.Empty,
                ["LeaveTypeCode"] = leave.LeaveTypeCode ?? string.Empty,
                ["LeaveTypeName"] = leave.LeaveTypeName ?? leave.LeaveTypeCode ?? string.Empty,
                ["FromDate"] = FormatDate(leave.FromDate),
                ["ToDate"] = FormatDate(leave.ToDate),
                ["NumDays"] = leave.NumDays?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty,
                ["Reason"] = leave.Reason ?? string.Empty,
                ["Status"] = leave.Status.ToString(CultureInfo.InvariantCulture),
                ["StatusText"] = GetLeaveStatusText(leave.Status),
                ["ApproverName"] = approver?.FullName ?? string.Empty,
                ["ApproverRole"] = GetRoleText(roleCode),
                ["Action"] = action ?? string.Empty,
                ["Comment"] = comment ?? leave.Comment ?? string.Empty,
                ["CreateAt"] = leave.CreateAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                ["HandoverEmployeeName"] = leave.HandoverEmployeeName ?? string.Empty
            };

            return tokens;
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

            if (!string.IsNullOrWhiteSpace(employee.Email))
            {
                return employee.Email;
            }

            if (!string.IsNullOrWhiteSpace(employee.PersonalEmail))
            {
                return employee.PersonalEmail;
            }

            return null;
        }
    }
}
