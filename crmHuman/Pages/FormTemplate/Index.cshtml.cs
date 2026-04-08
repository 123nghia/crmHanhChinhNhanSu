using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Item;

namespace crmHuman.Pages.FormTemplate
{
    [Authorize]
    public class IndexModel : BaseModel2
    {
        private readonly IFormTemplateBusiness _formTemplateBusiness;

        public FormTemplateRequest RequestSearch { get; set; } = new FormTemplateRequest();
        public BaseList TemplateList { get; set; } = new BaseList();

        public int TotalRecord => TemplateList?.Total ?? 0;

        public bool CanCreate => (Permision.Add ?? false) || UserData.RoleCode == "1";
        public bool CanEdit => (Permision.Edit ?? false) || UserData.RoleCode == "1";
        public bool CanDelete => (Permision.Delete ?? false) || UserData.RoleCode == "1";

        public IndexModel(IFormTemplateBusiness formTemplateBusiness)
        {
            _formTemplateBusiness = formTemplateBusiness;
            TitlePage = "Bieu mau";
            KeyPage = "FormTemplate";
        }

        public async Task<IActionResult> OnGet([FromQuery] FormTemplateRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            RequestSearch = request ?? new FormTemplateRequest();
            RequestSearch.Token ??= string.Empty;

            if (!(Permision.View ?? false) && UserData.RoleCode != "1")
            {
                TemplateList = new BaseList { Data = new List<object>(), Total = 0 };
                return Page();
            }

            TemplateList = await _formTemplateBusiness.GetAll(RequestSearch);
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
                return Redirect("/FormTemplate");
            }

            if (id > 0)
            {
                await _formTemplateBusiness.Delete(id);
                TempData["SuccessMessage"] = "Da xoa bieu mau.";
            }

            return Redirect("/FormTemplate");
        }
    }
}
