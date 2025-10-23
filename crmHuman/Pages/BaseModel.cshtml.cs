using crmHuman.Model;
using Microsoft.AspNetCore.Authorization;

namespace crmHuman.Pages
{


    [Authorize]
    public class BaseModel : BaseModel2
    {


        public UserDataView UserData
        {
            get;


            set;
        }

        public BaseModel()
        {
            UserData = new UserDataView()
            {
                UserId = -1
            };

        }
        public void OnGet()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                Redirect("/Login");
                return;
                // HttpContext.Response.Redirect("/Login");
            }
            GetInfoUser();



        }


    }
}
