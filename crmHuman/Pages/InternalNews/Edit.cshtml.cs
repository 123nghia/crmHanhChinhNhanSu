using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.InternalNews
{
    [Authorize]
    public class EditModel : BaseModel2
    {
        private readonly IInternalNewsBusiness _newsBusiness;
        private readonly IWebHostEnvironment _hostingEnvironment;

        [BindProperty]
        public InternalNewsAdd News { get; set; } = new InternalNewsAdd();

        [BindProperty]
        public List<IFormFile> AttachmentFiles { get; set; } = new List<IFormFile>();

        public EditModel(IInternalNewsBusiness newsBusiness, IWebHostEnvironment hostingEnvironment)
        {
            _newsBusiness = newsBusiness;
            _hostingEnvironment = hostingEnvironment;
            TitlePage = "Chinh sua tin noi bo";
            KeyPage = "InternalNews";
        }

        public async Task<IActionResult> OnGet(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanEdit())
            {
                return Redirect("/InternalNews");
            }

            if (id <= 0)
            {
                return Redirect("/InternalNews");
            }

            var item = await _newsBusiness.GetById(id);
            if (item == null || item.Id <= 0)
            {
                return Redirect("/InternalNews");
            }

            News = new InternalNewsAdd
            {
                Id = item.Id,
                Title = item.Title,
                Content = item.Content,
                IsSendMail = item.IsSendMail,
                Attachments = item.Attachments ?? new List<InternalNewsAttachment>()
            };

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanEdit())
            {
                return Redirect("/InternalNews");
            }

            if (News.Id <= 0)
            {
                return Redirect("/InternalNews");
            }

            if (string.IsNullOrWhiteSpace(News.Title))
            {
                ModelState.AddModelError("News.Title", "Vui long nhap tieu de.");
            }
            else if (News.Title.Length > 200)
            {
                ModelState.AddModelError("News.Title", "Tieu de toi da 200 ky tu.");
            }

            if (string.IsNullOrWhiteSpace(News.Content))
            {
                ModelState.AddModelError("News.Content", "Vui long nhap noi dung.");
            }

            if (!ModelState.IsValid)
            {
                await LoadExistingAttachments();
                return Page();
            }

            var attachments = new List<InternalNewsAttachment>();
            if (AttachmentFiles.Count > 0)
            {
                var uploadRoot = Path.Combine(_hostingEnvironment.WebRootPath, "assets", "internal-news");
                if (!Directory.Exists(uploadRoot))
                {
                    Directory.CreateDirectory(uploadRoot);
                }

                foreach (var file in AttachmentFiles)
                {
                    if (file == null || file.Length == 0)
                    {
                        continue;
                    }

                    var safeName = Path.GetFileName(file.FileName);
                    var extension = Path.GetExtension(safeName);
                    var storedName = $"{Guid.NewGuid():N}{extension}";
                    var fullPath = Path.Combine(uploadRoot, storedName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    attachments.Add(new InternalNewsAttachment
                    {
                        FileName = safeName,
                        FilePath = $"/assets/internal-news/{storedName}",
                        FileSize = file.Length
                    });
                }
            }

            var item = new InternalNewsItem
            {
                Id = News.Id,
                Title = News.Title?.Trim(),
                Content = News.Content,
                IsSendMail = News.IsSendMail,
                UpdatedBy = UserData.UserId,
                CreatedBy = UserData.UserId,
                Attachments = attachments
            };

            var result = await _newsBusiness.AddOrUpdate(item);
            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Khong the luu bai viet.");
                await LoadExistingAttachments();
                return Page();
            }

            return Redirect($"/InternalNews/Detail?Id={News.Id}");
        }

        private bool CanEdit()
        {
            return (Permision.Edit ?? false) || UserData.RoleCode == "1";
        }

        private async Task LoadExistingAttachments()
        {
            var existing = await _newsBusiness.GetById(News.Id);
            News.Attachments = existing?.Attachments ?? new List<InternalNewsAttachment>();
        }
    }
}
