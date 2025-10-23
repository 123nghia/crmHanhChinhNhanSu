namespace VS.Human.Rep.Model
{
    public class HDLD : BaseModel
    {
        public string UserName { get; set; } = string.Empty;
        public string NoAgree { get; set; } = string.Empty;
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public string CodeId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}
