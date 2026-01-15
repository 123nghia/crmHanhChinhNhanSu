using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.Candidate
{
    [Authorize]
    public class DashboardModel : BaseModel2
    {
        private readonly ICandidateBusiness _candidateBusiness;
        private readonly IScheduleInterviewBussiness _scheduleInterviewBusiness;
        private readonly ImasterDataBussiness _masterDataBusiness;
        private readonly INotificationBusiness _notificationBusiness;

        public VS.Human.Rep.Model.Candidate Candidate { get; set; } = new VS.Human.Rep.Model.Candidate();
        public string StatusText { get; set; } = string.Empty;
        public string DocumentStatusText { get; set; } = string.Empty;
        public BaseList ScheduleList { get; set; } = new BaseList();
        public List<AppNotification> Notifications { get; set; } = new List<AppNotification>();
        public int UnreadNotificationCount { get; set; }

        public DashboardModel(
            ICandidateBusiness candidateBusiness,
            IScheduleInterviewBussiness scheduleInterviewBusiness,
            ImasterDataBussiness masterDataBusiness,
            INotificationBusiness notificationBusiness)
        {
            _candidateBusiness = candidateBusiness;
            _scheduleInterviewBusiness = scheduleInterviewBusiness;
            _masterDataBusiness = masterDataBusiness;
            _notificationBusiness = notificationBusiness;
            TitlePage = "Candidate Dashboard";
            KeyPage = "CandidateDashboard";
        }

        public async Task<IActionResult> OnGet()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (UserData.RoleCode != "CANDIDATE")
            {
                return Redirect("/");
            }

            Candidate = await _candidateBusiness.GetById(UserData.UserId);
            if (Candidate == null || Candidate.Id <= 0)
            {
                return Redirect("/Login");
            }

            var statusList = await _masterDataBusiness.GetallByTypeData(9);
            StatusText = statusList.FirstOrDefault(item => item.Code == Candidate.Status?.ToString())?.Name ?? string.Empty;

            var documentStatusList = await _masterDataBusiness.GetallByTypeData(8);
            DocumentStatusText = documentStatusList.FirstOrDefault(item => item.Code == Candidate.StatusHuman?.ToString())?.Name ?? string.Empty;

            ScheduleList = await _scheduleInterviewBusiness.GetAll(new ScheduleInterviewRquest
            {
                RelId = Candidate.Id,
                Type = -1,
                Page = 1,
                Limit = 20
            });

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
    }
}
