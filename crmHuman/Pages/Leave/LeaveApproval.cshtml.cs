using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.Leave
{
    public class LeaveApprovalModel : BaseModel2
    {
        private readonly ILeaveBusiness _leaveBusiness;
        private readonly IWorkflowTimelineBusiness _workflowTimelineBusiness;

        private static bool IsApprovalRole(string? roleCode)
        {
            return roleCode == "1" || roleCode == "3" || roleCode == "8" || roleCode == "9";
        }

        private static bool IsAdminOrBgdRole(string? roleCode)
        {
            return roleCode == "1" || roleCode == "8";
        }

        private static int? ResolveApprovalStatus(string? roleCode, int? requestedStatus)
        {
            if (requestedStatus.HasValue)
            {
                return requestedStatus;
            }

            return roleCode switch
            {
                "3" => 0,
                "9" => 1,
                "8" => 2,
                "1" => null,
                _ => -999
            };
        }

        private static bool IsStatusAllowedForAction(string? roleCode, string? action, int status)
        {
            if (roleCode == "1")
            {
                return true;
            }

            if (string.Equals(action, "Reject", global::System.StringComparison.OrdinalIgnoreCase))
            {
                return roleCode switch
                {
                    "3" => status == 0,
                    "9" => status == 1,
                    "8" => status is 2 or 3 or 4,
                    _ => false
                };
            }

            if (string.Equals(action, "Acting", global::System.StringComparison.OrdinalIgnoreCase))
            {
                return roleCode == "9" && status is 1 or 2;
            }

            return roleCode switch
            {
                "3" => status == 0,
                "9" => status == 1,
                "8" => status == 2,
                _ => false
            };
        }

        private async Task<bool> CanAccessApprovalAsync(LeaveIndexModel? leave, string? action = null)
        {
            if (leave == null || leave.Id <= 0 || UserData?.UserId <= 0 || !IsApprovalRole(UserData.RoleCode))
            {
                return false;
            }

            if (!IsStatusAllowedForAction(UserData.RoleCode, action, leave.Status))
            {
                return false;
            }

            var leaveList = await _leaveBusiness.GetLeaveList(null, null, null, null, 1, 5000, UserData.UserId);
            var items = leaveList.Data?.OfType<LeaveIndexModel>() ?? Enumerable.Empty<LeaveIndexModel>();
            return items.Any(item => item.Id == leave.Id);
        }

        public LeaveApprovalModel(ILeaveBusiness leaveBusiness, IWorkflowTimelineBusiness workflowTimelineBusiness)
        {
            _leaveBusiness = leaveBusiness;
            _workflowTimelineBusiness = workflowTimelineBusiness;
            KeyPage = "LeaveApproval";
            TitlePage = "Duyệt nghỉ phép";
        }

        public BaseList LeaveList { get; set; }

        public async Task OnGetAsync(int page = 1, int limit = 20, int? status = null)
        {
            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                LeaveList = new BaseList { Data = new List<object>(), Total = 0 };
            }
            else
            {
                if (!IsApprovalRole(UserData.RoleCode))
                {
                    LeaveList = new BaseList { Data = new List<object>(), Total = 0 };
                    return;
                }

                int? filterStatus = ResolveApprovalStatus(UserData.RoleCode, status);
                LeaveList = await _leaveBusiness.GetLeaveList(null, filterStatus, null, null, page, limit, UserData.UserId);
            }
        }

        public async Task<IActionResult> OnGetLeaveHistoryAsync(int id)
        {
            GetInfoUser();
            if (!(Permision.View ?? false) || !IsApprovalRole(UserData.RoleCode))
            {
                return new JsonResult(new { success = false, message = "Forbidden" });
            }

            var leave = await _leaveBusiness.GetLeaveById(id);
            if (!await CanAccessApprovalAsync(leave))
            {
                return new JsonResult(new { success = false, message = "Forbidden" });
            }

            var result = await _leaveBusiness.GetLeaveHistory(id);
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnGetWorkflowDetailAsync(int id)
        {
            GetInfoUser();
            if (!(Permision.View ?? false) || !IsApprovalRole(UserData.RoleCode))
            {
                return new JsonResult(new { success = false, message = "Forbidden" });
            }

            var leave = await _leaveBusiness.GetLeaveById(id);
            if (!await CanAccessApprovalAsync(leave))
            {
                return new JsonResult(new { success = false, message = "Forbidden" });
            }

            var history = await _leaveBusiness.GetLeaveHistory(id);
            var timeline = await _workflowTimelineBusiness.GetByEntityAsync(WorkflowEntityTypes.Leave, id);
            var dueAt = ResolveDueAt(leave.Status);
            var canAct = (Permision.Approve ?? false)
                && !((UserData.RoleCode == "1" || UserData.RoleCode == "8") && leave.EmployeeId == UserData.UserId)
                && IsStatusAllowedForAction(UserData.RoleCode, "Agree", leave.Status);

            return new JsonResult(new
            {
                success = true,
                leave,
                history,
                timeline,
                currentStep = ResolveStepText(leave.Status),
                currentOwner = ResolveOwnerText(leave.Status),
                dueAt,
                isOverdue = dueAt.HasValue && dueAt.Value < global::System.DateTime.Now,
                canAct
            });
        }

        public async Task<IActionResult> OnPostApproveAsync([FromBody] LeaveApproveRequest model)
        {
            GetInfoUser();
            if (!(Permision.Approve ?? false))
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền phê duyệt." });
            }

            var leave = await _leaveBusiness.GetLeaveById(model.Id);
            if (leave == null || leave.Id <= 0)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy đơn nghỉ phép." });
            }

            if (IsAdminOrBgdRole(UserData.RoleCode) && leave.EmployeeId == UserData.UserId)
            {
                return new JsonResult(new { success = false, message = "Admin/BGĐ không được tự xử lý đơn của chính mình." });
            }

            if (!await CanAccessApprovalAsync(leave, model.Action))
            {
                return new JsonResult(new { success = false, message = "Ban khong co quyen xu ly don nghi phep nay." });
            }

            var result = await _leaveBusiness.ApproveWorkflow(model.Id, model.Action, UserData.UserId, UserData.RoleCode, model.Comment);
            return new JsonResult(new { success = result, message = result ? string.Empty : "Không thể xử lý đơn nghỉ phép." });
        }
        private static global::System.DateTime? ResolveDueAt(int status)
        {
            var hours = status switch
            {
                0 => 24,
                1 => 12,
                2 => 24,
                _ => 0
            };

            return hours > 0 ? global::System.DateTime.Now.AddHours(hours) : null;
        }

        private static string ResolveStepText(int status)
        {
            return status switch
            {
                0 => "Manager",
                1 => "HCNS",
                2 => "BGD",
                3 => "Final approved",
                4 => "Final approved",
                5 => "Rejected",
                6 => "Cancelled",
                _ => "Unknown"
            };
        }

        private static string ResolveOwnerText(int status)
        {
            return status switch
            {
                0 => "Quan ly truc tiep",
                1 => "Phong HCNS",
                2 => "Ban Giam doc",
                _ => string.Empty
            };
        }
    }
}
