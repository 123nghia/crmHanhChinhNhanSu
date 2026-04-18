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

        public LeaveApprovalModel(ILeaveBusiness leaveBusiness)
        {
            _leaveBusiness = leaveBusiness;
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
    }
}
