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
    public class IndexModel : BaseModel2
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly IDashboardBusinness dashboardBusinness;

        private readonly ImasterDataBussiness _masterDataBusinness;
        private readonly IJobItemBusiness _jobItemBusiness;

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
        public dynamic LeaveSummary { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IDashboardBusinness dashboardBusinness, ImasterDataBussiness imasterDataBussiness,
        IJobItemBusiness jobItemBusiness)
        {
            _logger = logger;
            this.dashboardBusinness = dashboardBusinness;
            TopOrder = new BaseList();
            TopImpact = new BaseList();
            ReportGroupStatus = new BaseList();
            InfoDashboard = new { };
            _masterDataBusinness = imasterDataBussiness;
            _jobItemBusiness = jobItemBusiness;
            RecordSource = 10;
        }

        public async Task<IActionResult> OnPostLogOut(LoginRequest request)
        {
            var authenticationScheme =
                HttpContext.User
                .FindFirstValue
                (ClaimTypes.AuthenticationMethod);
            if (authenticationScheme == null)
            {

            }
            await HttpContext.SignOutAsync(authenticationScheme);
            return Redirect("/login");
        }

        public async Task<IActionResult> OnPostCall(CallRequest request)
        {
            var userId = UserData.UserId;
            var data = new StringContent(JsonConvert.SerializeObject(new
            {
                phoneNumber = request.Phone,
                userId,
                lineCode = UserData.LineCode
            }));
            data.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var linkUrl = "http://192.168.1.9:3002";
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(linkUrl);
                var reponse = await client.PostAsync("api/client/makeCall", data);
                var result = await reponse.Content.ReadAsStringAsync();
            }
            var reponseResult = new
            {
                Success = true,
                Data = true
            };
            return new JsonResult(reponseResult) { StatusCode = StatusCodes.Status200OK };
        }

        public async Task<ActionResult> OnGet([FromQuery] OrderRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }
            
            GetInfoUser();
            var roleCodeText = UserData.RoleCode;
            RequestPage = request;
            
            UserActive.DataActiveOnline.AddOrUpdate(UserData.UserId.ToString(), UserData.UserName, UserData.FullName);
            
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
            TopOrder = await dashboardBusinness.GetTopOrder(orderRequest);
            TopImpact = await dashboardBusinness.GetTopImpactOrder(orderRequest);
            ParamDashboard = await dashboardBusinness.GetParam(orderRequest);
            ReportGroupStatus = await dashboardBusinness.StatisticsReport(orderRequest);
            StatusListOverviewDashboard = await dashboardBusinness.GetDashboardStatus(orderRequest);
            var dataReport = ParamDashboard.Data;
            var allOrder = await dashboardBusinness.GetALLOrderInfo(orderRequest);
            double sumTotalCV = 0;
            double totalCVProcess = 0;
            double totalOnboard = 0;
            double cvDone = 0;
            double totalCVNew = 0;
            double totalDone = 0;
            var allCV = await dashboardBusinness.GetAllCV(orderRequest);
            var totalCVInput = allCV.Total;
            if (allOrder.Data != null)
                foreach (var item in allOrder.Data)
                {
                    sumTotalCV++;
                    var iteminfo = item as dynamic;

                    if (iteminfo.Assignee < 1 || iteminfo.Assignee == null)
                    {
                        totalCVNew++;
                        continue;
                    }

                    if (iteminfo.Result < 1 || iteminfo.Result == null)
                    {
                        totalCVProcess++;
                        continue;

                    }
                    if (iteminfo.Result == 1)
                    {
                        totalOnboard++;
                        continue;
                    }

                    if (iteminfo.Result == 2)
                    {
                        cvDone++;
                        continue;
                    }
                }

            if (dataReport == null)
            {
                dataReport = new List<object>();
            }
            InfoDashboard = new
            {
                totalCV = sumTotalCV,
                totalCVProcess = totalCVProcess,
                totalOnboard = totalOnboard,
                totalDone = cvDone,
                totalCVNew = totalCVNew,
                totalCVInput = totalCVInput
            };
            JobList = await _jobItemBusiness.GetAll(new JobRequest());
            StatusList = await _masterDataBusinness.GetAll(new CommonRequest()
            {
                Type = 4

            });
            OrderList = ParamDashboard;
            LeaveSummary = await dashboardBusinness.GetLeaveSummary(UserData.UserId, UserData.RoleCode);
            return Page();
        }


    }
}
