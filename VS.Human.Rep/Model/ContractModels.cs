using System;

namespace VS.Human.Rep.Model
{
    public class Contract : BaseModel
    {
        public int EmployeeId { get; set; }
        public string? ContractTypeCode { get; set; }
        public string? ContractTypeName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string? FileUrl { get; set; }
        public string? Note { get; set; }
        public string? OriginalFileHash { get; set; }
        public bool IsHrSigned { get; set; }
        public DateTime? HrSignedAt { get; set; }
        public int? HrSignedBy { get; set; }
        public string? HrSignedByUserName { get; set; }
        public string? HrSignedByFullName { get; set; }
        public string? HrSignatureHash { get; set; }
        public string? HrFileHash { get; set; }
        public string? HrSignNote { get; set; }
        public string? HrSignMethod { get; set; }
        public string? HrSignedIpAddress { get; set; }
        public string? HrSignedUserAgent { get; set; }
        public string? HrSignedByUserNameSnapshot { get; set; }
        public string? HrSignedByFullNameSnapshot { get; set; }
        public bool IsSignedInternal { get; set; }
        public DateTime? SignedAt { get; set; }
        public int? SignedBy { get; set; }
        public string? SignedByUserName { get; set; }
        public string? SignedByFullName { get; set; }
        public string? SignatureHash { get; set; }
        public string? FileHash { get; set; }
        public string? SignNote { get; set; }
        public string? SignMethod { get; set; }
        public string? SignedIpAddress { get; set; }
        public string? SignedUserAgent { get; set; }
        public string? SignedFileArchivePath { get; set; }
        public string? SignatureImagePath { get; set; }
        public string? SignatureIntentText { get; set; }
        public string? SignedByUserNameSnapshot { get; set; }
        public string? SignedByFullNameSnapshot { get; set; }
        public DateTime? TermsAcceptedAt { get; set; }
        public string? FullName { get; set; }
        public string? UserName { get; set; }
    }

    public class ContractHistory : BaseModel
    {
        public int ContractId { get; set; }
        public int EmployeeId { get; set; }
        public string? Action { get; set; }
        public string? ContractTypeCode { get; set; }
        public string? ContractTypeName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string? FileUrl { get; set; }
        public string? Note { get; set; }
        public bool? IsHrSigned { get; set; }
        public DateTime? HrSignedAt { get; set; }
        public int? HrSignedBy { get; set; }
        public string? HrSignedByUserName { get; set; }
        public string? HrSignedByFullName { get; set; }
        public string? HrSignatureHash { get; set; }
        public string? HrFileHash { get; set; }
        public string? HrSignNote { get; set; }
        public string? HrSignMethod { get; set; }
        public string? HrSignedIpAddress { get; set; }
        public string? HrSignedUserAgent { get; set; }
        public string? HrSignedByUserNameSnapshot { get; set; }
        public string? HrSignedByFullNameSnapshot { get; set; }
        public bool? IsSignedInternal { get; set; }
        public DateTime? SignedAt { get; set; }
        public int? SignedBy { get; set; }
        public string? SignedByUserName { get; set; }
        public string? SignedByFullName { get; set; }
        public string? SignatureHash { get; set; }
        public string? FileHash { get; set; }
        public string? SignNote { get; set; }
        public string? SignMethod { get; set; }
        public string? SignedIpAddress { get; set; }
        public string? SignedUserAgent { get; set; }
        public string? SignedFileArchivePath { get; set; }
        public string? SignatureImagePath { get; set; }
        public string? SignatureIntentText { get; set; }
        public string? SignedByUserNameSnapshot { get; set; }
        public string? SignedByFullNameSnapshot { get; set; }
        public DateTime? TermsAcceptedAt { get; set; }
    }
}
