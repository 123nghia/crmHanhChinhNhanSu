using System;

namespace VS.Human.Rep.Model
{
    public class AttendanceRecord : BaseModel
    {
        public int? EmployeeId { get; set; }
        public string FingerprintCode { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? PositionName { get; set; }
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
        public decimal? Shift1 { get; set; }
        public decimal? Shift2 { get; set; }
        public decimal? Shift3 { get; set; }
        public string? ShiftName { get; set; }
        public string? Symbol { get; set; }
        public string? SymbolPlus { get; set; }
        public decimal? TotalHours { get; set; }
        public string? SourceFile { get; set; }
        public int? RowIndex { get; set; }
        public bool IsLocked { get; set; }
    }
}
