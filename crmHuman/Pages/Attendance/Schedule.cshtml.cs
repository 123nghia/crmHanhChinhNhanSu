using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;
using MasterDataModel = VS.Human.Rep.Model.MasterData;

namespace crmHuman.Pages.Attendance
{
    public class ScheduleModel : BaseModel2
    {
        private const int DepartmentMasterType = 5;
        private readonly IAttendanceBusiness _attendanceBusiness;
        private readonly ImasterDataBussiness _masterDataBusiness;

        public ScheduleModel(
            IAttendanceBusiness attendanceBusiness,
            ImasterDataBussiness masterDataBusiness)
        {
            _attendanceBusiness = attendanceBusiness;
            _masterDataBusiness = masterDataBusiness;
            KeyPage = "Attendance";
            TitlePage = "Ca kíp / lịch làm việc";
        }

        public List<MasterDataModel> Departments { get; private set; } = new List<MasterDataModel>();
        public List<AttendanceDepartmentRuleModel> DepartmentRules { get; private set; } = new List<AttendanceDepartmentRuleModel>();
        public List<AttendanceHolidayModel> Holidays { get; private set; } = new List<AttendanceHolidayModel>();

        [BindProperty]
        public AttendanceDepartmentRuleModel RuleForm { get; set; } = new AttendanceDepartmentRuleModel
        {
            IsActive = true
        };

        [BindProperty]
        public AttendanceHolidayModel HolidayForm { get; set; } = new AttendanceHolidayModel
        {
            FromDate = DateTime.Today,
            ToDate = DateTime.Today,
            IsActive = true
        };

        [BindProperty(SupportsGet = true)]
        public int EditId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int EditHolidayId { get; set; }

        public bool CanManageRules { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                return Page();
            }

            await LoadDataAsync();

            if (EditId > 0)
            {
                var editRule = DepartmentRules.FirstOrDefault(item => item.Id == EditId);
                if (editRule != null)
                {
                    RuleForm = CloneRule(editRule);
                }
            }

            if (EditHolidayId > 0)
            {
                var editHoliday = Holidays.FirstOrDefault(item => item.Id == EditHolidayId);
                if (editHoliday != null)
                {
                    HolidayForm = CloneHoliday(editHoliday);
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSaveRuleAsync()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanManage())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền cập nhật cấu hình chấm công.";
                return RedirectToPage();
            }

            NormalizeRuleForm();
            var validationError = ValidateRuleForm();
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                TempData["ErrorMessage"] = validationError;
                return RedirectToPage(new { editId = RuleForm.Id });
            }

            var saved = await _attendanceBusiness.SaveDepartmentRuleAsync(RuleForm, UserData.UserId);
            if (!saved)
            {
                TempData["ErrorMessage"] = "Không thể lưu cấu hình giờ làm việc.";
                return RedirectToPage(new { editId = RuleForm.Id });
            }

            await RecalculateCurrentMonthAsync();
            TempData["SuccessMessage"] = "Đã lưu cấu hình giờ làm việc theo bộ phận.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteRuleAsync(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanManage())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa cấu hình chấm công.";
                return RedirectToPage();
            }

            var deleted = await _attendanceBusiness.DeleteDepartmentRuleAsync(id, UserData.UserId);
            if (!deleted)
            {
                TempData["ErrorMessage"] = "Không thể xóa cấu hình giờ làm việc.";
                return RedirectToPage();
            }

            await RecalculateCurrentMonthAsync();
            TempData["SuccessMessage"] = "Đã xóa cấu hình giờ làm việc.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveHolidayAsync()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanManage())
            {
                TempData["ErrorMessage"] = "Báº¡n khÃ´ng cÃ³ quyá»n cáº­p nháº­t ngÃ y nghá»‰ lá»….";
                return RedirectToPage();
            }

            NormalizeHolidayForm();
            var validationError = ValidateHolidayForm();
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                TempData["ErrorMessage"] = validationError;
                return RedirectToPage(new { editHolidayId = HolidayForm.Id });
            }

            var saved = await _attendanceBusiness.SaveHolidayAsync(HolidayForm, UserData.UserId);
            if (!saved)
            {
                TempData["ErrorMessage"] = "KhÃ´ng thá»ƒ lÆ°u ngÃ y nghá»‰ lá»….";
                return RedirectToPage(new { editHolidayId = HolidayForm.Id });
            }

            await RecalculateCurrentMonthAsync();
            TempData["SuccessMessage"] = "ÄÃ£ lÆ°u ngÃ y nghá»‰ lá»… vÃ  cáº­p nháº­t cháº¥m cÃ´ng.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteHolidayAsync(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanManage())
            {
                TempData["ErrorMessage"] = "Báº¡n khÃ´ng cÃ³ quyá»n xÃ³a ngÃ y nghá»‰ lá»….";
                return RedirectToPage();
            }

            var deleted = await _attendanceBusiness.DeleteHolidayAsync(id, UserData.UserId);
            if (!deleted)
            {
                TempData["ErrorMessage"] = "KhÃ´ng thá»ƒ xÃ³a ngÃ y nghá»‰ lá»….";
                return RedirectToPage();
            }

            await RecalculateCurrentMonthAsync();
            TempData["SuccessMessage"] = "ÄÃ£ xÃ³a ngÃ y nghá»‰ lá»….";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRunEvaluationAsync()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanManage())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chạy đánh giá chấm công.";
                return RedirectToPage();
            }

            var updated = await RecalculateCurrentMonthAsync();
            TempData["SuccessMessage"] = $"Đã chạy đánh giá lại chấm công. Số dòng cập nhật: {updated}.";
            return RedirectToPage();
        }

        public string FormatTime(TimeSpan? value)
        {
            return value.HasValue ? value.Value.ToString(@"hh\:mm") : string.Empty;
        }

        private async Task LoadDataAsync()
        {
            CanManageRules = CanManage();

            Departments = (await _masterDataBusiness.GetallByTypeData(DepartmentMasterType))
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Code))
                .OrderBy(item => item.Name ?? item.Code)
                .ToList();

            DepartmentRules = (await _attendanceBusiness.GetDepartmentRulesAsync())
                .OrderBy(item => item.DepartmentText ?? item.DepartmentCode)
                .ToList();

            Holidays = (await _attendanceBusiness.GetHolidaysAsync())
                .OrderByDescending(item => item.FromDate)
                .ThenByDescending(item => item.ToDate)
                .ThenBy(item => item.HolidayName)
                .ToList();
        }

        private bool CanManage()
        {
            return (Permision.Add ?? false)
                || (Permision.Edit ?? false)
                || string.Equals(UserData?.RoleCode, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(UserData?.RoleCode, "8", StringComparison.OrdinalIgnoreCase);
        }

        private void NormalizeRuleForm()
        {
            RuleForm.DepartmentCode = RuleForm.DepartmentCode?.Trim() ?? string.Empty;
        }

        private void NormalizeHolidayForm()
        {
            HolidayForm.HolidayName = HolidayForm.HolidayName?.Trim() ?? string.Empty;
            HolidayForm.FromDate = HolidayForm.FromDate.Date;
            HolidayForm.ToDate = HolidayForm.ToDate.Date;
        }

        private string ValidateRuleForm()
        {
            if (string.IsNullOrWhiteSpace(RuleForm.DepartmentCode))
            {
                return "Vui lòng chọn bộ phận.";
            }

            if (!RuleForm.WorkStartTime.HasValue
                || !RuleForm.LunchStartTime.HasValue
                || !RuleForm.LunchEndTime.HasValue
                || !RuleForm.WorkEndTime.HasValue)
            {
                return "Vui lòng nhập đầy đủ giờ vào, nghỉ trưa, vào lại và tan làm.";
            }

            if (!(RuleForm.WorkStartTime.Value < RuleForm.LunchStartTime.Value
                && RuleForm.LunchStartTime.Value <= RuleForm.LunchEndTime.Value
                && RuleForm.LunchEndTime.Value < RuleForm.WorkEndTime.Value))
            {
                return "Thứ tự thời gian không hợp lệ. Cần theo thứ tự: vào làm < nghỉ trưa <= vào lại < tan làm.";
            }

            return string.Empty;
        }

        private string ValidateHolidayForm()
        {
            if (string.IsNullOrWhiteSpace(HolidayForm.HolidayName))
            {
                return "Vui lÃ²ng nháº­p tÃªn ngÃ y nghá»‰ lá»….";
            }

            if (HolidayForm.FromDate == DateTime.MinValue || HolidayForm.ToDate == DateTime.MinValue)
            {
                return "Vui lÃ²ng chá»n tá»« ngÃ y vÃ  Ä‘áº¿n ngÃ y.";
            }

            if (HolidayForm.FromDate.Date > HolidayForm.ToDate.Date)
            {
                return "Khoáº£ng ngÃ y nghá»‰ lá»… khÃ´ng há»£p lá»‡.";
            }

            return string.Empty;
        }

        private async Task<int> RecalculateCurrentMonthAsync()
        {
            var fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            return await _attendanceBusiness.EvaluateAttendanceRangeAsync(fromDate, DateTime.Today, UserData.UserId);
        }

        private static AttendanceDepartmentRuleModel CloneRule(AttendanceDepartmentRuleModel source)
        {
            return new AttendanceDepartmentRuleModel
            {
                Id = source.Id,
                DepartmentCode = source.DepartmentCode,
                DepartmentText = source.DepartmentText,
                WorkStartTime = source.WorkStartTime,
                LunchStartTime = source.LunchStartTime,
                LunchEndTime = source.LunchEndTime,
                WorkEndTime = source.WorkEndTime,
                IsActive = source.IsActive,
                ExpectedWorkHours = source.ExpectedWorkHours
            };
        }

        private static AttendanceHolidayModel CloneHoliday(AttendanceHolidayModel source)
        {
            return new AttendanceHolidayModel
            {
                Id = source.Id,
                HolidayName = source.HolidayName,
                FromDate = source.FromDate,
                ToDate = source.ToDate,
                IsActive = source.IsActive
            };
        }
    }
}
