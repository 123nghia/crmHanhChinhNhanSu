using System.Collections.Generic;

namespace VS.Human.Rep.Model
{
    public class InternalNewsItem : BaseModel
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public bool IsSendMail { get; set; }
        public string? DirectRecipientEmails { get; set; }
        public string? AuthorName { get; set; }
        public List<int> MailGroupIds { get; set; } = new List<int>();
        public List<InternalNewsAttachment> Attachments { get; set; } = new List<InternalNewsAttachment>();
    }
}
