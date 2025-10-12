namespace VS.Human.Rep.Model
{
    public class BHXHItem : BaseModel
    {
        public bool? IsConfirmletter { get; set; }
        public string NumberCode { get; set; }

        public string DependentName { get; set; }
        public string Dependent { get; set; }
        public string Relid{ get; set; }

        public string UserName { get; set; }
    
        public string ChungTuThue { get; set; }
    }
}
