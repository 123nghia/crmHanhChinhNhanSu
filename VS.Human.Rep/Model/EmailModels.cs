namespace VS.Human.Rep.Model
{
    public class EmailSetting : BaseModel
    {
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public bool EnableSsl { get; set; }
        public string? SmtpUser { get; set; }
        public string? SmtpPassword { get; set; }
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }
    }

    public class EmailTemplate : BaseModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool CcManager { get; set; }
        public string? CcEmails { get; set; }
        public string? BccEmails { get; set; }
    }
}
