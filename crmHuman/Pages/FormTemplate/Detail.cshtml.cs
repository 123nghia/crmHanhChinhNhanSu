using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.FormTemplate
{
    [Authorize]
    public class DetailModel : BaseModel2
    {
        private readonly IFormTemplateBusiness _formTemplateBusiness;

        public FormTemplateItem TemplateItem { get; set; } = new FormTemplateItem();

        public bool CanEdit => (Permision.Edit ?? false) || UserData.RoleCode == "1";
        public bool CanDelete => (Permision.Delete ?? false) || UserData.RoleCode == "1";

        public DetailModel(IFormTemplateBusiness formTemplateBusiness)
        {
            _formTemplateBusiness = formTemplateBusiness;
            TitlePage = "Chi tiet bieu mau";
            KeyPage = "FormTemplate";
        }

        public async Task<IActionResult> OnGet(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!(Permision.View ?? false) && UserData.RoleCode != "1")
            {
                return Redirect("/FormTemplate");
            }

            if (id <= 0)
            {
                return Redirect("/FormTemplate");
            }

            TemplateItem = await _formTemplateBusiness.GetById(id);
            if (TemplateItem == null || TemplateItem.Id <= 0)
            {
                return Redirect("/FormTemplate");
            }

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
