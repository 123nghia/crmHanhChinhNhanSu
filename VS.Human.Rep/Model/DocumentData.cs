namespace VS.Human.Rep.Model
{
    public class DocumentData : BaseModel
    {
        public int RelId { get; set; }
        public string RelCode { get; set; }
        public string Code{get;set;}
        public string DisplayText{get;set;}

        public string ValueFile {get;set;}

        public DocumentData()
        {
        
        }
    }
}
