namespace VS.Human.Rep.Model
{
    public class OrderImpactHIstory : BaseModel
    {
        public string? OrderCode { get; set; }

        public string? ObjectInfo { get; set; }
        public int? OldStatus { get; set; }
        public int? NewStatus { get; set; }
        public string? Noted
        {
            get; set;
        }

        public string? TxtTimer { get; set; }
        public DateTime? DateFrom { get; set; }

        public string? TxtPlace { get; set; }
        public OrderImpactHIstory()
        {

        }

    }
}
