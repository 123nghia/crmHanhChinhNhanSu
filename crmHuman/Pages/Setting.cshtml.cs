using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
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
        private readonly IBrandingBusiness _brandingBusiness;
        private readonly IWebHostEnvironment _env;
        public Employee UserProfile;
        public EmployeeRequest RequestSearch { get; set; }

        [BindProperty]
        public IFormFile? LogoFile { get; set; }




        public SettingModel(ILogger<SettingModel> logger,
            IEmpBusiness empBusiness,
            IBrandingBusiness brandingBusiness,
            IWebHostEnvironment env
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            _brandingBusiness = brandingBusiness;
            _env = env;
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

        public async Task<IActionResult> OnPostUpdateLogo()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (UserData == null || UserData.RoleCode != "1")
            {
                return Redirect("/");
            }

            if (LogoFile == null || LogoFile.Length == 0)
            {
                TempData["LogoError"] = "missing";
                return RedirectToPage();
            }

            if (LogoFile.Length > 2 * 1024 * 1024)
            {
                TempData["LogoError"] = "size";
                return RedirectToPage();
            }

            var ext = Path.GetExtension(LogoFile.FileName).ToLowerInvariant();
            var allowed = new[] { ".png", ".jpg", ".jpeg" };
            if (!allowed.Contains(ext))
            {
                TempData["LogoError"] = "type";
                return RedirectToPage();
            }

            var folder = Path.Combine(_env.WebRootPath, "assets", "img", "branding");
            Directory.CreateDirectory(folder);

            var fileName = $"logo_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(folder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await LogoFile.CopyToAsync(stream);
            }

            var logoPath = $"/assets/img/branding/{fileName}";
            var ok = await _brandingBusiness.SaveLogoAsync(logoPath, UserData.UserId);
            TempData["LogoSaved"] = ok ? "1" : "0";

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
            if (UserData.RoleCode != "1")
            {
                return Redirect("/");
            }
            UserProfile = await _empBusiness.GetById(UserData.UserId);

            return Page();
        }






    }
}
