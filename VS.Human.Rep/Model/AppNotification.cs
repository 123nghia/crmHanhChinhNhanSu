namespace VS.Human.Rep.Model
{
    public class AppNotification : BaseModel
    {
        public int ReceiverId { get; set; }
        public int? SenderId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Link { get; set; }
        public string? Type { get; set; }
        public bool IsRead { get; set; }
    }
}
