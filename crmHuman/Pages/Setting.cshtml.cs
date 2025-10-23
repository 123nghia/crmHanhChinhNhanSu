using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class SettingModel : BaseModel2
    {
        private readonly ILogger<SettingModel> _logger;
        private readonly IEmpBusiness _empBusiness;
        public Employee UserProfile;
        public EmployeeRequest RequestSearch { get; set; }





        public SettingModel(ILogger<SettingModel> logger,
            IEmpBusiness empBusiness
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            TitlePage = "Thông tin nhân viên";
            KeyPage = "Infomation";

            TableColumnText = new List<string>()
            {
                "STT","Họ tên","Tài khoản","Vai trò","Trạng thái","Ngày tạo","Cập nhật gần nhất","Thao tác"
            };
            UserProfile = new Employee()
            {

            };


        }

        public async Task<IActionResult> OnPostAddEmployeee
            (EmployeeInfoAdd request)
        {
            var listEror = new List<object>();

            if (string.IsNullOrEmpty(request.FullName))
            {
                var itemError = new
                {
                    name = "txtFullName",
                    Content = "Thiếu thông tin họ và tên"
                };
                listEror.Add(itemError);

            }
            if (string.IsNullOrEmpty(request.Pass) && request.Id < 0)
            {
                var itemError = new
                {
                    name = "txtPass",
                    Content = "Thiếu thông tin mật khẩu"
                };
                listEror.Add(itemError);

            }
            if (string.IsNullOrEmpty(request.Phone))
            {
                var itemError = new
                {
                    name = "txtPhone",
                    Content = "Thiếu thông tin số điện thoại"
                };
                listEror.Add(itemError);

            }
            if (string.IsNullOrEmpty(request.RoleCode))
            {
                var itemError = new
                {
                    name = "txtRoleCode",
                    Content = "Thiếu thông tin vai trò"
                };
                listEror.Add(itemError);

            }
            if (listEror.Count > 0)
            {
                return new JsonResult(listEror)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var result = true;
            if (request.Id < 0)
            {
                result = await _empBusiness.Add(request);
            }
            else
            {
                result = await _empBusiness.Update(request);
            }
            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }

        public async Task<ActionResult> OnGet([FromQuery] EmployeeRequest request)
        {


            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");


            }
            GetInfoUser();
            if (UserData.RoleCode == "2")
            {
                return Redirect("/");
            }
            UserProfile = await _empBusiness.GetById(UserData.UserId);

            return Page();
        }






    }
}
