namespace VS.Human.Rep.Model
{
    public class UserThemeSetting : BaseModel
    {
        public int UserId { get; set; }
        public string? PrimaryColor { get; set; }
        public string? ButtonColor { get; set; }
        public string? BackgroundColor { get; set; }
    }
}
