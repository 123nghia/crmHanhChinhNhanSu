namespace VS.Human.Rep.Model
{
    public class TaxItem : BaseModel
    {
        // item.UserName,
        // item.CodeId,
        // item.Number,
        // item.RegBHYT,
        // item.PageTax,
        // item.BiaSo,

        public string UserName { get; set; } = string.Empty;
        public string CodeId { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string RegBHYT { get; set; } = string.Empty;
        public int PageTax { get; set; } 
         public int BiaSo{ get; set; }
    }
}
