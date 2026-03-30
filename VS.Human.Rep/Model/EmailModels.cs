using System;

namespace VS.Human.Rep.Model
{
    public static class EmailSenderTypes
    {
        public const string Hr = "HR";
        public const string Employee = "EMPLOYEE";
        public const string Candidate = "CANDIDATE";

        public static string Normalize(string? senderType)
        {
            if (string.Equals(senderType, Employee, StringComparison.OrdinalIgnoreCase))
            {
                return Employee;
            }

            if (string.Equals(senderType, Candidate, StringComparison.OrdinalIgnoreCase))
            {
                return Candidate;
            }

            return Hr;
        }

        public static string GetDisplayText(string? senderType)
        {
            return Normalize(senderType) switch
            {
                Employee => "Mail nhan vien",
                Candidate => "Mail ung vien",
                _ => "Mail nhan su"
            };
        }
    }

    public class EmailSetting : BaseModel
    {
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public bool EnableSsl { get; set; }
        public string? SmtpUser { get; set; }
        public string? SmtpPassword { get; set; }
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }
        public string? HrFromEmail { get; set; }
        public string? HrFromName { get; set; }
        public string? HrSignature { get; set; }
        public string? EmployeeFromEmail { get; set; }
        public string? EmployeeFromName { get; set; }
        public string? EmployeeSignature { get; set; }
        public string? CandidateFromEmail { get; set; }
        public string? CandidateFromName { get; set; }
        public string? CandidateSignature { get; set; }
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
        public string SenderType { get; set; } = EmailSenderTypes.Hr;
    }

    public class EmailSentLog : BaseModel
    {
        public int? TemplateId { get; set; }
        public string? TemplateCode { get; set; }
        public string? TemplateName { get; set; }
        public string SenderType { get; set; } = EmailSenderTypes.Hr;
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }
        public string? ToEmails { get; set; }
        public string? CcEmails { get; set; }
        public string? BccEmails { get; set; }
        public string? Subject { get; set; }
        public string? BodyHtml { get; set; }
        public bool SendSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int? TriggeredByUserId { get; set; }
        public string? TriggeredByUserName { get; set; }
        public string? TriggeredByFullName { get; set; }
        public string? TriggeredByDisplay { get; set; }
        public int? SenderEmployeeId { get; set; }
        public int? ManagerId { get; set; }
        public string? MessageId { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        public int? ParentEmailSentLogId { get; set; }
        public string? ParentMessageId { get; set; }
    }
}
