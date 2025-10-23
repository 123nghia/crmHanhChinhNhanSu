namespace VS.Human.Rep.Model
{
    public class JobItem : BaseModel
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public int? Field { get; set; }
        public int? CareerId { get; set; }
        public string? Content { get; set; }
        public string? ShortDes { get; set; }
        public int? IsActive { get; set; }
        public int? ProjectId {get;set;}
        public int? PartnerId {get;set;}
        public string? Noted
        {
            get; set;
        }
        public int? Status { get; set; }

        public int? WarrantyDate { get; set; }

        public string? Inputfile { get; set; }

        public JobItem()
        {
            Inputfile = "";
        }

    }
}
