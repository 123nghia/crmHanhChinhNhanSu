using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace crmHuman.Pages.InternalNews
{
    [Authorize]
    public class UploadImageModel : BaseModel2
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public UploadImageModel(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            KeyPage = "InternalNews";
        }

        public async Task<IActionResult> OnPostAsync(IFormFile file)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            GetInfoUser();
            var canUpload = (Permision.Add ?? false) || (Permision.Edit ?? false) || (UserData?.RoleCode == "1");
            if (!canUpload)
            {
                return Forbid();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".gif",
                ".webp",
                ".bmp"
            };

            if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
            {
                return BadRequest(new { error = "Unsupported file type" });
            }

            var uploadRoot = Path.Combine(_hostingEnvironment.WebRootPath, "assets", "internal-news");
            if (!Directory.Exists(uploadRoot))
            {
                Directory.CreateDirectory(uploadRoot);
            }

            var storedName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadRoot, storedName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var location = $"/assets/internal-news/{storedName}";
            return new JsonResult(new { location });
        }
    }
}
