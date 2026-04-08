using System.Collections.Generic;

namespace VS.Human.Rep.Model
{
    public class MailGroupItem : BaseModel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IncludeAllCompanyEmails { get; set; }
        public bool IncludeAllPersonalEmails { get; set; }
        public bool IsBlocked { get; set; }
        public int RecipientCount { get; set; }
        public List<MailGroupRecipient> Recipients { get; set; } = new List<MailGroupRecipient>();
    }

    public class MailGroupRecipient : BaseModel
    {
        public int MailGroupId { get; set; }
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public bool IsBlocked { get; set; }
    }
}
