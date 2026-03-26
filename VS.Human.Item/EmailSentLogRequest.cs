namespace VS.Human.Item
{
    public class EmailSentLogRequest : BaseRequest
    {
        public string? RoleCode { get; set; }
        public string? TemplateCode { get; set; }
        public int? SendStatus { get; set; }

        public EmailSentLogRequest()
        {
            SendStatus = -1;
            Limit = 20;
        }
    }
}
