using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Item;

namespace crmHuman.Pages.System.MailGroup
{
    [Authorize]
    public class IndexModel : BaseModel2
    {
        private readonly IMailGroupBusiness _mailGroupBusiness;

        public MailGroupRequest RequestSearch { get; set; } = new MailGroupRequest();
        public BaseList GroupList { get; set; } = new BaseList();

        public int TotalRecord => GroupList?.Total ?? 0;

        public bool CanCreate => (Permision.Add ?? false) || UserData.RoleCode == "1";
        public bool CanEdit => (Permision.Edit ?? false) || UserData.RoleCode == "1";
        public bool CanDelete => (Permision.Delete ?? false) || UserData.RoleCode == "1";

        public IndexModel(IMailGroupBusiness mailGroupBusiness)
        {
            _mailGroupBusiness = mailGroupBusiness;
            TitlePage = "Nhom mail";
            KeyPage = "MailGroup";
        }

        public async Task<IActionResult> OnGet([FromQuery] MailGroupRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            RequestSearch = request ?? new MailGroupRequest();
            RequestSearch.Token ??= string.Empty;

            if (!(Permision.View ?? false) && UserData.RoleCode != "1")
            {
                GroupList = new BaseList { Data = new List<object>(), Total = 0 };
                return Page();
            }

            GroupList = await _mailGroupBusiness.GetAll(RequestSearch);
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
                return Redirect("/System/MailGroup");
            }

            if (id > 0)
            {
                await _mailGroupBusiness.Delete(id);
                TempData["SuccessMessage"] = "Da xoa nhom mail.";
            }

            return Redirect("/System/MailGroup");
        }
    }
}
