using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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

        public LeaveApprovalModel(ILeaveBusiness leaveBusiness, IWorkflowTimelineBusiness workflowTimelineBusiness)
        {
            _leaveBusiness = leaveBusiness;
            _workflowTimelineBusiness = workflowTimelineBusiness;
            KeyPage = "LeaveApproval";
            TitlePage = "Duyet nghi phep";
        }

        public BaseList LeaveList { get; set; } = new BaseList
        {
            Data = new List<object>(),
            Total = 0
        };

        public async Task OnGetAsync(int page = 1, int limit = 20, int? status = null)
        {
            GetInfoUser();
            if (!(Permision.View ?? false) || !IsApprovalRole(UserData.RoleCode))
            {
                LeaveList = new BaseList { Data = new List<object>(), Total = 0 };
                return;
            }

            var filterStatus = ResolveApprovalStatus(UserData.RoleCode, status);
            LeaveList = await _leaveBusiness.GetLeaveList(null, filterStatus, null, null, page, limit, UserData.UserId);
        }

        public async Task<IActionResult> OnGetLeaveHistoryAsync(int id)
        {
            GetInfoUser();
            if (!(Permision.View ?? false) || !IsApprovalRole(UserData.RoleCode))
            {
                return new JsonResult(new { success = false, message = "Forbidden" });
            }

            var access = await _leaveBusiness.GetApprovalAccessAsync(id, UserData.UserId, UserData.RoleCode);
            if (!access.CanView)
            {
                return new JsonResult(new { success = false, message = access.Message ?? "Forbidden" });
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

            var access = await _leaveBusiness.GetApprovalAccessAsync(id, UserData.UserId, UserData.RoleCode);
            if (!access.CanView)
            {
                return new JsonResult(new { success = false, message = access.Message ?? "Forbidden" });
            }

            var leave = await _leaveBusiness.GetLeaveById(id);
            if (leave == null || leave.Id <= 0)
            {
                return new JsonResult(new { success = false, message = "Khong tim thay don nghi phep." });
            }

            var history = await _leaveBusiness.GetLeaveHistory(id);
            var timeline = await _workflowTimelineBusiness.GetByEntityAsync(WorkflowEntityTypes.Leave, id);
            var dueAt = ResolveDueAt(leave.Status);

            return new JsonResult(new
            {
                success = true,
                leave,
                history,
                timeline,
                currentStep = ResolveStepText(leave.Status),
                currentOwner = access.AssignedApproverName ?? ResolveOwnerText(leave.Status),
                dueAt,
                isOverdue = dueAt.HasValue && dueAt.Value < global::System.DateTime.Now,
                canView = access.CanView,
                canApprove = (Permision.Approve ?? false) && access.CanApprove,
                canReject = (Permision.Approve ?? false) && access.CanReject,
                canActingApprove = (Permision.Approve ?? false) && access.CanActingApprove
            });
        }

        public async Task<IActionResult> OnPostApproveAsync([FromBody] LeaveApproveRequest model)
        {
            GetInfoUser();
            if (!(Permision.Approve ?? false))
            {
                return new JsonResult(new { success = false, message = "Ban khong co quyen phe duyet." });
            }

            var normalizedAction = ResolveApprovalAction(model.Action);
            if (normalizedAction == null)
            {
                return new JsonResult(new { success = false, message = "Thao tac khong hop le." });
            }

            var leave = await _leaveBusiness.GetLeaveById(model.Id);
            if (leave == null || leave.Id <= 0)
            {
                return new JsonResult(new { success = false, message = "Khong tim thay don nghi phep." });
            }

            var access = await _leaveBusiness.GetApprovalAccessAsync(model.Id, UserData.UserId, UserData.RoleCode);
            if (!access.CanView || !IsAllowedAction(normalizedAction, access))
            {
                return new JsonResult(new { success = false, message = access.Message ?? "Ban khong co quyen xu ly don nghi phep nay." });
            }

            var result = await _leaveBusiness.ApproveWorkflow(model.Id, normalizedAction, UserData.UserId, UserData.RoleCode, model.Comment);
            return new JsonResult(new { success = result, message = result ? string.Empty : "Khong the xu ly don nghi phep." });
        }

        private static bool IsApprovalRole(string? roleCode)
        {
            return roleCode == "1" || roleCode == "3" || roleCode == "8" || roleCode == "9";
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

        private static string? ResolveApprovalAction(string? action)
        {
            if (string.Equals(action, "Agree", global::System.StringComparison.OrdinalIgnoreCase))
            {
                return "Agree";
            }

            if (string.Equals(action, "Reject", global::System.StringComparison.OrdinalIgnoreCase))
            {
                return "Reject";
            }

            if (string.Equals(action, "Acting", global::System.StringComparison.OrdinalIgnoreCase))
            {
                return "Acting";
            }

            return null;
        }

        private static bool IsAllowedAction(string? action, LeaveApprovalAccessResult access)
        {
            if (access == null)
            {
                return false;
            }

            return action switch
            {
                "Agree" => access.CanApprove,
                "Reject" => access.CanReject,
                "Acting" => access.CanActingApprove,
                _ => false
            };
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
