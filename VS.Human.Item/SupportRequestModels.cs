namespace VS.Human.Item
{
    public class SupportRequestListRequest : BaseRequest
    {
        public string? TargetDepartmentCode { get; set; }
        public string? RoleCode { get; set; }
        public bool IsProcessingView { get; set; }
    }
}
