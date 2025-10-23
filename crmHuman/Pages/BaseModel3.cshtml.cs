using crmHuman.Model;
using Microsoft.AspNetCore.Authorization;

namespace crmHuman.Pages
{


    [Authorize]
    public class BaseModel3 : BaseModel2
    {


        public UserDataView UserData
        {
            get;


            set;
        }

        public BaseModel3()
        {
            UserData = new UserDataView()
            {
                UserId = -1
            };

        }



    }
}
