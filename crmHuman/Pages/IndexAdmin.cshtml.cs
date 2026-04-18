using crmHuman.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using VS.Human.Business;
using VS.Human.Item;

namespace crmHuman.Pages
{
    [Authorize]
    public class IndexAdminModel : BaseModel2
    {
        private readonly ILogger<IndexAdminModel> _logger;

        private readonly IDashboardBusinness dashboardBusinness;

        private readonly ImasterDataBussiness _masterDataBusinness;
        private readonly IJobItemBusiness _jobItemBusiness;
        private readonly ILogHistoryBusiness _logHistoryBusiness;
        private readonly IAuditLogBusiness _auditLogBusiness;
        private readonly ICallBussiness _callBusiness;

        public BaseList TopOrder;
        public BaseList TopImpact;

        public BaseList ParamDashboard;
        public BaseList ReportGroupStatus;

        public BaseList StatusListOverviewDashboard { get; set; }

        public BaseList OrderList { get; set; }
        public dynamic InfoDashboard { get; set; }

        public dynamic RequestPage { get; set; }

        public BaseList JobList { get; set; }

        public BaseList StatusList { get; set; }

        public IndexAdminModel(
         ILogger<IndexAdminModel> logger,
        IDashboardBusinness dashboardBusinness,
        ImasterDataBussiness imasterDataBussiness,
        IJobItemBusiness jobItemBusiness,
        ILogHistoryBusiness logHistoryBusiness,
        IAuditLogBusiness auditLogBusiness,
        ICallBussiness callBusiness
            )
        {
            _logger = logger;
            this.dashboardBusinness = dashboardBusinness;
            TopOrder = new BaseList();
            TopImpact = new BaseList();
            ReportGroupStatus = new BaseList();
            InfoDashboard = new { };
            _masterDataBusinness = imasterDataBussiness;
            _jobItemBusiness = jobItemBusiness;
            _logHistoryBusiness = logHistoryBusiness;
            _auditLogBusiness = auditLogBusiness;
            _callBusiness = callBusiness;
        }

        public async Task<IActionResult> OnPostLogOut(LoginRequest request)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var idUser = identity.Claims.FirstOrDefault(o => o.Type == "userId")?.Value;
                var userName = identity.Claims.FirstOrDefault(o => o.Type == "UserName")?.Value;
                var fullName = identity.Claims.FirstOrDefault(o => o.Type == "FullName")?.Value;
                var roleCode = identity.Claims.FirstOrDefault(o => o.Type == "RoleCode")?.Value;
                if (!string.IsNullOrWhiteSpace(idUser))
                {
                    UserActive.DataActiveOnline.MarkLogout(idUser, userName, fullName);
                    if (int.TryParse(idUser, out var userId))
                    {
                        await _logHistoryBusiness.LogLogout(userId, roleCode);
                    }
                }
            }

            var authenticationScheme = HttpContext.User.FindFirstValue(ClaimTypes.AuthenticationMethod);
            if (authenticationScheme == null)
            {


            }
            await HttpContext.SignOutAsync(authenticationScheme);
            return Redirect("/login");

        }


        public async Task<IActionResult> OnPostCall(CallRequest request)
        {
            GetInfoUser();
            var userId = UserData.UserId;
            var result = await _callBusiness.MakeCallDetailed(request.Phone, "Dashboard", null, UserData.LineCode, userId, "IndexAdmin");
            var reponseResult = new
            {
                Success = result.Success,
                Data = result.Success,
                result.CallLogId,
                result.ProviderCallId,
                Message = result.Success ? "Da kich hoat client goi." : result.Error


            };
            return new JsonResult(reponseResult) { StatusCode = StatusCodes.Status200OK };
        }

        public async Task<ActionResult> OnGet([FromQuery] OrderRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }


            var userId = UserData.UserId;
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var roleCodeText = "";
            RequestPage = request;

            if (identity != null)
            {
                var userClaims = identity.Claims;
                var idUser = identity.Claims.FirstOrDefault(o => o.Type == "userId")?.Value;
                var userName = userClaims.FirstOrDefault(o => o.Type == "UserName")?.Value;
                var roleCode = userClaims.FirstOrDefault(o => o.Type == "RoleCode")?.Value;
                var fullName = userClaims.FirstOrDefault(o => o.Type == "FullName")?.Value;
                var lineCode = userClaims.FirstOrDefault(o => o.Type == "LineCode")?.Value;
                if (UserData == null)
                {
                    UserData = new UserDataView();
                }
                UserData.UserName = userName;
                UserData.FullName = fullName;
                UserData.UserId = int.Parse(idUser);
                UserData.RoleCode = roleCode;
                UserData.LineCode = lineCode;
                roleCodeText = roleCode;
                UserActive.DataActiveOnline.AddOrUpdate(idUser, userName, fullName);
            }

            RequestPage.UserId = UserData.UserId;

            RequestPage.RoleCode = UserData.RoleCode;




            if (RequestPage.RoleCode == "4")
            {

                this.Permision.SearchGrop = false;
            }
            if (UserData.RoleCode == "2" || UserData.RoleCode == "4")
            {
                this.Permision.SearchGrop = false;
            }
            if (UserData.RoleCode == "3")
            {

                if (UserData.UserId == 37)
                {
                    RequestPage.GroupId = 6;


                }
                else if (UserData.UserId == 38)
                {
                    RequestPage.GroupId = 7;

                }
            }

            var orderRequest = new OrderRequest()
            {
                UserId = UserData.UserId,
                Limit = 10,
                From = request.From,
                Status = request.Status,
                Job = request.Job,
                GroupId = request.GroupId,
                MemberId = request.MemberId,
                To = request.To
            };
            orderRequest.RoleCode = roleCodeText;
            var allOrderApply = await dashboardBusinness.GetAllOrderApply(orderRequest);
            var allOnboardCV = await dashboardBusinness.GetAllOnboardCV(orderRequest);
            var allOrderDraft = await dashboardBusinness.GetAllOrderDraft(orderRequest);
            var allOrderCV = await dashboardBusinness.GetALlOrderRemove(orderRequest);

            var allCountCVChartAllCV = await dashboardBusinness.GetAllCountCVGroupByDate(orderRequest);

            var allCountCVApply = await dashboardBusinness.GetAllCountCVApplyGroupByDate(orderRequest);

            var allCountCVOnboard = await dashboardBusinness.GetAllCountCVOnboardGroupByDate(orderRequest);
            double totalCVAply = 0;
            double totalCVUV = 0;
            double sumTotalCV = 0;
            double sumOnboard = 0;
            var listOrderDraft = new List<Object>();
            var listOrderApply = new List<Object>();

            double sumOnboardcV = 0;
            double SumcvPass = 0;
            foreach (var item in allOnboardCV.Data)
            {
                sumOnboardcV++;
                var iteminfo = item as dynamic;
                if (iteminfo.Result == 1)
                {
                    SumcvPass++;
                }


            }

            string rateCVPass = "0";

            if (sumOnboardcV > 0 && SumcvPass > 0)
            {
                rateCVPass = (SumcvPass / sumOnboardcV).ToString("0.00%");
            }
            foreach (var item in allOrderDraft.Data)
            {
                var iteminfo = item as dynamic;
                totalCVUV = iteminfo.TotalRecord;
                listOrderDraft.Add(iteminfo);
            }

            foreach (var item in allOrderApply.Data)
            {
                var iteminfo = item as dynamic;
                totalCVAply = iteminfo.TotalRecord;
                if (iteminfo.Result == 1)
                {
                    sumOnboard++;
                }
                listOrderApply.Add(iteminfo);
            }
            foreach (var item in allOrderCV.Data)
            {
                var iteminfo = item as dynamic;
                sumTotalCV = iteminfo.TotalRecord;



            }
            var listUser = UserActive.DataActiveOnline.GetListUser(UserData.RoleCode, UserData.UserId);
            var auditStats = await _auditLogBusiness.GetTodayStatsAsync();

            string rateUV = "0";
            string rateOB = "0";
            if (totalCVAply > 0 && sumTotalCV > 0)
            {
                rateUV = (totalCVAply / sumTotalCV).ToString("0.00%");
            }

            if (totalCVAply > 0 && sumOnboard > 0 && sumTotalCV > 0)
            {
                rateOB = (sumOnboard / totalCVAply).ToString("0.00%");
            }
            InfoDashboard = new
            {

                rateOB,
                rateUV,
                totalCVUV,
                totalCVAply,
                sumTotalCV,
                sumOnboard,
                listOrderDraft,
                listOrderApply,
                allCountCVChartAllCV,
                allCountCVApply,
                listUser,
                allCountCVOnboard,
                rateCVPass,
                sumOnboardcV,
                SumcvPass,
                allOnboardCV,
                totalRequestsToday = auditStats.TotalRequests,
                totalVisitsToday = auditStats.TotalVisits
            };
            return Page();
        }


    }
}
