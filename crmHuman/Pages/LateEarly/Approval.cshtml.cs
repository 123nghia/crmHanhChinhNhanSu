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

        private static bool IsApprovalRole(string? roleCode)
        {
            return roleCode == "1"
                || roleCode == "2"
                || roleCode == "3"
                || roleCode == "8"
                || roleCode == "9"
                || roleCode == "TL"
                || roleCode == "HCNS"
                || roleCode == "BGD"
                || roleCode == "ADMIN";
        }

        private static bool IsAdminOrBgdRole(string? roleCode)
        {
            return roleCode == "1"
                || roleCode == "8"
                || roleCode == "BGD"
                || roleCode == "ADMIN";
        }

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
            if (!(Permision.Approve ?? false) || !IsApprovalRole(UserData.RoleCode))
            {
                RequestList = new BaseList();
                return;
            }

            int? filterStatus = status;
            if (filterStatus == null)
            {
                if (UserData.RoleCode == "3" || UserData.RoleCode == "TL") filterStatus = 0;
                else if (UserData.RoleCode == "2" || UserData.RoleCode == "9" || UserData.RoleCode == "HCNS") filterStatus = 1;
                else if (UserData.RoleCode == "8" || UserData.RoleCode == "BGD") filterStatus = 2;
                else filterStatus = 3;
            }

            RequestList = await _lateEarlyBusiness.GetList(null, filterStatus, null, null, page, limit, UserData.UserId, UserData.RoleCode);
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
                return new JsonResult(new { success = false, message = "Bạn không có quyền phê duyệt." });
            }

            var request = await _lateEarlyBusiness.GetById(model.Id);
            if (request == null || request.Id <= 0)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy yêu cầu." });
            }

            if (IsAdminOrBgdRole(UserData.RoleCode) && request.EmployeeId == UserData.UserId)
            {
                return new JsonResult(new { success = false, message = "Admin/BGĐ không được tự xử lý đơn của chính mình." });
            }

            var result = await _lateEarlyBusiness.ApproveWorkflow(model.Id, model.Action, UserData.UserId, UserData.RoleCode, model.Comment);
            return new JsonResult(new { success = result, message = result ? string.Empty : "Không thể xử lý yêu cầu này." });
        }
    }
}
