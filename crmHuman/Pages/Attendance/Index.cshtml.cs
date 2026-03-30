using crmHuman.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using OfficeOpenXml;
using OfficeOpenXml.Style;
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
            RequestSearch.FingerprintCode = string.IsNullOrWhiteSpace(RequestSearch.FingerprintCode)
                ? null
                : RequestSearch.FingerprintCode.Trim();

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

            if (!string.IsNullOrWhiteSpace(RequestSearch.FingerprintCode))
            {
                SummaryItems = SummaryItems
                    .Where(x => string.Equals(x.FingerprintCode, RequestSearch.FingerprintCode, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                AttendanceSummary.Total = SummaryItems.Count;
                AttendanceSummary.Data = SummaryItems;
            }

            SelectedEmployeeId = RequestSearch.EmployeeId ?? 0;
            SelectedFingerprintCode = RequestSearch.FingerprintCode ?? string.Empty;

            SelectedSummary = FindSelectedSummary(SummaryItems, SelectedEmployeeId, SelectedFingerprintCode)
                ?? SummaryItems.FirstOrDefault();
            SelectedEmployeeId = SelectedSummary?.EmployeeId ?? SelectedEmployeeId;
            SelectedFingerprintCode = SelectedSummary?.FingerprintCode ?? SelectedFingerprintCode;
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

        public async Task<IActionResult> OnPostExport([FromForm] AttendanceRequest request)
        {
            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                return ApiResponseHelper.Error("No permission");
            }

            request ??= new AttendanceRequest();
            request.Token ??= string.Empty;

            var (fromDate, toDate, normalizedMonth) = ResolveMonth(request.Month);
            request.Month = normalizedMonth;
            request.From = fromDate;
            request.To = toDate;
            request.UserId = UserData.UserId;

            IsFullAccess = IsFullAccessRole();
            IsManager = UserData?.RoleCode == "3";
            IsTcRole = UserData?.RoleCode == "2" && !IsFullAccess;

            if (IsTcRole)
            {
                request.EmployeeId = UserData.UserId;
            }
            else if (request.EmployeeId.HasValue && request.EmployeeId.Value <= 0)
            {
                request.EmployeeId = null;
            }

            var summary = await _attendanceBusiness.GetSummary(request);
            var items = summary.Data?.OfType<AttendanceSummaryIndexModel>().ToList() ?? new List<AttendanceSummaryIndexModel>();

            if (request.EmployeeId.HasValue)
            {
                items = items.Where(x => x.EmployeeId == request.EmployeeId.Value).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.FingerprintCode))
            {
                items = items.Where(x => string.Equals(x.FingerprintCode, request.FingerprintCode, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (items.Count == 0)
            {
                var errorBytes = global::System.Text.Encoding.UTF8.GetBytes("Kh\u00F4ng c\u00F3 d\u1EEF li\u1EC7u \u0111\u1EC3 xu\u1EA5t.");
                return File(errorBytes, "text/plain", "Khong_co_du_lieu.txt");
            }

            var headers = new[]
            {
                "M\u00E3 NV",
                "T\u00EAn nh\u00E2n vi\u00EAn",
                "Ph\u00F2ng ban",
                "Ch\u1EE9c v\u1EE5",
                "Ng\u00E0y",
                "Th\u1EE9",
                "V\u00E0o",
                "Ra",
                "C\u00F4ng",
                "Gi\u1EDD",
                "C\u00F4ng+",
                "Gi\u1EDD+",
                "V\u00E0o tr\u1EC5",
                "Ra s\u1EDBm",
                "TC1",
                "TC2",
                "TC3",
                "T\u00EAn ca",
                "K\u00FD hi\u1EC7u",
                "K\u00FD hi\u1EC7u+",
                "T\u1ED5ng gi\u1EDD"
            };

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Chi tiết chấm công");

            var colCount = headers.Length;
            worksheet.Cells[1, 1, 1, colCount].Merge = true;
            worksheet.Cells[1, 1].Value = "CHI TI\u1EBET CH\u1EA4M C\u00D4NG";
            worksheet.Cells[2, 1, 2, colCount].Merge = true;
            worksheet.Cells[2, 1].Value = $"T\u1EEB ng\u00E0y {fromDate:dd/MM/yyyy} \u0111\u1EBFn ng\u00E0y {toDate:dd/MM/yyyy}";

            worksheet.Cells[1, 1, 2, colCount].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[1, 1, 2, colCount].Style.Font.Bold = true;

            const int headerRow = 4;
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[headerRow, i + 1].Value = headers[i];
            }

            worksheet.Cells[headerRow, 1, headerRow, colCount].Style.Font.Bold = true;
            worksheet.Cells[headerRow, 1, headerRow, colCount].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[headerRow, 1, headerRow, colCount].Style.Fill.BackgroundColor.SetColor(global::System.Drawing.Color.LightGray);

            var row = headerRow + 1;
            var dateList = BuildDateRange(fromDate, toDate);

            foreach (var item in items)
            {
                var details = await _attendanceBusiness.GetDetails(item.EmployeeId, item.FingerprintCode, fromDate, toDate, UserData.UserId);
                var detailMap = details.ToDictionary(d => d.WorkDate.Date, d => d);

                foreach (var date in dateList)
                {
                    detailMap.TryGetValue(date, out var detail);

                    var checkIn = FormatTime(detail?.CheckIn);
                    var checkOut = FormatTime(detail?.CheckOut);
                    var workDay = detail?.WorkDay ?? 0;
                    var workHours = detail?.WorkHours ?? 0;
                    var workDayPlus = detail?.WorkDayPlus ?? 0;
                    var workHoursPlus = detail?.WorkHoursPlus ?? 0;
                    var lateMinutes = detail?.LateMinutes ?? 0;
                    var earlyMinutes = detail?.EarlyMinutes ?? 0;
                    var totalHours = detail?.TotalHours ?? detail?.WorkHours ?? 0;

                    int col = 1;
                    worksheet.Cells[row, col++].Value = item.FingerprintCode;
                    worksheet.Cells[row, col++].Value = item.FullName;
                    worksheet.Cells[row, col++].Value = item.DepartmentText;
                    worksheet.Cells[row, col++].Value = item.PositionText;
                    worksheet.Cells[row, col++].Value = date.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, col++].Value = GetDayNameShort(date);
                    worksheet.Cells[row, col++].Value = checkIn;
                    worksheet.Cells[row, col++].Value = checkOut;
                    worksheet.Cells[row, col++].Value = workDay;
                    worksheet.Cells[row, col++].Value = workHours;
                    worksheet.Cells[row, col++].Value = workDayPlus;
                    worksheet.Cells[row, col++].Value = workHoursPlus;
                    worksheet.Cells[row, col++].Value = lateMinutes;
                    worksheet.Cells[row, col++].Value = earlyMinutes;
                    worksheet.Cells[row, col++].Value = 0;
                    worksheet.Cells[row, col++].Value = 0;
                    worksheet.Cells[row, col++].Value = 0;
                    worksheet.Cells[row, col++].Value = detail?.ShiftName;
                    worksheet.Cells[row, col++].Value = detail?.Symbol;
                    worksheet.Cells[row, col++].Value = detail?.SymbolPlus;
                    worksheet.Cells[row, col++].Value = totalHours;

                    row++;
                }
            }

            if (row > headerRow + 1)
            {
                var dataRange = worksheet.Cells[headerRow, 1, row - 1, colCount];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var fileContents = package.GetAsByteArray();
            var fileName = $"ChamCong_{fromDate:yyyyMM}_ChiTiet.xlsx";
            return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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

        private static string GetDayNameShort(DateTime date)
        {
            return date.DayOfWeek switch
            {
                DayOfWeek.Monday => "Hai",
                DayOfWeek.Tuesday => "Ba",
                DayOfWeek.Wednesday => "Tư",
                DayOfWeek.Thursday => "Năm",
                DayOfWeek.Friday => "Sáu",
                DayOfWeek.Saturday => "Bảy",
                _ => "CN"
            };
        }

        private static List<DateTime> BuildDateRange(DateTime from, DateTime to)
        {
            var list = new List<DateTime>();
            var start = from.Date;
            var end = to.Date;
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                list.Add(date);
            }
            return list;
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

        private static AttendanceSummaryIndexModel? FindSelectedSummary(
            IEnumerable<AttendanceSummaryIndexModel> items,
            int selectedEmployeeId,
            string? selectedFingerprintCode)
        {
            var normalizedFingerprint = string.IsNullOrWhiteSpace(selectedFingerprintCode)
                ? null
                : selectedFingerprintCode.Trim();

            if (selectedEmployeeId > 0 && normalizedFingerprint != null)
            {
                return items.FirstOrDefault(x =>
                    x.EmployeeId == selectedEmployeeId &&
                    string.Equals(x.FingerprintCode, normalizedFingerprint, StringComparison.OrdinalIgnoreCase));
            }

            if (selectedEmployeeId > 0)
            {
                return items.FirstOrDefault(x => x.EmployeeId == selectedEmployeeId);
            }

            if (normalizedFingerprint != null)
            {
                return items.FirstOrDefault(x =>
                    string.Equals(x.FingerprintCode, normalizedFingerprint, StringComparison.OrdinalIgnoreCase));
            }

            return items.FirstOrDefault();
        }
    }
}
