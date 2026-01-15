namespace crmHuman.Model
{
    public class LeaveBalanceUpdateRequest
    {
        public int EmployeeId { get; set; }
        public decimal? AllowedLeaveDays { get; set; }
        public decimal? UsedLeaveDays { get; set; }
    }
}
