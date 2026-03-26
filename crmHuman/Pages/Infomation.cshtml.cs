using crmHuman.Model;
using crmHuman.Services;
using IOFile = System.IO.File;
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
    public class InfomationModel : BaseModel2
    {
        private readonly ILogger<InfomationModel> _logger;
        private readonly IEmpBusiness _empBusiness;
        private readonly IThemeSettingBusiness _themeSettingBusiness;
        private readonly IWebHostEnvironment _env;
        public Employee UserProfile;
        public EmployeeRequest RequestSearch { get; set; }

        [BindProperty]
        public ThemeSettingRequest ThemeForm { get; set; } = new ThemeSettingRequest();

        [BindProperty]
        public IFormFile? AvatarFileUpload { get; set; }




        public InfomationModel(ILogger<InfomationModel> logger,
            IEmpBusiness empBusiness,
            IThemeSettingBusiness themeSettingBusiness,
            IWebHostEnvironment env
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            _themeSettingBusiness = themeSettingBusiness;
            _env = env;
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

        public async Task<IActionResult> OnPostUpdateAvatar()
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

            if (AvatarFileUpload == null || AvatarFileUpload.Length == 0)
            {
                TempData["AvatarError"] = "missing";
                return RedirectToPage();
            }

            if (AvatarFileUpload.Length > 2 * 1024 * 1024)
            {
                TempData["AvatarError"] = "size";
                return RedirectToPage();
            }

            var ext = Path.GetExtension(AvatarFileUpload.FileName).ToLowerInvariant();
            var allowed = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
            if (!allowed.Contains(ext))
            {
                TempData["AvatarError"] = "type";
                return RedirectToPage();
            }

            var currentEmployee = await _empBusiness.GetById(UserData.UserId);
            var folder = Path.Combine(_env.WebRootPath, "uploads", "avatars", UserData.UserId.ToString());
            Directory.CreateDirectory(folder);

            var fileName = $"avatar_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(folder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await AvatarFileUpload.CopyToAsync(stream);
            }

            var avatarPath = $"/uploads/avatars/{UserData.UserId}/{fileName}";
            var ok = await _empBusiness.UpdateAvatar(UserData.UserId, avatarPath, UserData.UserId);

            if (ok && !string.IsNullOrWhiteSpace(currentEmployee?.AvatarFile)
                && currentEmployee.AvatarFile.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var oldRelativePath = currentEmployee.AvatarFile.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var oldPhysicalPath = Path.Combine(_env.WebRootPath, oldRelativePath);
                    if (!string.Equals(oldPhysicalPath, filePath, StringComparison.OrdinalIgnoreCase) && IOFile.Exists(oldPhysicalPath))
                    {
                        IOFile.Delete(oldPhysicalPath);
                    }
                }
                catch
                {
                }
            }

            TempData["AvatarSaved"] = ok ? "1" : "0";
            return RedirectToPage();
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

        public async Task<IActionResult> OnPostSaveMailSignature([FromForm] string? mailSignature)
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

            var ok = await _empBusiness.UpdateMailSignature(UserData.UserId, mailSignature, UserData.UserId);
            TempData["MailSignatureSaved"] = ok ? "1" : "0";
            return RedirectToPage(new { tab = "mail-signature" });
        }

        public async Task<IActionResult> OnPostUploadMailSignatureImageAsync(IFormFile? file)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            GetInfoUser();
            if (UserData == null || UserData.UserId < 1)
            {
                return Unauthorized();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { error = "Image size must be 5MB or smaller" });
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".gif",
                ".webp",
                ".bmp"
            };

            if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
            {
                return BadRequest(new { error = "Unsupported file type" });
            }

            var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", "mail-signatures", UserData.UserId.ToString());
            Directory.CreateDirectory(uploadRoot);

            var storedName = $"signature_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadRoot, storedName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var location = $"/uploads/mail-signatures/{UserData.UserId}/{storedName}";
            return new JsonResult(new { location });
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
