namespace VS.Human.Rep.Model
{
    public class MasterData : BaseModel
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public int? IsActive { get; set; }
        public string? Noted { get; set; }
        public int TypeData { get; set; }

        public string? Extra { get; set; }

        public int? ApplyFor { get; set; }
    }
}
