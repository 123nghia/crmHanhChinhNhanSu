using System;
using VS.Human.Item;

namespace VS.Human.Rep.Model
{
    public class LeaveRequest : BaseModel
    {
        public int EmployeeId { get; set; }
        public string LeaveTypeCode { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal? NumDays { get; set; }
        public string Reason { get; set; }
        public int Status { get; set; } // 0: Pending Lead, 1: Pending HCNS, 2: Pending BGD, 3: Approved, 4: Approved (Acting), 5: Rejected, 6: Cancelled
        
        public int? HandoverEmployeeId { get; set; }
        public string? Attachment { get; set; }

        public int? LeadApproverId { get; set; }
        public DateTime? LeadApproveAt { get; set; }
        public string? LeadComment { get; set; }

        public int? HCNSApproverId { get; set; }
        public DateTime? HCNSApproveAt { get; set; }
        public string? HCNSComment { get; set; }

        public int? BGDApproverId { get; set; }
        public DateTime? BGDApproveAt { get; set; }
        public string? BGDComment { get; set; }

        public bool IsActingApproval { get; set; }
        
        public int? ApproverId { get; set; } // Keep for backward compatibility or legacy
        public string? Comment { get; set; }
        public DateTime? ApproveAt { get; set; }
    }

    public class LeaveIndexModel : BaseIndexModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string LeaveTypeCode { get; set; }
        public string LeaveTypeName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal? NumDays { get; set; }
        public string Reason { get; set; }
        public int Status { get; set; }
        
        public string? HandoverEmployeeName { get; set; }

        public int? LeadApproverId { get; set; }
        
        public string? LeadApproverName { get; set; }
        public string? LeadComment { get; set; }

        public int? HCNSApproverId { get; set; }
        public string? HCNSApproverName { get; set; }
        public string? HCNSComment { get; set; }

        public int? BGDApproverId { get; set; }
        public string? BGDApproverName { get; set; }
        public string? BGDComment { get; set; }
        
        public bool IsActingApproval { get; set; }

        public int? ApproverId { get; set; }
        public string ApproverName { get; set; }
        public string Comment { get; set; }
        public DateTime? ApproveAt { get; set; }

        public string? AttendanceSyncStatus { get; set; }
        public DateTime? LastAttendanceSyncAt { get; set; }
        public string? LastAttendanceSyncError { get; set; }
        public DateTime? LastAttendanceSyncRangeFrom { get; set; }
        public DateTime? LastAttendanceSyncRangeTo { get; set; }
        public int? AttendanceSyncAttemptCount { get; set; }
        public DateTime CreateAt { get; set; }
    }

    public class LeaveApprovalAccessResult
    {
        public bool CanView { get; set; }
        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
        public bool CanActingApprove { get; set; }
        public int? AssignedApproverId { get; set; }
        public string? AssignedApproverRoleCode { get; set; }
        public string? AssignedApproverName { get; set; }
        public string? Message { get; set; }
    }

    public class LeaveAddUpdate
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string LeaveTypeCode { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal NumDays { get; set; }
        public string Reason { get; set; }
        public int? HandoverEmployeeId { get; set; }
    }

    public class LeaveApproveRequest
    {
        public int Id { get; set; }
        public int Status { get; set; }
        public string Comment { get; set; }
        public string Action { get; set; }
    }

    public class LeaveHistory : BaseModel
    {
        public int LeaveId { get; set; }
        public string Action { get; set; }
        public int ActionBy { get; set; }
        public string ActionByName { get; set; }
        public DateTime ActionTime { get; set; }
        public string Comment { get; set; }
        public int StatusAfter { get; set; }
    }
}
