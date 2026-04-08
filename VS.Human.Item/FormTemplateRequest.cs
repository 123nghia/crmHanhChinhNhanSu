namespace VS.Human.Item
{
    public class FormTemplateRequest : BaseRequest
    {
        public FormTemplateRequest() : base()
        {
            From = null;
            To = null;
        }
    }

    public class FormTemplateIndexModel : BaseIndexModel
    {
        public string? Title { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
    }
}
