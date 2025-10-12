namespace VS.Human.Rep.Model
{
    public class HDLD : BaseModel
    {
        public string UserName { get; set; }
        public string NoAgree { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public string CodeId { get; set; }
        public string UserId { get; set; }
    }
}
