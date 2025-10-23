using crmHuman.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using VS.Human.Business;

namespace crmHuman.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;

        private ILoginBussiness _business;
        private IEmpBusiness _empBusiness;

        public LoginModel(ILogger<LoginModel> logger, ILoginBussiness loginBussiness,
            IEmpBusiness empBusiness
            )
        {
            _logger = logger;
            _business = loginBussiness;
            _empBusiness = empBusiness;

        }
        private async Task<IActionResult> Login(string userName, string pass)
        {
            var userProfile = await _empBusiness.Login(userName, pass);
            if (userProfile == null || userProfile.Id < 1)
            {
                var listError = new
                {
                    name = "UserName",
                    Content = "Tên đăng nhập hoặc mật khẩu không chính xác"
                };
                ModelState.AddModelError("UserName", "Tên đăng nhập hoặc mật khẩu không chính xác");
                return Page();

            }
            var lineCode = userProfile.LineCode;
            if (string.IsNullOrEmpty(lineCode))
            {
                lineCode = "9999";
            }
            var account = new
            {
                id = userProfile.Id,
                userProfile.UserName,
                userProfile.FullName,
                lineCode,
                userProfile.Email,
                userProfile.RoleCode
            };
            List<Claim> claims = new List<Claim>
            {
                new Claim("userId", account.id.ToString()),
                new Claim("UserName", account.UserName),
                new Claim("LineCode", account.lineCode)
            };
            if (!string.IsNullOrWhiteSpace(account.FullName))
                claims.Add(new Claim("FullName", account.FullName));
            if (!string.IsNullOrWhiteSpace(account.RoleCode))
                claims.Add(new Claim("RoleCode", account.RoleCode));

            var userIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal = new ClaimsPrincipal(userIdentity);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false
            };
            await HttpContext.SignInAsync(principal);
            return Redirect("/");

        }

        public async Task<string> GetPageName()
        {
            return await _business.Print();

        }

        public void OnGet()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {

                HttpContext.Response.Redirect("/");
            }


        }
        public async Task<IActionResult> OnPostLogin(LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.UserName))
            {
                var listError = new
                {
                    name = "UserName",
                    Content = "Thiếu thông tin tên đăng nhập"
                };
                return StatusCode(StatusCodes.Status400BadRequest, listError);
            }

            var dataReponse = new
            {
                success = true,

            };
            return await Login(request.UserName, request.Password);


        }
    }

}
