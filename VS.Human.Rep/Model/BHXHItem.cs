
namespace VS.Human.Rep.Model
{
    public class BHXHItem : BaseModel
    {
        public string NumberCode { get; set; } = string.Empty;
        public string Relid{ get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime? PITDate { get; set; }
        public DateTime? EffectedFrom { get; set; }
        public DateTime? StartMonth { get; set; }

        public string? RegBHYT {get;set;}

        public int? Number {get;set;}   

        public string? RegPageNumber {get;set;}
    }
}
