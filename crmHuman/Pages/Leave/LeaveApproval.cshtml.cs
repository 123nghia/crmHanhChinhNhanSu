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
            TitlePage = "Duyệt nghỉ phép";
        }

        public BaseList LeaveList { get; set; } = new BaseList
        {
            Data = new List<object>(),
            Total = 0
        };

        public int DeepLinkLeaveId { get; private set; }

        public bool IsDeepLinkMode => DeepLinkLeaveId > 0;

        public async Task OnGetAsync(int page = 1, int limit = 20, int? status = null, int? id = null)
        {
            GetInfoUser();
            DeepLinkLeaveId = id ?? 0;
            if (!(Permision.View ?? false) || !IsApprovalRole(UserData.RoleCode))
            {
                LeaveList = new BaseList { Data = new List<object>(), Total = 0 };
                return;
            }

            if (IsDeepLinkMode)
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
                return new JsonResult(new { success = false, message = "Bạn không có quyền xem lịch sử xử lý đơn này." });
            }

            var access = await _leaveBusiness.GetApprovalAccessAsync(id, UserData.UserId, UserData.RoleCode);
            if (!access.CanView)
            {
                return new JsonResult(new { success = false, message = access.Message ?? "Bạn không có quyền xem đơn nghỉ phép này." });
            }

            var result = await _leaveBusiness.GetLeaveHistory(id);
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnGetWorkflowDetailAsync(int id)
        {
            GetInfoUser();
            if (!(Permision.View ?? false) || !IsApprovalRole(UserData.RoleCode))
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền xem đơn nghỉ phép này." });
            }

            var leave = await _leaveBusiness.GetLeaveById(id);
            if (leave == null || leave.Id <= 0)
            {
                return new JsonResult(new { success = false, message = "Đơn nghỉ phép không tồn tại hoặc đã bị xóa." });
            }

            var access = await _leaveBusiness.GetApprovalAccessAsync(id, UserData.UserId, UserData.RoleCode);
            if (!access.CanView)
            {
                return new JsonResult(new { success = false, message = access.Message ?? "Bạn không có quyền xem đơn nghỉ phép này." });
            }

            var history = await _leaveBusiness.GetLeaveHistory(id);
            var timeline = await _workflowTimelineBusiness.GetByEntityAsync(WorkflowEntityTypes.Leave, id);
            var dueAt = ResolveDueAt(leave.Status);
            var canApprove = (Permision.Approve ?? false) && access.CanApprove;
            var canReject = (Permision.Approve ?? false) && access.CanReject;
            var canActingApprove = (Permision.Approve ?? false) && access.CanActingApprove;

            return new JsonResult(new
            {
                success = true,
                leave,
                history,
                timeline,
                currentStep = ResolveStepText(leave.Status),
                currentOwner = FirstNonEmpty(access.AssignedApproverName, ResolveOwnerText(leave.Status)),
                dueAt,
                isOverdue = dueAt.HasValue && dueAt.Value < global::System.DateTime.Now,
                stateMessage = ResolveStateMessage(leave, canApprove, canReject, canActingApprove),
                stateTone = ResolveStateTone(leave.Status, canApprove, canReject, canActingApprove),
                isActionable = canApprove || canReject || canActingApprove,
                canView = access.CanView,
                canApprove,
                canReject,
                canActingApprove
            });
        }

        public async Task<IActionResult> OnPostApproveAsync([FromBody] LeaveApproveRequest model)
        {
            GetInfoUser();
            if (!(Permision.Approve ?? false))
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền phê duyệt." });
            }

            var normalizedAction = ResolveApprovalAction(model.Action);
            if (normalizedAction == null)
            {
                return new JsonResult(new { success = false, message = "Thao tác không hợp lệ." });
            }

            var leave = await _leaveBusiness.GetLeaveById(model.Id);
            if (leave == null || leave.Id <= 0)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy đơn nghỉ phép." });
            }

            var access = await _leaveBusiness.GetApprovalAccessAsync(model.Id, UserData.UserId, UserData.RoleCode);
            if (!access.CanView || !IsAllowedAction(normalizedAction, access))
            {
                return new JsonResult(new { success = false, message = access.Message ?? "Bạn không có quyền xử lý đơn nghỉ phép này." });
            }

            var result = await _leaveBusiness.ApproveWorkflow(model.Id, normalizedAction, UserData.UserId, UserData.RoleCode, model.Comment);
            return new JsonResult(new { success = result, message = result ? string.Empty : "Không thể xử lý đơn nghỉ phép." });
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
                0 => "Chờ quản lý trực tiếp",
                1 => "HCNS",
                2 => "BGD",
                3 => "Đã phê duyệt",
                4 => "Đã phê duyệt (duyệt thay)",
                5 => "Từ chối",
                6 => "Đã hủy",
                _ => "Không xác định"
            };
        }

        private static string ResolveOwnerText(int status)
        {
            return status switch
            {
                0 => "Quản lý trực tiếp",
                1 => "Phòng HCNS",
                2 => "Ban Giám đốc",
                _ => string.Empty
            };
        }

        private static string ResolveStateMessage(LeaveIndexModel leave, bool canApprove, bool canReject, bool canActingApprove)
        {
            if (canApprove || canReject || canActingApprove)
            {
                return "Đơn đang chờ bạn xử lý. Kiểm tra nội dung bên dưới trước khi phê duyệt hoặc từ chối.";
            }

            var approverName = FirstNonEmpty(
                leave.ApproverName,
                leave.BGDApproverName,
                leave.HCNSApproverName,
                leave.LeadApproverName,
                "người xử lý");

            return leave.Status switch
            {
                0 => "Đơn đang chờ quản lý trực tiếp phê duyệt. Bạn chỉ có thể xem trạng thái hiện tại.",
                1 => "Đơn đang chờ Phòng HCNS phê duyệt. Bạn chỉ có thể xem trạng thái hiện tại.",
                2 => "Đơn đang chờ Ban Giám đốc phê duyệt. Bạn chỉ có thể xem trạng thái hiện tại.",
                3 => $"Đơn đã được phê duyệt bởi {approverName}. Không còn thao tác duyệt trên link này.",
                4 => $"Đơn đã được HCNS duyệt thay Ban Giám đốc bởi {approverName}. Không còn thao tác duyệt trên link này.",
                5 => $"Đơn đã bị từ chối bởi {approverName}. Xem chi tiết và ghi chú xử lý bên dưới.",
                6 => "Đơn đã bị hủy. Link này chỉ hiển thị thông tin hiện tại của đơn.",
                _ => "Không xác định được trạng thái hiện tại của đơn."
            };
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }

        private static string ResolveStateTone(int status, bool canApprove, bool canReject, bool canActingApprove)
        {
            if (canApprove || canReject || canActingApprove)
            {
                return "warning";
            }

            return status switch
            {
                3 or 4 => "success",
                5 => "danger",
                6 => "secondary",
                0 or 1 or 2 => "info",
                _ => "secondary"
            };
        }
    }
}
