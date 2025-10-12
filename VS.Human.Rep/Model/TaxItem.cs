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

        public string UserName { get; set; }
        public string CodeId { get; set; }
        public string Number { get; set; }
        public string RegBHYT { get; set; }
        public int PageTax { get; set; } 
         public int BiaSo{ get; set; }
    }
}
