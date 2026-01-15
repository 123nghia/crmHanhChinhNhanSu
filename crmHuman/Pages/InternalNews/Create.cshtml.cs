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
        private readonly IWebHostEnvironment _hostingEnvironment;

        [BindProperty]
        public InternalNewsAdd News { get; set; } = new InternalNewsAdd();

        [BindProperty]
        public List<IFormFile> AttachmentFiles { get; set; } = new List<IFormFile>();

        public CreateModel(IInternalNewsBusiness newsBusiness, IWebHostEnvironment hostingEnvironment)
        {
            _newsBusiness = newsBusiness;
            _hostingEnvironment = hostingEnvironment;
            TitlePage = "Tạo tin nội bộ";
            KeyPage = "InternalNews";
        }

        public IActionResult OnGet()
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
                ModelState.AddModelError("News.Title", "Vui lòng nhập tiêu đề.");
            }
            else if (News.Title.Length > 200)
            {
                ModelState.AddModelError("News.Title", "Tiêu đề tối đa 200 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(News.Content))
            {
                ModelState.AddModelError("News.Content", "Vui lòng nhập nội dung.");
            }

            if (!ModelState.IsValid)
            {
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
                CreatedBy = UserData.UserId,
                UpdatedBy = UserData.UserId,
                Attachments = attachments
            };

            var result = await _newsBusiness.AddOrUpdate(item);
            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Không thể lưu bài đăng.");
                return Page();
            }

            return Redirect("/InternalNews");
        }

        private bool CanCreate()
        {
            return (Permision.Add ?? false) || UserData.RoleCode == "1";
        }
    }
}
