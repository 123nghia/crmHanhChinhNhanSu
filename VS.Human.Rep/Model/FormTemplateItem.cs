namespace VS.Human.Rep.Model
{
    public class FormTemplateItem : BaseModel
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public long? FileSize { get; set; }
        public string? AuthorName { get; set; }
    }
}
