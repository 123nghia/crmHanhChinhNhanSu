namespace VS.Human.Rep.Model
{
    public class OnboardMember : BaseModel
    {
        public string? Code { get; set; }
        public int? JobId { get; set; }
        public int? Status { get; set; }
        public int? SystemStatus { get; set; }
        public int? CandidateId { get; set; }
        public int? PartnerId { get; set; }
        public int? ProjectId { get; set; }
        public string? OrderCode { get; set; }
        public int? Source { get; set; }
        public string? CVLink { get; set; }
        public string? ShortDes { get; set; }
        public DateTime? OnboardDate { get; set; }

        public DateTime? Warrantydate { get; set; }
        public int? Dpd { get; set; }
        public int? Assignee { get; set; }
        public string? Noted
        {
            get; set;
        }
        public OnboardMember()
        {


        }


    }
}
