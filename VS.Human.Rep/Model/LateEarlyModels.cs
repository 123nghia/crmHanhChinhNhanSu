using System;
using VS.Human.Item;

namespace VS.Human.Rep.Model
{
    public class LateEarlyRequest : BaseModel
    {
        public int EmployeeId { get; set; }
        public string RequestType { get; set; } = string.Empty; // LATE/EARLY
        public DateTime RequestDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Status { get; set; }

        public int? LeadApproverId { get; set; }
        public DateTime? LeadApproveAt { get; set; }
        public string? LeadComment { get; set; }

        public int? HCNSApproverId { get; set; }
        public DateTime? HCNSApproveAt { get; set; }
        public string? HCNSComment { get; set; }

        public int? BGDApproverId { get; set; }
        public DateTime? BGDApproveAt { get; set; }
        public string? BGDComment { get; set; }

        public int? AdminApproverId { get; set; }
        public DateTime? AdminApproveAt { get; set; }
        public string? AdminComment { get; set; }

        public int? ApproverId { get; set; }
        public string? Comment { get; set; }
        public DateTime? ApproveAt { get; set; }
    }

    public class LateEarlyIndexModel : BaseIndexModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Status { get; set; }
        public string? LeadApproverName { get; set; }
        public string? HCNSApproverName { get; set; }
        public string? BGDApproverName { get; set; }
        public string? AdminApproverName { get; set; }
        public string? ApproverName { get; set; }
        public string? Comment { get; set; }
        public DateTime? ApproveAt { get; set; }
        public DateTime CreateAt { get; set; }
    }

    public class LateEarlyAddUpdate
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class LateEarlyApproveRequest
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty; // Agree/Reject
        public string? Comment { get; set; }
    }

    public class LateEarlyHistory : BaseModel
    {
        public int RequestId { get; set; }
        public string Action { get; set; } = string.Empty;
        public int ActionBy { get; set; }
        public string ActionByName { get; set; } = string.Empty;
        public DateTime ActionTime { get; set; }
        public string Comment { get; set; } = string.Empty;
        public int StatusAfter { get; set; }
    }
}
