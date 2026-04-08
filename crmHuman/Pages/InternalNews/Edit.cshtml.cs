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
        private readonly IMailGroupBusiness _mailGroupBusiness;
        private readonly IWebHostEnvironment _hostingEnvironment;

        [BindProperty]
        public InternalNewsAdd News { get; set; } = new InternalNewsAdd();

        [BindProperty]
        public List<IFormFile> AttachmentFiles { get; set; } = new List<IFormFile>();

        public List<MailGroupItem> AvailableMailGroups { get; set; } = new List<MailGroupItem>();

        public EditModel(IInternalNewsBusiness newsBusiness, IMailGroupBusiness mailGroupBusiness, IWebHostEnvironment hostingEnvironment)
        {
            _newsBusiness = newsBusiness;
            _mailGroupBusiness = mailGroupBusiness;
            _hostingEnvironment = hostingEnvironment;
            TitlePage = "Chá»‰nh sá»­a tin ná»™i bá»™";
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
                DirectRecipientEmails = item.DirectRecipientEmails,
                MailGroupIds = item.MailGroupIds ?? new List<int>(),
                Attachments = item.Attachments ?? new List<InternalNewsAttachment>()
            };

            await LoadMailGroupsAsync();
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
                ModelState.AddModelError("News.Title", "Vui lÃ²ng nháº­p tiÃªu Ä‘á».");
            }
            else if (News.Title.Length > 200)
            {
                ModelState.AddModelError("News.Title", "TiÃªu Ä‘á» tá»‘i Ä‘a 200 kÃ½ tá»±.");
            }

            if (string.IsNullOrWhiteSpace(News.Content))
            {
                ModelState.AddModelError("News.Content", "Vui lÃ²ng nháº­p ná»™i dung.");
            }

            if (!ModelState.IsValid)
            {
                await LoadExistingAttachments();
                await LoadMailGroupsAsync();
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
                DirectRecipientEmails = News.DirectRecipientEmails,
                MailGroupIds = News.MailGroupIds ?? new List<int>(),
                UpdatedBy = UserData.UserId,
                CreatedBy = UserData.UserId,
                Attachments = attachments
            };

            var result = await _newsBusiness.AddOrUpdate(item);
            if (!result)
            {
                ModelState.AddModelError(string.Empty, "KhÃ´ng thá»ƒ lÆ°u bÃ i viáº¿t.");
                await LoadExistingAttachments();
                await LoadMailGroupsAsync();
                return Page();
            }

            await SendNotificationIfNeededAsync(item.Id);
            return Redirect($"/InternalNews/Detail?Id={item.Id}");
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

        private async Task LoadMailGroupsAsync()
        {
            AvailableMailGroups = await _mailGroupBusiness.GetSelectableGroupsAsync();
        }

        private async Task SendNotificationIfNeededAsync(int newsId)
        {
            if (!News.IsSendMail || newsId <= 0)
            {
                return;
            }

            var sendResult = await _newsBusiness.SendNotificationAsync(newsId, BuildDetailUrl(newsId), UserData.UserId);
            if (sendResult.Success)
            {
                TempData["SuccessMessage"] = $"ÄÃ£ gá»­i mail tá»›i {sendResult.RecipientCount} ngÆ°á»i nháº­n.";
                return;
            }

            TempData["WarningMessage"] = sendResult.Error ?? "BÃ i Ä‘Äƒng Ä‘Ã£ lÆ°u nhÆ°ng khÃ´ng thá»ƒ gá»­i mail.";
        }

        private string BuildDetailUrl(int newsId)
        {
            var absoluteUrl = Url.Page("/InternalNews/Detail", null, new { id = newsId }, Request.Scheme);
            if (!string.IsNullOrWhiteSpace(absoluteUrl))
            {
                return absoluteUrl;
            }

            return $"{Request.Scheme}://{Request.Host}/InternalNews/Detail?Id={newsId}";
        }
    }
}
