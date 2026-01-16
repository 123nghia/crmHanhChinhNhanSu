using crmHuman.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.Candidate
{
    [Authorize]
    public class ProfileModel : BaseModel2
    {
        private readonly ICandidateBusiness _candidateBusiness;

        public VS.Human.Rep.Model.Candidate Candidate { get; set; } = new VS.Human.Rep.Model.Candidate();

        public ProfileModel(ICandidateBusiness candidateBusiness)
        {
            _candidateBusiness = candidateBusiness;
            TitlePage = "Candidate Profile";
            KeyPage = "CandidateProfile";
        }

        public async Task<IActionResult> OnGet()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (UserData.RoleCode != "CANDIDATE")
            {
                return Redirect("/");
            }

            Candidate = await _candidateBusiness.GetById(UserData.UserId);
            if (Candidate == null || Candidate.Id <= 0)
            {
                return Redirect("/Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdate(CandidateProfileUpdate request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (UserData.RoleCode != "CANDIDATE")
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            request.CandidateId = UserData.UserId;
            var errors = new List<object>();
            ValidationHelper.ValidateFullName(request.Name, "txtFullName", errors);
            ValidationHelper.ValidatePhone(request.Phone, errors);

            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var result = await _candidateBusiness.UpdateProfile(request);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<IActionResult> OnPostChangePassword(string password)
        {
            GetInfoUser();
            if (UserData.RoleCode != "CANDIDATE")
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return ApiResponseHelper.Error("Password cannot be empty");
            }

            var result = await _candidateBusiness.ChangePassword(password, UserData.UserId);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }
    }
}
