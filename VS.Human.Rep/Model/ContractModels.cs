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
    }
}
