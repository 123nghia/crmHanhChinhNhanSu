namespace VS.Human.Rep.Model
{
    public class Partner : BaseModel
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public int? IsActive { get; set; }
        public string? Noted { get; set; }

        public string? TaxCode { get; set; }

        public string? ShortName { get; set; }


    }
}
