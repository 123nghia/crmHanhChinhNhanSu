using Microsoft.AspNetCore.Mvc;

namespace crmHuman.Pages.Attendance
{
    public class AbsentModel : BaseModel2
    {
        public AbsentModel()
        {
            KeyPage = "Attendance";
            TitlePage = "Nghi phep / ky hieu cong / ngay le";
        }

        public IActionResult OnGet()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            return Redirect("/Attendance/Schedule");
        }
    }
}
