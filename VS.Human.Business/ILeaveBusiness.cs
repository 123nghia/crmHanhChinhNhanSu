using System;
using System.Threading.Tasks;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface ILeaveBusiness
    {
        Task<BaseList> GetLeaveList(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, int page, int limit, int? userId = null);
        Task<LeaveIndexModel> GetLeaveById(int id);
        Task<int> CreateOrUpdateLeave(LeaveAddUpdate model, int userId);
        Task<bool> ApproveLeave(int id, int status, int approverId, string comment);
        Task<bool> ApproveWorkflow(int id, string action, int approverId, string roleCode, string comment);
        Task<LeaveApprovalAccessResult> GetApprovalAccessAsync(int leaveId, int userId, string? roleCode);
        Task<List<LeaveHistory>> GetLeaveHistory(int leaveId);
        Task<LeaveBalanceIndexModel> GetEmployeeLeaveBalance(int employeeId);
        Task<bool> DeleteLeave(int id, int userId);
    }
}
