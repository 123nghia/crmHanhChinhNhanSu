using crmHuman.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;

namespace crmHuman.Pages.Attendance
{
    public class IndexModel : BaseModel2
    {
        private readonly IAttendanceBusiness _attendanceBusiness;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IAttendanceBusiness attendanceBusiness, ILogger<IndexModel> logger)
        {
            _attendanceBusiness = attendanceBusiness;
            _logger = logger;
            KeyPage = "Attendance";
            TitlePage = "Quan ly cham cong";
        }

        public AttendanceRequest RequestSearch { get; set; } = new AttendanceRequest();
        public BaseList AttendanceSummary { get; set; } = new BaseList();
        public List<AttendanceSummaryIndexModel> SummaryItems { get; set; } = new List<AttendanceSummaryIndexModel>();
        public AttendanceSummaryIndexModel? SelectedSummary { get; set; }

        public int SelectedEmployeeId { get; set; }
        public string SelectedFingerprintCode { get; set; } = string.Empty;
        public string SelectedMonth { get; set; } = string.Empty;

        public bool IsManager { get; set; }
        public bool IsTcRole { get; set; }
        public bool IsFullAccess { get; set; }

        public async Task<IActionResult> OnGetAsync([FromQuery] AttendanceRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                AttendanceSummary = new BaseList { Data = new List<object>(), Total = 0 };
                return Page();
            }

            RequestSearch = request ?? new AttendanceRequest();
            RequestSearch.Token ??= string.Empty;

            var monthText = RequestSearch.Month;
            var (fromDate, toDate, normalizedMonth) = ResolveMonth(monthText);
            RequestSearch.Month = normalizedMonth;
            RequestSearch.From = fromDate;
            RequestSearch.To = toDate;
            RequestSearch.UserId = UserData.UserId;

            IsFullAccess = IsFullAccessRole();
            IsManager = UserData?.RoleCode == "3";
            IsTcRole = UserData?.RoleCode == "2" && !IsFullAccess;

            if (IsTcRole)
            {
                RequestSearch.EmployeeId = UserData.UserId;
            }
            else if (RequestSearch.EmployeeId.HasValue && RequestSearch.EmployeeId.Value <= 0)
            {
                RequestSearch.EmployeeId = null;
            }

            AttendanceSummary = await _attendanceBusiness.GetSummary(RequestSearch);
            SummaryItems = AttendanceSummary.Data?.OfType<AttendanceSummaryIndexModel>().ToList() ?? new List<AttendanceSummaryIndexModel>();

            SelectedEmployeeId = RequestSearch.EmployeeId ?? 0;
            if (SelectedEmployeeId <= 0 && SummaryItems.Any())
            {
                SelectedEmployeeId = SummaryItems[0].EmployeeId;
            }

            SelectedSummary = SummaryItems.FirstOrDefault(x => x.EmployeeId == SelectedEmployeeId) ?? SummaryItems.FirstOrDefault();
            SelectedFingerprintCode = SelectedSummary?.FingerprintCode ?? string.Empty;
            SelectedMonth = normalizedMonth;

            return Page();
        }

        public async Task<IActionResult> OnGetAttendanceDetailsAsync(int? employeeId, string? fingerprintCode, string? month)
        {
            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                return new JsonResult(new List<object>());
            }

            var (fromDate, toDate, _) = ResolveMonth(month);
            var details = await _attendanceBusiness.GetDetails(employeeId, fingerprintCode, fromDate, toDate, UserData.UserId);

            var response = details.Select(item => new
            {
                WorkDate = item.WorkDate,
                item.DayName,
                CheckIn = FormatTime(item.CheckIn),
                CheckOut = FormatTime(item.CheckOut),
                item.WorkDay,
                item.WorkHours,
                item.WorkDayPlus,
                item.WorkHoursPlus,
                item.LateMinutes,
                item.EarlyMinutes,
                item.ShiftName,
                item.Symbol,
                item.SymbolPlus,
                item.TotalHours,
                item.FingerprintCode
            }).ToList();

            return new JsonResult(response);
        }

        public async Task<IActionResult> OnPostImportAttendance([FromForm] ImportSourceFileAdd request)
        {
            if (!(Permision.Add ?? false))
            {
                return ApiResponseHelper.Error("No permission");
            }

            if (request?.FileRequest == null || request.FileRequest.Length == 0)
            {
                var listError = new List<object>
                {
                    new { content = "File khong hop le" }
                };
                return ApiResponseHelper.BadRequest(listError);
            }

            try
            {
                GetInfoUser();
                var importResult = await _attendanceBusiness.ImportAsync(request.FileRequest, UserData.UserId);
                var formattedErrors = importResult.Errors
                    .Select(e => (object)new { e.Row, e.Content })
                    .ToList();
                var response = new
                {
                    success = importResult.TotalError == 0,
                    importResult.Total,
                    importResult.TotalSuccess,
                    importResult.TotalError,
                    errors = formattedErrors
                };

                if (importResult.TotalError > 0)
                {
                    var errs = string.Join(" | ", importResult.Errors.Select(e => $"Row {e.Row}: {e.Content}"));
                    _logger.LogWarning("Import attendance failed: {Errors}", errs);
                    return ApiResponseHelper.BadRequest(formattedErrors);
                }

                return ApiResponseHelper.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while importing attendance");
                return ApiResponseHelper.Error("Loi he thong khi import. Vui long thu lai sau.");
            }
        }

        public async Task<IActionResult> OnPostSyncAttendanceFromMdb([FromForm] string? month)
        {
            if (!(Permision.Add ?? false))
            {
                return ApiResponseHelper.Error("No permission");
            }

            try
            {
                GetInfoUser();
                var (fromDate, toDate, _) = ResolveMonth(month);
                var syncResult = await _attendanceBusiness.SyncFromAccessAsync(fromDate, toDate, UserData.UserId);
                var formattedErrors = syncResult.Errors
                    .Select(e => (object)new { e.Row, e.Content })
                    .ToList();
                var response = new
                {
                    success = syncResult.TotalError == 0,
                    syncResult.Total,
                    syncResult.TotalSuccess,
                    syncResult.TotalError,
                    errors = formattedErrors
                };

                if (syncResult.TotalError > 0)
                {
                    var errs = string.Join(" | ", syncResult.Errors.Select(e => $"Row {e.Row}: {e.Content}"));
                    _logger.LogWarning("Sync attendance failed: {Errors}", errs);
                    return ApiResponseHelper.BadRequest(formattedErrors);
                }

                return ApiResponseHelper.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while syncing attendance");
                return ApiResponseHelper.Error("Loi he thong khi dong bo. Vui long thu lai sau.");
            }
        }

        private bool IsFullAccessRole()
        {
            if (UserData == null)
            {
                return false;
            }

            if (UserData.RoleCode == "1" || UserData.RoleCode == "8")
            {
                return true;
            }

            return UserData.RoleCode == "2" && string.Equals(UserData.UserName, "VS061", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatTime(TimeSpan? time)
        {
            return time.HasValue ? time.Value.ToString(@"hh\:mm", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static (DateTime fromDate, DateTime toDate, string monthText) ResolveMonth(string? monthText)
        {
            if (string.IsNullOrWhiteSpace(monthText))
            {
                var now = DateTime.Today;
                monthText = now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            }

            if (!DateTime.TryParseExact(monthText + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate))
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                monthText = fromDate.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            }

            var toDate = fromDate.AddMonths(1).AddDays(-1);
            return (fromDate, toDate, monthText);
        }
    }
}
