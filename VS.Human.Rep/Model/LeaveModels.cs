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
        public int Status { get; set; } // 0: Pending, 1: Approved, 2: Rejected
        public int? ApproverId { get; set; }
        public string Comment { get; set; }
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
        public int? ApproverId { get; set; }
        public string ApproverName { get; set; }
        public string Comment { get; set; }
        public DateTime? ApproveAt { get; set; }
        public DateTime CreateAt { get; set; }
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
    }

    public class LeaveApproveRequest
    {
        public int Id { get; set; }
        public int Status { get; set; }
        public string Comment { get; set; }
    }
}
