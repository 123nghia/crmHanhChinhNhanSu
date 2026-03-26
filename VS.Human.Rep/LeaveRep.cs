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

        public async Task<BaseList> GetAll(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, int page, int limit, int? userId = null)
        {
            var p = new DynamicParameters();
            p.Add("@EmployeeId", employeeId);
            p.Add("@Status", status);
            p.Add("@FromDate", fromDate);
            p.Add("@ToDate", toDate);
            p.Add("@Page", page);
            p.Add("@Limit", limit);
            p.Add("@UserId", userId);

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
                var levelStatus = roleCode switch
                {
                    "3" => 0,
                    "9" => 1,
                    "1" => 2,
                    "8" => 2,
                    _ => -1
                };

                var isCompanyWide = roleCode == "1" || roleCode == "8" || roleCode == "9";
                var isTeamScope = roleCode == "3";
                var isSelfScope = !isCompanyWide && !isTeamScope;

                var sql = @"
                    SELECT
                        (SELECT COUNT(*)
                         FROM LeaveRequests l
                         WHERE l.Deleted = 0
                           AND @levelStatus >= 0
                           AND l.Status = @levelStatus
                           AND (
                                @EmployeeId IS NULL
                                OR @IsCompanyWide = 1
                                OR (@IsTeamScope = 1 AND l.EmployeeId IN (SELECT id FROM getAllUserByUserId(@EmployeeId)))
                                OR (@IsSelfScope = 1 AND l.EmployeeId = @EmployeeId)
                           )
                        ) as PendingApproval,
                        (SELECT COUNT(*)
                         FROM LeaveRequests l
                         WHERE l.Deleted = 0
                           AND l.Status IN (3,4)
                           AND MONTH(l.CreateAt) = MONTH(GETDATE())
                           AND YEAR(l.CreateAt) = YEAR(GETDATE())
                           AND (
                                @EmployeeId IS NULL
                                OR @IsCompanyWide = 1
                                OR (@IsTeamScope = 1 AND l.EmployeeId IN (SELECT id FROM getAllUserByUserId(@EmployeeId)))
                                OR (@IsSelfScope = 1 AND l.EmployeeId = @EmployeeId)
                           )
                        ) as ApprovedMonth,
                        (SELECT AllowedLeaveDays - ISNULL(UsedLeaveDays, 0) FROM Employees WHERE Id = @empId) as RemainingLeave
                ";

                return await con.QueryFirstOrDefaultAsync<dynamic>(sql, new
                {
                    levelStatus,
                    EmployeeId = employeeId,
                    empId = employeeId,
                    IsCompanyWide = isCompanyWide,
                    IsTeamScope = isTeamScope,
                    IsSelfScope = isSelfScope
                });
            }
        }

        public async Task<LeaveBalanceIndexModel> GetEmployeeLeaveBalance(int employeeId)
        {
            using (var con = GetConnection())
            {
                var sql = @"
                    SELECT TOP 1
                        e.Id,
                        e.UserName,
                        e.FullName,
                        e.AllowedLeaveDays,
                        e.CarryOverLeaveDays,
                        e.UsedLeaveDays,
                        ISNULL(e.AllowedLeaveDays, 0) - ISNULL(e.UsedLeaveDays, 0) AS RemainingLeaveDays,
                        ISNULL(la.UsedAnnualLeaveDays, 0) AS UsedAnnualLeaveDays,
                        ISNULL(la.UsedSickLeaveDays, 0) AS UsedSickLeaveDays,
                        ISNULL(la.UsedPersonalLeaveDays, 0) AS UsedPersonalLeaveDays,
                        ISNULL(la.UsedMaternityLeaveDays, 0) AS UsedMaternityLeaveDays,
                        ISNULL(la.UsedUnpaidLeaveDays, 0) AS UsedUnpaidLeaveDays,
                        ISNULL(la.TotalApprovedLeaveDays, 0) AS TotalApprovedLeaveDays
                    FROM Employees e
                    LEFT JOIN (
                        SELECT
                            l.EmployeeId,
                            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NP' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedAnnualLeaveDays,
                            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NB' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedSickLeaveDays,
                            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NVR' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedPersonalLeaveDays,
                            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NTS' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedMaternityLeaveDays,
                            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NKL' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedUnpaidLeaveDays,
                            SUM(CASE WHEN l.Status IN (3,4) THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS TotalApprovedLeaveDays
                        FROM LeaveRequests l
                        WHERE l.Deleted = 0 AND l.EmployeeId = @EmployeeId
                        GROUP BY l.EmployeeId
                    ) la ON e.Id = la.EmployeeId
                    WHERE e.Id = @EmployeeId AND ISNULL(e.Deleted, 0) = 0;
                ";

                return await con.QueryFirstOrDefaultAsync<LeaveBalanceIndexModel>(sql, new { EmployeeId = employeeId });
            }
        }

        public async Task<bool> Delete(int id, int userId)
        {
            var p = new DynamicParameters();
            p.Add("@Id", id);
            p.Add("@del", 1);
            p.Add("@UpdateAt", DateTime.Now);
            p.Add("@UpdatedBy", userId);

            var sql = "UPDATE LeaveRequests SET Deleted = 1, UpdateAt = GETDATE(), UpdatedBy = @UpdatedBy WHERE Id = @Id";
            return await ExecuteSQL(sql, new { Id = id, UpdatedBy = userId }, CommandType.Text);
        }
    }
}
