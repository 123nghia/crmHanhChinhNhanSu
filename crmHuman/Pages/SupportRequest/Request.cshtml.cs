using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.SupportRequest
{
    [Authorize]
    public class RequestModel : BaseModel2
    {
        private readonly ISupportRequestBusiness _supportRequestBusiness;
        private readonly ImasterDataBussiness _masterDataBusiness;
        private readonly IWebHostEnvironment _hostingEnvironment;

        [BindProperty]
        public SupportRequestItem FormModel { get; set; } = new SupportRequestItem();

        [BindProperty]
        public List<IFormFile> AttachmentFiles { get; set; } = new List<IFormFile>();

        [BindProperty(SupportsGet = true)]
        public string? Token { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TargetDepartmentCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Status { get; set; }

        public BaseList RequestList { get; set; } = new BaseList();
        public List<VS.Human.Rep.Model.MasterData> Departments { get; set; } = new List<VS.Human.Rep.Model.MasterData>();

        public RequestModel(
            ISupportRequestBusiness supportRequestBusiness,
            ImasterDataBussiness masterDataBusiness,
            IWebHostEnvironment hostingEnvironment)
        {
            _supportRequestBusiness = supportRequestBusiness;
            _masterDataBusiness = masterDataBusiness;
            _hostingEnvironment = hostingEnvironment;
            KeyPage = "SupportRequest";
            TitlePage = "Yeu cau ho tro";
        }

        public async Task<IActionResult> OnGetAsync(int page = 1, int limit = 20)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!(Permision.View ?? false) && UserData.RoleCode != "1")
            {
                return Redirect("/");
            }

            await LoadPageDataAsync(page, limit);
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!(Permision.Add ?? false) && UserData.RoleCode != "1")
            {
                return Redirect("/SupportRequest/Request");
            }

            FormModel.Attachments = await SaveAttachmentsAsync();
            var result = await _supportRequestBusiness.Create(FormModel, UserData.UserId);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

            if (!result.Success)
            {
                await LoadPageDataAsync(1, 20);
                return Page();
            }

            return Redirect("/SupportRequest/Request");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            var result = await _supportRequestBusiness.Delete(id, UserData.UserId, UserData.RoleCode);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return Redirect("/SupportRequest/Request");
        }

        public static string GetStatusText(int status)
        {
            return status switch
            {
                0 => "Moi tao",
                1 => "Dang xu ly",
                2 => "Hoan thanh",
                3 => "Da huy",
                _ => "Khong xac dinh"
            };
        }

        public static string GetStatusClass(int status)
        {
            return status switch
            {
                0 => "bg-warning text-dark",
                1 => "bg-info text-dark",
                2 => "bg-success",
                3 => "bg-secondary",
                _ => "bg-light text-dark"
            };
        }

        public static string GetStatusTooltip(int status)
        {
            return status switch
            {
                0 => "Yeu cau vua tao va da duoc he thong gan nguoi phu trach.",
                1 => "Bo phan duoc giao dang tiep nhan va xu ly yeu cau.",
                2 => "Yeu cau da xu ly xong.",
                3 => "Yeu cau da dong ma khong tiep tuc xu ly.",
                _ => "Trang thai khong xac dinh."
            };
        }

        private async Task LoadPageDataAsync(int page, int limit)
        {
            Departments = (await _masterDataBusiness.GetallByTypeData(5))
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Code))
                .OrderBy(x => x.Name)
                .ToList();

            RequestList = await _supportRequestBusiness.GetAll(new SupportRequestListRequest
            {
                UserId = UserData.UserId,
                RoleCode = UserData.RoleCode,
                IsProcessingView = false,
                Page = page,
                Limit = limit,
                Token = Token,
                Status = Status,
                TargetDepartmentCode = string.IsNullOrWhiteSpace(TargetDepartmentCode) ? null : TargetDepartmentCode
            });
        }

        private async Task<List<SupportRequestAttachment>> SaveAttachmentsAsync()
        {
            var attachments = new List<SupportRequestAttachment>();
            if (AttachmentFiles == null || AttachmentFiles.Count == 0)
            {
                return attachments;
            }

            var uploadRoot = Path.Combine(_hostingEnvironment.WebRootPath, "assets", "support-requests");
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

                attachments.Add(new SupportRequestAttachment
                {
                    FileName = safeName,
                    FilePath = $"/assets/support-requests/{storedName}",
                    FileSize = file.Length
                });
            }

            return attachments;
        }
    }
}
