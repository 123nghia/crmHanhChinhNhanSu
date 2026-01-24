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
        private ICandidateBusiness _candidateBusiness;
        private readonly ILogHistoryBusiness _logHistoryBusiness;
        private readonly IBrandingBusiness _brandingBusiness;

        public string LogoPath { get; set; } = "/assets/img/logo.png";

        public LoginModel(ILogger<LoginModel> logger, ILoginBussiness loginBussiness,
            IEmpBusiness empBusiness,
            ICandidateBusiness candidateBusiness,
            ILogHistoryBusiness logHistoryBusiness,
            IBrandingBusiness brandingBusiness
            )
        {
            _logger = logger;
            _business = loginBussiness;
            _empBusiness = empBusiness;
            _candidateBusiness = candidateBusiness;
            _logHistoryBusiness = logHistoryBusiness;
            _brandingBusiness = brandingBusiness;

        }

        private async Task LoadBrandingAsync()
        {
            try
            {
                var branding = await _brandingBusiness.GetActiveAsync();
                if (!string.IsNullOrWhiteSpace(branding?.LogoPath))
                {
                    LogoPath = branding.LogoPath;
                }
            }
            catch
            {
            }
        }

        private async Task<IActionResult> Login(string userName, string pass)
        {
            await LoadBrandingAsync();
            var userProfile = await _empBusiness.Login(userName, pass);
            if (userProfile != null && userProfile.Id > 0)
            {
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
                UserActive.DataActiveOnline.MarkLogin(account.id.ToString(), account.UserName, account.FullName);
                await _logHistoryBusiness.LogLogin(account.id, account.UserName, account.FullName, account.RoleCode, "Employee");
                await HttpContext.SignInAsync(principal);
                return Redirect("/");
            }

            var candidateProfile = await _candidateBusiness.Login(userName, pass);
            if (candidateProfile == null || candidateProfile.Id < 1)
            {
                var listError = new
                {
                    name = "UserName",
                    Content = "T?n dang nh?p ho?c m?t kh?u kh?ng ch?nh x?c"
                };
                ModelState.AddModelError("UserName", "T?n dang nh?p ho?c m?t kh?u kh?ng ch?nh x?c");
                return Page();
            }

            var candidateLineCode = string.IsNullOrWhiteSpace(candidateProfile.Code) ? "CANDIDATE" : candidateProfile.Code;
            var candidateAccount = new
            {
                id = candidateProfile.Id,
                UserName = candidateProfile.UserName,
                FullName = candidateProfile.Name,
                lineCode = candidateLineCode,
                RoleCode = "CANDIDATE"
            };
            List<Claim> candidateClaims = new List<Claim>
            {
                new Claim("userId", candidateAccount.id.ToString()),
                new Claim("UserName", candidateAccount.UserName ?? string.Empty),
                new Claim("LineCode", candidateAccount.lineCode),
                new Claim("RoleCode", candidateAccount.RoleCode)
            };
            if (!string.IsNullOrWhiteSpace(candidateAccount.FullName))
                candidateClaims.Add(new Claim("FullName", candidateAccount.FullName));

            var candidateIdentity = new ClaimsIdentity(candidateClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal candidatePrincipal = new ClaimsPrincipal(candidateIdentity);
            UserActive.DataActiveOnline.MarkLogin(candidateAccount.id.ToString(), candidateAccount.UserName ?? string.Empty, candidateAccount.FullName ?? string.Empty);
            await _logHistoryBusiness.LogLogin(candidateAccount.id, candidateAccount.UserName, candidateAccount.FullName, candidateAccount.RoleCode, "Candidate");
            await HttpContext.SignInAsync(candidatePrincipal);
            return Redirect("/Candidate/Dashboard");

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
                return;
            }

            LoadBrandingAsync().GetAwaiter().GetResult();
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
