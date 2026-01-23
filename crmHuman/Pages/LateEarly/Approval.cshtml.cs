using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.LateEarly
{
    public class ApprovalModel : BaseModel2
    {
        private readonly ILateEarlyBusiness _lateEarlyBusiness;

        public ApprovalModel(ILateEarlyBusiness lateEarlyBusiness)
        {
            _lateEarlyBusiness = lateEarlyBusiness;
            KeyPage = "LateEarlyApproval";
            TitlePage = "Duyệt đi trễ / về sớm";
        }

        public BaseList RequestList { get; set; } = new BaseList();

        public async Task OnGetAsync(int page = 1, int limit = 20, int? status = null)
        {
            GetInfoUser();
            if (!(Permision.Approve ?? false))
            {
                RequestList = new BaseList();
                return;
            }

            int? filterStatus = status;
            if (filterStatus == null && UserData.RoleCode != "1")
            {
                if (UserData.RoleCode == "3") filterStatus = 0;
                else if (UserData.RoleCode == "2" || UserData.RoleCode == "9") filterStatus = 1;
                else if (UserData.RoleCode == "8") filterStatus = 2;
                else filterStatus = 3;
            }

            RequestList = await _lateEarlyBusiness.GetList(null, filterStatus, null, null, page, limit);
        }

        public async Task<IActionResult> OnGetHistoryAsync(int id)
        {
            var result = await _lateEarlyBusiness.GetHistory(id);
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostApproveAsync([FromBody] LateEarlyApproveRequest model)
        {
            GetInfoUser();
            if (!(Permision.Approve ?? false))
            {
                return new JsonResult(new { success = false, message = "No permission" });
            }

            var result = await _lateEarlyBusiness.ApproveWorkflow(model.Id, model.Action, UserData.UserId, UserData.RoleCode, model.Comment);
            return new JsonResult(new { success = result });
        }
    }
}
