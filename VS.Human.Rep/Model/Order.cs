namespace VS.Human.Rep.Model
{
    public class Order : BaseModel
    {
        public string? Code { get; set; }
        public int? JobId { get; set; }
        public int? Status { get; set; }
        public int? CandidateId { get; set; }
        public int? PartnerId { get; set; }
        public int? Source { get; set; }
        public string? CVLink { get; set; }
        public string? ShortDes { get; set; }
        public int? ProjectId { get; set; }
        public DateTime? DateGet { get; set; }
        public int? Assignee { get; set; }



        public bool Enable { get; set; }
        public string? Noted
        {
            get; set;
        }
        public Order()
        {
            ImpactHIstories = new List<OrderImpactHIstory>();
            Enable = false;
        }
        public List<OrderImpactHIstory> ImpactHIstories { get; set; }

    }
}
