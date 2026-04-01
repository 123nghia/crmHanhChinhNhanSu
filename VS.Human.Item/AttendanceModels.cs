using System;

namespace VS.Human.Item
{
    public class AttendanceRequest : BaseRequest
    {
        public AttendanceRequest()
        {
            Page = 1;
            Limit = 50;
            Token = string.Empty;
        }

        // Format: yyyy-MM
        public string? Month { get; set; }
        public int? EmployeeId { get; set; }
        public string? FingerprintCode { get; set; }
    }

    public class AttendanceSummaryIndexModel : BaseIndexModel
    {
        public int EmployeeId { get; set; }
        public string? FingerprintCode { get; set; }
        public string? FullName { get; set; }
        public string? DepartmentText { get; set; }
        public string? PositionText { get; set; }
        public decimal? TotalWorkDays { get; set; }
        public decimal? TotalWorkHours { get; set; }
        public decimal? TotalOvertimeHours { get; set; }
        public decimal? TotalHours { get; set; }
        public int? LateCount { get; set; }
        public int? LateMinutes { get; set; }
        public int? EarlyCount { get; set; }
        public int? EarlyMinutes { get; set; }
        public int? OffCount { get; set; }
    }

    public class AttendanceDetailModel : BaseIndexModel
    {
        public DateTime WorkDate { get; set; }
        public string? DayName { get; set; }
        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }
        public decimal? WorkDay { get; set; }
        public decimal? WorkHours { get; set; }
        public decimal? WorkDayPlus { get; set; }
        public decimal? WorkHoursPlus { get; set; }
        public int? LateMinutes { get; set; }
        public int? EarlyMinutes { get; set; }
        public string? ShiftName { get; set; }
        public string? Symbol { get; set; }
        public string? SymbolPlus { get; set; }
        public decimal? TotalHours { get; set; }
        public string? FingerprintCode { get; set; }
    }

    public class AttendanceDepartmentRuleModel : BaseIndexModel
    {
        public string DepartmentCode { get; set; } = string.Empty;
        public string? DepartmentText { get; set; }
        public TimeSpan? WorkStartTime { get; set; }
        public TimeSpan? LunchStartTime { get; set; }
        public TimeSpan? LunchEndTime { get; set; }
        public TimeSpan? WorkEndTime { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal? ExpectedWorkHours { get; set; }
    }

    public class AttendanceHolidayModel : BaseIndexModel
    {
        public string HolidayName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
