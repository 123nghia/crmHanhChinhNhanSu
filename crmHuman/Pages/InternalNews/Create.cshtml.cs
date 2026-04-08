using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.InternalNews
{
    [Authorize]
    public class CreateModel : BaseModel2
    {
        private readonly IInternalNewsBusiness _newsBusiness;
        private readonly IMailGroupBusiness _mailGroupBusiness;
        private readonly IWebHostEnvironment _hostingEnvironment;

        [BindProperty]
        public InternalNewsAdd News { get; set; } = new InternalNewsAdd();

        [BindProperty]
        public List<IFormFile> AttachmentFiles { get; set; } = new List<IFormFile>();

        public List<MailGroupItem> AvailableMailGroups { get; set; } = new List<MailGroupItem>();

        public CreateModel(IInternalNewsBusiness newsBusiness, IMailGroupBusiness mailGroupBusiness, IWebHostEnvironment hostingEnvironment)
        {
            _newsBusiness = newsBusiness;
            _mailGroupBusiness = mailGroupBusiness;
            _hostingEnvironment = hostingEnvironment;
            TitlePage = "Táº¡o tin ná»™i bá»™";
            KeyPage = "InternalNews";
        }

        public async Task<IActionResult> OnGet()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanCreate())
            {
                return Redirect("/InternalNews");
            }

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
            if (!CanCreate())
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
                Title = News.Title?.Trim(),
                Content = News.Content,
                IsSendMail = News.IsSendMail,
                DirectRecipientEmails = News.DirectRecipientEmails,
                MailGroupIds = News.MailGroupIds ?? new List<int>(),
                CreatedBy = UserData.UserId,
                UpdatedBy = UserData.UserId,
                Attachments = attachments
            };

            var result = await _newsBusiness.AddOrUpdate(item);
            if (!result)
            {
                ModelState.AddModelError(string.Empty, "KhÃ´ng thá»ƒ lÆ°u bÃ i Ä‘Äƒng.");
                await LoadMailGroupsAsync();
                return Page();
            }

            await SendNotificationIfNeededAsync(item.Id);
            return Redirect($"/InternalNews/Detail?Id={item.Id}");
        }

        private bool CanCreate()
        {
            return (Permision.Add ?? false) || UserData.RoleCode == "1";
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
