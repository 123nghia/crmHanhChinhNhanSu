namespace VS.Human.Rep.Model
{
    public class DocumentData : BaseModel
    {
        public int? RelId { get; set; }
        public string? RelCode { get; set; }
        public string? Code { get; set; }
        public string? DisplayText { get; set; }
        public string? ValueFile { get; set; }
        public int? ParentId { get; set; }
        public bool IsFolder { get; set; }
        public int AccessLevel { get; set; }
        public string? ShareToken { get; set; }
        public int dataType { get; set; }
        public bool IsSignedInternal { get; set; }
        public DateTime? SignedAt { get; set; }
        public int? SignedBy { get; set; }
        public string? SignedByUserName { get; set; }
        public string? SignedByFullName { get; set; }
        public string? SignedByUserNameSnapshot { get; set; }
        public string? SignedByFullNameSnapshot { get; set; }
        public string? SignedIpAddress { get; set; }
        public string? SignedUserAgent { get; set; }
        public string? SignedFileArchivePath { get; set; }
        public string? SignatureImagePath { get; set; }
        public string? SignatureHash { get; set; }
        public string? FileHash { get; set; }
        public string? SignNote { get; set; }
        public string? SignMethod { get; set; }
        public string? SignatureIntentText { get; set; }
        public bool IsSignatureRequested { get; set; }
        public DateTime? SignatureRequestedAt { get; set; }
        public int? SignatureRequestedBy { get; set; }
        public string? SignatureRequestedByUserName { get; set; }
        public string? SignatureRequestedByFullName { get; set; }
        public DateTime? TermsAcceptedAt { get; set; }

        public DocumentData()
        {
        }
    }
}
