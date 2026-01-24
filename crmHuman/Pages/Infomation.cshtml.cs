using crmHuman.Model;
using crmHuman.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class InfomationModel : BaseModel2
    {
        private readonly ILogger<InfomationModel> _logger;
        private readonly IEmpBusiness _empBusiness;
        private readonly IThemeSettingBusiness _themeSettingBusiness;
        public Employee UserProfile;
        public EmployeeRequest RequestSearch { get; set; }

        [BindProperty]
        public ThemeSettingRequest ThemeForm { get; set; } = new ThemeSettingRequest();




        public InfomationModel(ILogger<InfomationModel> logger,
            IEmpBusiness empBusiness,
            IThemeSettingBusiness themeSettingBusiness
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            _themeSettingBusiness = themeSettingBusiness;
            TitlePage = "Thông tin tài khoản";
            KeyPage = "Infomation";

            TableColumnText = new List<string>()
            {
                "STT","Họ tên","Tài khoản","Vai trò","Trạng thái","Ngày tạo","Cập nhật gần nhất","Thao tác"
            };
            UserProfile = new Employee()
            {

            };


        }

        public async Task<IActionResult> OnPostSaveTheme()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (UserData == null || UserData.UserId < 1)
            {
                return Redirect("/Login");
            }

            var primary = ThemeStyleBuilder.NormalizeOrDefault(ThemeForm?.PrimaryColor, ThemeStyleBuilder.DefaultPrimary);
            var button = ThemeStyleBuilder.NormalizeOrDefault(ThemeForm?.ButtonColor, primary);
            var background = ThemeStyleBuilder.NormalizeOrDefault(ThemeForm?.BackgroundColor, ThemeStyleBuilder.DefaultBackground);

            var setting = new UserThemeSetting
            {
                UserId = UserData.UserId,
                PrimaryColor = primary,
                ButtonColor = button,
                BackgroundColor = background,
                CreatedBy = UserData.UserId,
                UpdatedBy = UserData.UserId
            };

            var ok = await _themeSettingBusiness.SaveAsync(setting);
            TempData["ThemeSaved"] = ok ? "1" : "0";
            return RedirectToPage();
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

            Employee? result = null;
            if (request.Id < 0)
            {
                result = await _empBusiness.Add(request);
            }
            else
            {
                var ok = await _empBusiness.Update(request);
                if (ok)
                {
                    result = await _empBusiness.CheckDuplicate(request.Email ?? string.Empty, request.Phone ?? string.Empty);
                }
            }
            var dataReponse = new
            {
                success = result != null && result.Id > 0,

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
            //if (UserData.RoleCode == "2")
            //{
            //    return Redirect("/");
            //}
            UserProfile = await _empBusiness.GetById(UserData.UserId);

            var themeSetting = await _themeSettingBusiness.GetByUserIdAsync(UserData.UserId);
            var primary = ThemeStyleBuilder.NormalizeOrDefault(themeSetting?.PrimaryColor, ThemeStyleBuilder.DefaultPrimary);
            ThemeForm.PrimaryColor = primary;
            ThemeForm.ButtonColor = ThemeStyleBuilder.NormalizeOrDefault(themeSetting?.ButtonColor, primary);
            ThemeForm.BackgroundColor = ThemeStyleBuilder.NormalizeOrDefault(themeSetting?.BackgroundColor, ThemeStyleBuilder.DefaultBackground);

            return Page();
        }






    }
}
