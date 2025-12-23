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

        public async Task OnGetAsync(int page = 1, int limit = 20, int? status = 0)
        {
            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                LeaveList = new BaseList { Data = new List<object>(), Total = 0 };
            }
            else
            {
                // Managers can see all requests they are supposed to approve
                // For simplicity, showing all pending requests if status is 0
                LeaveList = await _leaveBusiness.GetLeaveList(null, status, null, null, page, limit);
            }
        }

        public async Task<IActionResult> OnPostApproveAsync([FromBody] LeaveApproveRequest model)
        {
            GetInfoUser();
            if (!(Permision.Approve ?? false)) return new JsonResult(new { success = false, message = "No permission to approve" });

            var result = await _leaveBusiness.ApproveLeave(model.Id, model.Status, UserData.UserId, model.Comment);
            return new JsonResult(new { success = result });
        }
    }
}
