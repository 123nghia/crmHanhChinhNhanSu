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
