using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.FormTemplate
{
    [Authorize]
    public class EditModel : BaseModel2
    {
        private readonly IFormTemplateBusiness _formTemplateBusiness;
        private readonly IWebHostEnvironment _hostingEnvironment;

        [BindProperty]
        public FormTemplateItem Template { get; set; } = new FormTemplateItem { IsActive = 1 };

        [BindProperty]
        public IFormFile? UploadFile { get; set; }

        public bool IsEditMode => Template.Id > 0;

        public EditModel(IFormTemplateBusiness formTemplateBusiness, IWebHostEnvironment hostingEnvironment)
        {
            _formTemplateBusiness = formTemplateBusiness;
            _hostingEnvironment = hostingEnvironment;
            KeyPage = "FormTemplate";
            TitlePage = "Them bieu mau";
        }

        public async Task<IActionResult> OnGet(int? id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanManage())
            {
                return Redirect("/FormTemplate");
            }

            if (id.HasValue && id.Value > 0)
            {
                Template = await _formTemplateBusiness.GetById(id.Value);
                if (Template == null || Template.Id <= 0)
                {
                    return Redirect("/FormTemplate");
                }

                TitlePage = "Chinh sua bieu mau";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanManage())
            {
                return Redirect("/FormTemplate");
            }

            TitlePage = Template.Id > 0 ? "Chinh sua bieu mau" : "Them bieu mau";

            if (string.IsNullOrWhiteSpace(Template.Title))
            {
                ModelState.AddModelError("Template.Title", "Vui long nhap tieu de.");
            }
            else if (Template.Title.Length > 250)
            {
                ModelState.AddModelError("Template.Title", "Tieu de toi da 250 ky tu.");
            }

            var existing = Template.Id > 0 ? await _formTemplateBusiness.GetById(Template.Id) : null;
            if (Template.Id > 0 && (existing == null || existing.Id <= 0))
            {
                return Redirect("/FormTemplate");
            }

            if (UploadFile != null && UploadFile.Length > 0)
            {
                var uploadRoot = Path.Combine(_hostingEnvironment.WebRootPath, "assets", "form-templates");
                if (!Directory.Exists(uploadRoot))
                {
                    Directory.CreateDirectory(uploadRoot);
                }

                var safeName = Path.GetFileName(UploadFile.FileName);
                var extension = Path.GetExtension(safeName);
                var storedName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(uploadRoot, storedName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await UploadFile.CopyToAsync(stream);
                }

                Template.FileName = safeName;
                Template.FilePath = $"/assets/form-templates/{storedName}";
                Template.FileSize = UploadFile.Length;
            }
            else if (existing != null && existing.Id > 0)
            {
                Template.FileName = existing.FileName;
                Template.FilePath = existing.FilePath;
                Template.FileSize = existing.FileSize;
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Template.Title = Template.Title?.Trim();
            Template.IsActive = 1;
            Template.UpdatedBy = UserData.UserId;
            Template.CreatedBy = Template.Id > 0 && existing != null ? existing.CreatedBy : UserData.UserId;

            var result = await _formTemplateBusiness.Save(Template);
            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Khong the luu bieu mau.");
                return Page();
            }

            TempData["SuccessMessage"] = Template.Id > 0 ? "Da cap nhat bieu mau." : "Da tao bieu mau.";
            return Redirect($"/FormTemplate/Detail?Id={Template.Id}");
        }

        private bool CanManage()
        {
            return (Permision.Add ?? false) || (Permision.Edit ?? false) || UserData.RoleCode == "1";
        }
    }
}
