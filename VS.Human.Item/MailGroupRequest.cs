namespace VS.Human.Item
{
    public class MailGroupRequest : BaseRequest
    {
        public MailGroupRequest() : base()
        {
            From = null;
            To = null;
        }
    }

    public class MailGroupIndexModel : BaseIndexModel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IncludeAllCompanyEmails { get; set; }
        public bool IncludeAllPersonalEmails { get; set; }
        public bool IsBlocked { get; set; }
        public int RecipientCount { get; set; }
    }
}
