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
                int? filterStatus = status;
                // If status is not specified, auto-filter based on role
                if (filterStatus == null && UserData.RoleCode != "1")
                {
                    if (UserData.RoleCode == "3") filterStatus = 0; // TL (Lead) sees requests pending Lead
                    else if (UserData.RoleCode == "2") filterStatus = 1; // TC (HCNS) sees requests pending HCNS
                    else filterStatus = 2; // Others see pending BGD (assuming)
                }

                LeaveList = await _leaveBusiness.GetLeaveList(null, filterStatus, null, null, page, limit);
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

            // model.Action should be 'Agree', 'Reject', or 'Acting'
            var result = await _leaveBusiness.ApproveWorkflow(model.Id, model.Action, UserData.UserId, UserData.RoleCode, model.Comment);
            return new JsonResult(new { success = result });
        }
    }
}
