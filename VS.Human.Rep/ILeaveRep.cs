using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VS.Human.Rep.Model;
using VS.Human.Item;

namespace VS.Human.Rep
{
    public interface ILeaveRep
    {
        Task<BaseList> GetAll(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, int page, int limit, int? userId = null);
        Task<LeaveIndexModel> GetById(int id);
        Task<int> Save(LeaveAddUpdate model, int userId);
        Task<bool> Approve(int id, int status, int approverId, string comment);
        Task<bool> ApproveWorkflow(int id, string action, int approverId, string roleCode, string comment);
        Task<List<LeaveHistory>> GetHistory(int leaveId);
        Task<dynamic> GetLeaveSummary(int? employeeId, string roleCode);
        Task<LeaveBalanceIndexModel> GetEmployeeLeaveBalance(int employeeId);
        Task<bool> Delete(int id, int userId);
        Task<bool> UpdateAttendanceSyncStatusAsync(
            int leaveId,
            string status,
            DateTime? rangeFrom,
            DateTime? rangeTo,
            DateTime? syncedAt,
            string? error,
            int attemptCount,
            int userId);
    }
}
