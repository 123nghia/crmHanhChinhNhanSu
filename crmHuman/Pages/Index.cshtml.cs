using crmHuman.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class IndexModel : BaseModel2
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly IDashboardBusinness dashboardBusinness;

        private readonly ImasterDataBussiness _masterDataBusinness;
        private readonly IJobItemBusiness _jobItemBusiness;
        private readonly IEmpBusiness _empBusiness;
        private readonly ILeaveBusiness _leaveBusiness;
        private readonly IScheduleInterviewBussiness _scheduleInterviewBussiness;
        private readonly ILogHistoryBusiness _logHistoryBusiness;
        private readonly INotificationBusiness _notificationBusiness;

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
        public BaseList UpcomingInterviews { get; set; } = new BaseList();
        public List<AppNotification> Notifications { get; set; } = new List<AppNotification>();
        public int UnreadNotificationCount { get; set; }
        public List<CommonIndexModel> InterviewRoundOptions { get; set; } = new List<CommonIndexModel>();
        public List<CommonIndexModel> InterviewModeOptions { get; set; } = new List<CommonIndexModel>();
        public DashboardExtendedStats ExtendedStats { get; set; } = new DashboardExtendedStats();

        public IndexModel(ILogger<IndexModel> logger, IDashboardBusinness dashboardBusinness, ImasterDataBussiness imasterDataBussiness,
        IJobItemBusiness jobItemBusiness, IEmpBusiness empBusiness, ILeaveBusiness leaveBusiness, IScheduleInterviewBussiness scheduleInterviewBussiness,
        ILogHistoryBusiness logHistoryBusiness, INotificationBusiness notificationBusiness)
        {
            _logger = logger;
            this.dashboardBusinness = dashboardBusinness;
            TopOrder = new BaseList();
            TopImpact = new BaseList();
            ReportGroupStatus = new BaseList();
            InfoDashboard = new { };
            _masterDataBusinness = imasterDataBussiness;
            _jobItemBusiness = jobItemBusiness;
            _empBusiness = empBusiness;
            _leaveBusiness = leaveBusiness;
            _scheduleInterviewBussiness = scheduleInterviewBussiness;
            _logHistoryBusiness = logHistoryBusiness;
            _notificationBusiness = notificationBusiness;
            RecordSource = 10;
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
            if (UserData.RoleCode == "CANDIDATE")
            {
                return Redirect("/Candidate/Dashboard");
            }
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
            if (UserData.RoleCode != "2")
            {
                ExtendedStats = await BuildExtendedStats(orderRequest, allOrder);
            }

            var interviewRequest = new ScheduleInterviewRquest
            {
                Status = -1,
                Limit = 10
            };
            var isAdmin = UserData?.RoleCode == "1";
            if (!isAdmin)
            {
                interviewRequest.InterviewerId = UserData.UserId;
            }
            UpcomingInterviews = await _scheduleInterviewBussiness.GetAll(interviewRequest);
            var rounds = await _masterDataBusinness.GetAll(new CommonRequest { Type = 5 });
            InterviewRoundOptions = rounds?.Data?.Cast<CommonIndexModel>().ToList() ?? new List<CommonIndexModel>();
            var modes = await _masterDataBusinness.GetAll(new CommonRequest { Type = 6 });
            InterviewModeOptions = modes?.Data?.Cast<CommonIndexModel>().ToList() ?? new List<CommonIndexModel>();
            Notifications = await _notificationBusiness.GetByReceiverId(UserData.UserId, 10);
            UnreadNotificationCount = await _notificationBusiness.GetUnreadCount(UserData.UserId);

            return Page();
        }

        public async Task<IActionResult> OnPostMarkAllRead()
        {
            GetInfoUser();
            await _notificationBusiness.MarkAllAsRead(UserData.UserId);
            return RedirectToPage();
        }


        private async Task<DashboardExtendedStats> BuildExtendedStats(OrderRequest orderRequest, BaseList allOrder)
        {
            var stats = new DashboardExtendedStats();
            var employees = await LoadEmployeesForDashboard(orderRequest);
            stats.TotalEmployees = employees.Count;

            stats.ContractStats = BuildContractStats(employees, stats.TotalEmployees);
            stats.ExpiringContracts = BuildExpiringContracts(employees, stats.TotalEmployees);
            stats.EmployeeWorking = BuildEmployeeStatusStat(employees, stats.TotalEmployees, false);
            stats.EmployeeResigned = BuildEmployeeStatusStat(employees, stats.TotalEmployees, true);
            stats.OfficialEmployees = BuildOfficialEmployeeStat(employees, stats.TotalEmployees);

            BuildCandidateStats(allOrder, stats);
            stats.LeaveMonth = await BuildLeaveMonthStat();

            return stats;
        }

        private async Task<List<EmployeeExtendedModel>> LoadEmployeesForDashboard(OrderRequest orderRequest)
        {
            var employeeRequest = new EmployeeRequest()
            {
                UserId = UserData.UserId,
                Page = 1,
                Limit = 10000,
                GroupId = orderRequest.GroupId,
                MemberId = orderRequest.MemberId,
                IsDeleted = UserData.RoleCode == "1"
            };
            employeeRequest.From = null;
            employeeRequest.To = null;

            var result = await _empBusiness.GetAllExtended(employeeRequest);
            return result?.Data?.OfType<EmployeeExtendedModel>().ToList() ?? new List<EmployeeExtendedModel>();
        }

        private List<DashboardStat> BuildContractStats(List<EmployeeExtendedModel> employees, int totalEmployees)
        {
            var stats = new List<DashboardStat>();
            var noContract = employees.Where(e => string.IsNullOrWhiteSpace(e.HD_LoaiHD)).ToList();
            stats.Add(new DashboardStat
            {
                Key = "contract-none",
                Title = "Chưa có hợp đồng",
                Value = noContract.Count,
                Percent = CalcPercent(noContract.Count, totalEmployees),
                Details = BuildEmployeeDetails(noContract, _ => null)
            });

            var grouped = employees.Where(e => !string.IsNullOrWhiteSpace(e.HD_LoaiHD))
                .GroupBy(e => e.HD_LoaiHD!.Trim(), StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var group in grouped.OrderBy(g => g.Key))
            {
                var groupEmployees = group.ToList();
                stats.Add(new DashboardStat
                {
                    Key = $"contract-{index++}",
                    Title = group.Key,
                    Value = groupEmployees.Count,
                    Percent = CalcPercent(groupEmployees.Count, totalEmployees),
                    Details = BuildEmployeeDetails(groupEmployees, e => FormatDate(e.HD_NgayKetThuc))
                });
            }

            return stats;
        }

        private DashboardStat BuildExpiringContracts(List<EmployeeExtendedModel> employees, int totalEmployees)
        {
            var today = DateTime.Today;
            var soon = today.AddDays(30);
            var expiring = employees.Where(e => e.HD_NgayKetThuc.HasValue && e.HD_NgayKetThuc.Value.Date >= today && e.HD_NgayKetThuc.Value.Date <= soon).ToList();
            var details = BuildEmployeeDetails(expiring, e => FormatDate(e.HD_NgayKetThuc));
            return new DashboardStat
            {
                Key = "contract-expiring",
                Title = "HĐLĐ sắp đến hạn",
                Value = expiring.Count,
                Percent = CalcPercent(expiring.Count, totalEmployees),
                Details = details
            };
        }

        private DashboardStat BuildEmployeeStatusStat(List<EmployeeExtendedModel> employees, int totalEmployees, bool resigned)
        {
            var filtered = employees.Where(e => IsResigned(e) == resigned).ToList();
            var details = BuildEmployeeDetails(filtered, e => resigned ? FormatDate(e.ResignationDate) : null);
            return new DashboardStat
            {
                Key = resigned ? "employee-resigned" : "employee-working",
                Title = resigned ? "Nhân viên nghỉ việc" : "Nhân viên đang làm việc",
                Value = filtered.Count,
                Percent = CalcPercent(filtered.Count, totalEmployees),
                Details = details
            };
        }

        private DashboardStat BuildOfficialEmployeeStat(List<EmployeeExtendedModel> employees, int totalEmployees)
        {
            var official = employees.Where(IsOfficial).ToList();
            return new DashboardStat
            {
                Key = "employee-official",
                Title = "Nhân viên chính thức",
                Value = official.Count,
                Percent = CalcPercent(official.Count, totalEmployees),
                Details = BuildEmployeeDetails(official, _ => null)
            };
        }

        private void BuildCandidateStats(BaseList allOrder, DashboardExtendedStats stats)
        {
            var orderItems = allOrder?.Data?.OfType<OrderIndexModel>().ToList() ?? new List<OrderIndexModel>();

            var totalGroups = orderItems
                .Where(o => o.CandidateId.HasValue && o.CandidateId.Value > 0)
                .GroupBy(o => o.CandidateId.Value)
                .ToList();

            stats.TotalCandidates = totalGroups.Count;

            var totalDetails = totalGroups
                .Select(g => BuildCandidateDetail(g.Key, g.First()))
                .OrderBy(d => d.Name)
                .ToList();

            stats.CandidateTotal = new DashboardStat
            {
                Key = "candidate-total",
                Title = "Tổng số ứng viên",
                Value = stats.TotalCandidates,
                Percent = 0,
                Details = totalDetails
            };

            var passGroups = orderItems
                .Where(o => o.Result == 1 && o.CandidateId.HasValue && o.CandidateId.Value > 0)
                .GroupBy(o => o.CandidateId.Value)
                .ToList();
            var failGroups = orderItems
                .Where(o => o.Result == 2 && o.CandidateId.HasValue && o.CandidateId.Value > 0)
                .GroupBy(o => o.CandidateId.Value)
                .ToList();
            var interviewedTotal = passGroups.Count + failGroups.Count;
            var passDetails = passGroups
                .Select(g => BuildCandidateDetail(g.Key, g.First()))
                .OrderBy(d => d.Name)
                .ToList();

            stats.CandidatePass = new DashboardStat
            {
                Key = "candidate-pass",
                Title = "Phỏng vấn đạt",
                Value = passGroups.Count,
                Percent = CalcPercent(passGroups.Count, interviewedTotal),
                Details = passDetails
            };
        }

        private static DashboardDetailItem BuildCandidateDetail(int candidateId, OrderIndexModel item)
        {
            var name = !string.IsNullOrWhiteSpace(item.CandidateFullName)
                ? item.CandidateFullName
                : item.UserNameText ?? item.Code ?? $"Candidate {candidateId}";

            return new DashboardDetailItem
            {
                Id = candidateId,
                Name = name,
                Url = candidateId > 0 ? $"/CandidateDetail?id={candidateId}" : null,
                SubText = item.PositionText
            };
        }

        private async Task<DashboardStat> BuildLeaveMonthStat()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var leaveList = await _leaveBusiness.GetLeaveList(null, null, monthStart, monthEnd, 1, 10000);
            var leaves = leaveList?.Data?.OfType<LeaveIndexModel>().ToList() ?? new List<LeaveIndexModel>();
            var validLeaves = leaves.Where(l => l.Status != 6).ToList();
            var totalDays = validLeaves.Sum(l => l.NumDays ?? 0m);
            var approvedDays = validLeaves.Where(l => l.Status == 3 || l.Status == 4).Sum(l => l.NumDays ?? 0m);
            var details = validLeaves.Select(l => new DashboardDetailItem
            {
                Id = l.EmployeeId,
                Name = l.EmployeeName ?? "--",
                Url = l.EmployeeId > 0 ? $"/EmployeeInfo?id={l.EmployeeId}" : null,
                SubText = $"{(l.NumDays ?? 0m):0.#} ngày ({l.FromDate:dd/MM} - {l.ToDate:dd/MM}) - {GetLeaveStatusText(l.Status)}"
            }).ToList();

            return new DashboardStat
            {
                Key = "leave-month",
                Title = "Ngày phép tháng này",
                Value = totalDays,
                Unit = "ngày",
                Percent = CalcPercent(approvedDays, totalDays),
                Details = details
            };
        }

        private static List<DashboardDetailItem> BuildEmployeeDetails(IEnumerable<EmployeeExtendedModel> employees, Func<EmployeeExtendedModel, string?> subTextSelector)
        {
            return employees
                .Select(e => new DashboardDetailItem
                {
                    Id = e.Id,
                    Name = e.FullName ?? e.UserName ?? $"Employee {e.Id}",
                    Url = e.Id > 0 ? $"/EmployeeInfo?id={e.Id}" : null,
                    SubText = subTextSelector(e)
                })
                .OrderBy(d => d.Name)
                .ToList();
        }

        private static string NormalizeForSearch(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(ch);
                }
            }

            return builder.ToString();
        }

        private static bool IsResigned(EmployeeExtendedModel employee)
        {
            if (employee.ResignationDate.HasValue)
            {
                return true;
            }

            var statusText = NormalizeForSearch(employee.StatusWorkText);
            return statusText.IndexOf("nghi", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsOfficial(EmployeeExtendedModel employee)
        {
            var statusText = NormalizeForSearch(employee.StatusWorkText);
            return statusText.IndexOf("chinh", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static decimal CalcPercent(decimal part, decimal total)
        {
            if (total <= 0)
            {
                return 0m;
            }

            return Math.Round((part * 100m) / total, 1, MidpointRounding.AwayFromZero);
        }

        private static string FormatDate(DateTime? date)
        {
            return date.HasValue ? date.Value.ToString("dd/MM/yyyy") : "--";
        }

        private static string GetLeaveStatusText(int status)
        {
            return status switch
            {
                0 => "Chờ Lead duyệt",
                1 => "Chờ HCNS duyệt",
                2 => "Chờ BGD duyệt",
                3 => "Đã duyệt",
                4 => "HCNS duyệt thay",
                5 => "Từ chối",
                6 => "Đã hủy",
                _ => "Không xác định"
            };
        }

        public class DashboardExtendedStats
        {
            public int TotalEmployees { get; set; }
            public int TotalCandidates { get; set; }
            public List<DashboardStat> ContractStats { get; set; } = new List<DashboardStat>();
            public DashboardStat ExpiringContracts { get; set; } = new DashboardStat();
            public DashboardStat EmployeeWorking { get; set; } = new DashboardStat();
            public DashboardStat EmployeeResigned { get; set; } = new DashboardStat();
            public DashboardStat CandidateTotal { get; set; } = new DashboardStat();
            public DashboardStat CandidatePass { get; set; } = new DashboardStat();
            public DashboardStat LeaveMonth { get; set; } = new DashboardStat();
            public DashboardStat OfficialEmployees { get; set; } = new DashboardStat();
        }

        public class DashboardStat
        {
            public string Key { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public decimal Value { get; set; }
            public decimal Percent { get; set; }
            public string? Unit { get; set; }
            public List<DashboardDetailItem> Details { get; set; } = new List<DashboardDetailItem>();
        }

        public class DashboardDetailItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Url { get; set; }
            public string? SubText { get; set; }
        }

    }
}

