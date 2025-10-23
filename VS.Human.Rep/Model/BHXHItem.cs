namespace VS.Human.Rep.Model
{
    public class BHXHItem : BaseModel
    {
        public bool? IsConfirmletter { get; set; }
        public string NumberCode { get; set; } = string.Empty;

        public string DependentName { get; set; } = string.Empty;
        public string Dependent { get; set; } = string.Empty;
        public string Relid{ get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;
    
        public string ChungTuThue { get; set; } = string.Empty;
    }
}
