namespace crmHuman.DisplayModel
{
    public class DataMasterItem
    {
        public string? ShortName { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public int IsActive { get; set; }
        public string? Extra { get; set; }
        public int TypeData { get; set; }

        public int? ApplyFor { get; set; }
    }
}
