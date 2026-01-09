using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class LeaveRep : RepositoryBase<Model.LeaveRequest>, ILeaveRep
    {
        public LeaveRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<BaseList> GetAll(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, int page, int limit)
        {
            var p = new DynamicParameters();
            p.Add("@EmployeeId", employeeId);
            p.Add("@Status", status);
            p.Add("@FromDate", fromDate);
            p.Add("@ToDate", toDate);
            p.Add("@Page", page);
            p.Add("@Limit", limit);

            return await GetBaseAll<LeaveIndexModel>(new BaseRequest { Page = page, Limit = limit }, p, "sp_Leave_GetAll");
        }

        public async Task<LeaveIndexModel> GetById(int id)
        {
            var p = new DynamicParameters();
            p.Add("@Id", id);
            return await GetFirstRecordBySql<LeaveIndexModel>("sp_Leave_GetById", p);
        }

        public async Task<int> Save(LeaveAddUpdate model, int userId)
        {
            var p = new DynamicParameters();
            p.Add("@Id", model.Id);
            p.Add("@EmployeeId", model.EmployeeId);
            p.Add("@LeaveTypeCode", model.LeaveTypeCode);
            p.Add("@FromDate", model.FromDate);
            p.Add("@ToDate", model.ToDate);
            p.Add("@NumDays", model.NumDays);
            p.Add("@Reason", model.Reason);
            p.Add("@HandoverEmployeeId", model.HandoverEmployeeId);
            p.Add("@UserId", userId);

            return await ExecuteSQLScalar<int>("sp_Leave_Save", p);
        }

        public async Task<bool> Approve(int id, int status, int approverId, string comment)
        {
            var p = new DynamicParameters();
            p.Add("@Id", id);
            p.Add("@Status", status);
            p.Add("@ApproverId", approverId);
            p.Add("@Comment", comment);

            return await ExecuteSQL("sp_Leave_Approve", p);
        }

        public async Task<bool> ApproveWorkflow(int id, string action, int approverId, string roleCode, string comment)
        {
            var p = new DynamicParameters();
            p.Add("@Id", id);
            p.Add("@Action", action);
            p.Add("@ApproverId", approverId);
            p.Add("@RoleCode", roleCode);
            p.Add("@Comment", comment);

            return await ExecuteSQL("sp_Leave_Approve_Workflow", p);
        }

        public async Task<List<LeaveHistory>> GetHistory(int leaveId)
        {
            var p = new DynamicParameters();
            p.Add("@LeaveId", leaveId);

            return await GetDataList<LeaveHistory>("sp_Leave_GetHistory", p);
        }

        public async Task<dynamic> GetLeaveSummary(int? employeeId, string roleCode)
        {
            using (var con = GetConnection())
            {
                // Logic based on role:
                // TL (3): Sees status 0
                // HCNS (2): Sees status 1
                // BGĐ (1): Sees status 2
                int levelStatus = -1;
                if (roleCode == "3") levelStatus = 0;
                else if (roleCode == "2") levelStatus = 1;
                else if (roleCode == "1") levelStatus = 2;

                var sql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM LeaveRequests WHERE Deleted = 0 AND Status = @levelStatus) as PendingApproval,
                        (SELECT COUNT(*) FROM LeaveRequests WHERE Deleted = 0 AND Status IN (3,4) AND MONTH(CreateAt) = MONTH(GETDATE())) as ApprovedMonth,
                        (SELECT AllowedLeaveDays - ISNULL(UsedLeaveDays, 0) FROM Employees WHERE Id = @empId) as RemainingLeave
                ";

                return await con.QueryFirstOrDefaultAsync<dynamic>(sql, new { levelStatus, empId = employeeId });
            }
        }

        public new async Task<bool> Delete(int id, int userId)
        {
            var p = new DynamicParameters();
            p.Add("@Id", id);
            p.Add("@del", 1);
            p.Add("@UpdateAt", DateTime.Now);
            p.Add("@UpdatedBy", userId);

            // Simple update for soft delete if generic DeleteBase doesn't cover all fields needed
            var sql = "UPDATE LeaveRequests SET Deleted = 1, UpdateAt = GETDATE(), UpdatedBy = @UpdatedBy WHERE Id = @Id";
            return await ExecuteSQL(sql, new { Id = id, UpdatedBy = userId }, CommandType.Text);
        }
    }
}
