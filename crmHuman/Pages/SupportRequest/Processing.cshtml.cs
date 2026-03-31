using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.SupportRequest
{
    [Authorize]
    public class ProcessingModel : BaseModel2
    {
        private readonly ISupportRequestBusiness _supportRequestBusiness;
        private readonly ImasterDataBussiness _masterDataBusiness;

        [BindProperty(SupportsGet = true)]
        public string? Token { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TargetDepartmentCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Status { get; set; }

        [BindProperty]
        public SupportRequestStatusUpdate UpdateModel { get; set; } = new SupportRequestStatusUpdate();

        public BaseList RequestList { get; set; } = new BaseList();
        public List<VS.Human.Rep.Model.MasterData> Departments { get; set; } = new List<VS.Human.Rep.Model.MasterData>();

        public ProcessingModel(
            ISupportRequestBusiness supportRequestBusiness,
            ImasterDataBussiness masterDataBusiness)
        {
            _supportRequestBusiness = supportRequestBusiness;
            _masterDataBusiness = masterDataBusiness;
            KeyPage = "SupportRequestProcessing";
            TitlePage = "Xử lý yêu cầu hỗ trợ";
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

        public async Task<IActionResult> OnPostUpdateStatusAsync()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            var result = await _supportRequestBusiness.UpdateStatus(UpdateModel, UserData.UserId, UserData.RoleCode);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return Redirect("/SupportRequest/Processing");
        }

        public static string GetStatusText(int status) => RequestModel.GetStatusText(status);
        public static string GetStatusClass(int status) => RequestModel.GetStatusClass(status);
        public static string GetStatusTooltip(int status) => RequestModel.GetStatusTooltip(status);

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
                IsProcessingView = true,
                Page = page,
                Limit = limit,
                Token = Token,
                Status = Status,
                TargetDepartmentCode = string.IsNullOrWhiteSpace(TargetDepartmentCode) ? null : TargetDepartmentCode
            });
        }
    }
}
