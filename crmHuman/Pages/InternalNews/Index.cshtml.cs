using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Item;

namespace crmHuman.Pages.InternalNews
{
    [Authorize]
    public class IndexModel : BaseModel2
    {
        private readonly IInternalNewsBusiness _newsBusiness;

        public InternalNewsRequest RequestSearch { get; set; }
        public BaseList NewsList { get; set; }

        public int TotalRecord => NewsList?.Total ?? 0;

        public bool CanCreate => (Permision.Add ?? false) || (UserData?.RoleCode == "1");
        public bool CanEdit => (Permision.Edit ?? false) || (UserData?.RoleCode == "1");
        public bool CanDelete => (Permision.Delete ?? false) || (UserData?.RoleCode == "1");

        public IndexModel(IInternalNewsBusiness newsBusiness)
        {
            _newsBusiness = newsBusiness;
            TitlePage = "Tin nội bộ";
            KeyPage = "InternalNews";
            NewsList = new BaseList();
            RequestSearch = new InternalNewsRequest();
        }

        public async Task<IActionResult> OnGet([FromQuery] InternalNewsRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            RequestSearch = request;

            var canView = (Permision.View ?? false) || (UserData?.RoleCode == "1");
            if (!canView)
            {
                NewsList = new BaseList { Data = new List<object>(), Total = 0 };
                return Page();
            }

            NewsList = await _newsBusiness.GetAll(request);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanDelete)
            {
                return Redirect("/InternalNews");
            }

            if (id > 0)
            {
                await _newsBusiness.Delete(id);
            }

            return Redirect("/InternalNews");
        }
    }
}
