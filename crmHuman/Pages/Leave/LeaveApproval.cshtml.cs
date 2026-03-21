using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VS.Human.Business;
using VS.Human.Rep.Model;
using VS.Human.Item;

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
            var result = await _leaveBusiness.GetLeaveHistory(id);
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostApproveAsync([FromBody] LeaveApproveRequest model)
        {
            GetInfoUser();
            if (!(Permision.Approve ?? false)) return new JsonResult(new { success = false, message = "No permission to approve" });

            var leave = await _leaveBusiness.GetLeaveById(model.Id);
            if (leave == null || leave.Id <= 0)
            {
                return new JsonResult(new { success = false, message = "Khong tim thay don nghi phep" });
            }

            if (IsAdminOrBgdRole(UserData.RoleCode) && leave.EmployeeId == UserData.UserId)
            {
                return new JsonResult(new { success = false, message = "Admin/BGD khong duoc tu xu ly don cua chinh minh" });
            }

            // model.Action should be 'Agree', 'Reject', or 'Acting'
            var result = await _leaveBusiness.ApproveWorkflow(model.Id, model.Action, UserData.UserId, UserData.RoleCode, model.Comment);
            return new JsonResult(new { success = result, message = result ? string.Empty : "Khong the xu ly don nghi phep" });
        }
    }
}
