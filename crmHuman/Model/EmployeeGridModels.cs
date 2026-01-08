namespace crmHuman.Model
{
    /// <summary>
    /// DTO for quick update from editable grid
    /// </summary>
    public class EmployeeQuickUpdate
    {
        public int? Id { get; set; }
        public string? FullName { get; set; }
        public string? RoleCode { get; set; }
        public string? PositionCode { get; set; }
        public string? DepartmentCode { get; set; }
        public int? GroupId { get; set; }
        public int? Status { get; set; }
        public string? StatusWork { get; set; }
        public string? DocumentStatus { get; set; }
        public DateTime? Onboard { get; set; }
    }

    /// <summary>
    /// DTO for quick add from editable grid
    /// </summary>
    public class EmployeeQuickAdd
    {
        public string? FullName { get; set; }
        public string? RoleCode { get; set; }
        public string? PositionCode { get; set; }
        public string? DepartmentCode { get; set; }
        public int? GroupId { get; set; }
        public int? Status { get; set; }
        public string? StatusWork { get; set; }
        public string? DocumentStatus { get; set; }
        public DateTime? Onboard { get; set; }
    }
}
