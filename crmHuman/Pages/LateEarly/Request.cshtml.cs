using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.LateEarly
{
    public class RequestModel : BaseModel2
    {
        private readonly ILateEarlyBusiness _lateEarlyBusiness;

        public RequestModel(ILateEarlyBusiness lateEarlyBusiness)
        {
            _lateEarlyBusiness = lateEarlyBusiness;
            KeyPage = "LateEarlyRequest";
            TitlePage = "Xin phép đi trễ / về sớm";
        }

        public BaseList RequestList { get; set; } = new BaseList();

        public async Task OnGetAsync(int page = 1, int limit = 20)
        {
            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                RequestList = new BaseList();
                return;
            }

            var scope = ResolveScope();
            RequestList = await _lateEarlyBusiness.GetList(scope.employeeId, null, null, null, page, limit);
        }

        private (int? employeeId, int? userId) ResolveScope()
        {
            var roleCode = UserData?.RoleCode;
            if (roleCode == "1" || roleCode == "8" || roleCode == "9" || roleCode == "3")
            {
                return (null, UserData?.UserId);
            }

            return (UserData?.UserId, UserData?.UserId);
        }

        public async Task<IActionResult> OnGetByIdAsync(int id)
        {
            var result = await _lateEarlyBusiness.GetById(id);
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnGetHistoryAsync(int id)
        {
            var result = await _lateEarlyBusiness.GetHistory(id);
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostSaveAsync([FromBody] LateEarlyAddUpdate model)
        {
            GetInfoUser();
            if (model == null)
            {
                return new JsonResult(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            if (model.Id == 0 && !(Permision.Add ?? false))
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền tạo yêu cầu." });
            }

            if (model.Id > 0 && !(Permision.Edit ?? false))
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền chỉnh sửa yêu cầu." });
            }

            if (model.EmployeeId == 0)
            {
                model.EmployeeId = UserData.UserId;
            }

            var result = await _lateEarlyBusiness.CreateOrUpdate(model, UserData.UserId);
            if (result > 0)
            {
                return new JsonResult(new { success = true, id = result });
            }

            var message = "Có lỗi xảy ra.";
            if (result == -1)
            {
                message = "Giờ kết thúc phải lớn hơn giờ bắt đầu.";
            }
            else if (result == -2)
            {
                message = "Ngày xin phép phải trùng với ngày của giờ bắt đầu và giờ kết thúc.";
            }
            else if (result == -3)
            {
                message = "Nhân viên không hợp lệ.";
            }

            return new JsonResult(new { success = false, message });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            GetInfoUser();
            if (!(Permision.Delete ?? false))
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền xóa yêu cầu." });
            }

            var result = await _lateEarlyBusiness.Delete(id, UserData.UserId);
            return new JsonResult(new { success = result });
        }
    }
}
