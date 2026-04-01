using System;

namespace VS.Human.Rep.Model
{
    public class AttendanceEvaluationRecord
    {
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public string FingerprintCode { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }
        public decimal? WorkDay { get; set; }
        public decimal? WorkHours { get; set; }
        public decimal? TotalHours { get; set; }
        public int? LateMinutes { get; set; }
        public int? EarlyMinutes { get; set; }
        public string? Symbol { get; set; }
        public string? SymbolPlus { get; set; }
        public string? DepartmentCode { get; set; }
        public string? DepartmentText { get; set; }
    }

    public class AttendanceEvaluationEmployee
    {
        public int EmployeeId { get; set; }
        public string FingerprintCode { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public string? DepartmentCode { get; set; }
        public string? DepartmentText { get; set; }
        public string? PositionText { get; set; }
        public DateTime? OnboardDate { get; set; }
        public DateTime? ResignationDate { get; set; }
    }

    public class AttendanceApprovedLeave
    {
        public int EmployeeId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? LeaveTypeCode { get; set; }
        public string? LeaveTypeName { get; set; }
    }

    public class AttendanceEvaluationUpdate
    {
        public int Id { get; set; }
        public decimal? WorkDay { get; set; }
        public decimal? WorkHours { get; set; }
        public decimal? TotalHours { get; set; }
        public int? LateMinutes { get; set; }
        public int? EarlyMinutes { get; set; }
        public string? Symbol { get; set; }
        public string? ShiftName { get; set; }
    }
}
