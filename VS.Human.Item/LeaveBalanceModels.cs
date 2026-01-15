namespace VS.Human.Item
{
    public class LeaveBalanceRequest : BaseRequest
    {
        public LeaveBalanceRequest()
        {
            Page = 1;
            Limit = 20;
            Token = string.Empty;
            From = null;
            To = null;
        }
    }

    public class LeaveBalanceIndexModel : BaseIndexModel
    {
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? DepartmentCode { get; set; }
        public string? DepartmentText { get; set; }
        public string? PositionCode { get; set; }
        public string? PositionText { get; set; }
        public decimal? AllowedLeaveDays { get; set; }
        public decimal? UsedLeaveDays { get; set; }
        public decimal? RemainingLeaveDays { get; set; }
        public decimal? UsedAnnualLeaveDays { get; set; }
        public decimal? UsedSickLeaveDays { get; set; }
        public decimal? UsedPersonalLeaveDays { get; set; }
        public decimal? UsedMaternityLeaveDays { get; set; }
        public decimal? UsedUnpaidLeaveDays { get; set; }
        public decimal? TotalApprovedLeaveDays { get; set; }
    }
}
