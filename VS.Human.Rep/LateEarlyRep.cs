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
    public class LateEarlyRep : RepositoryBase<LateEarlyRequest>, ILateEarlyRep
    {
        public LateEarlyRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<BaseList> GetAll(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, int page, int limit)
        {
            if (page <= 0) page = 1;
            if (limit <= 0) limit = 20;
            var offset = (page - 1) * limit;

            var where = new List<string> { "ISNULL(r.Deleted, 0) = 0" };
            if (employeeId.HasValue)
            {
                where.Add("r.EmployeeId = @EmployeeId");
            }
            if (status.HasValue)
            {
                where.Add("r.Status = @Status");
            }
            if (fromDate.HasValue)
            {
                where.Add("r.RequestDate >= @FromDate");
            }
            if (toDate.HasValue)
            {
                where.Add("r.RequestDate <= @ToDate");
            }

            var whereSql = string.Join(" AND ", where);

            var countSql = $@"
                SELECT COUNT(1)
                FROM LateEarlyRequests r
                WHERE {whereSql}";

            var dataSql = $@"
                SELECT
                    r.Id,
                    r.EmployeeId,
                    e.FullName AS EmployeeName,
                    r.RequestType,
                    r.RequestDate,
                    r.StartTime,
                    r.EndTime,
                    r.Reason,
                    r.Status,
                    l.FullName AS LeadApproverName,
                    h.FullName AS HCNSApproverName,
                    b.FullName AS BGDApproverName,
                    a.FullName AS AdminApproverName,
                    ap.FullName AS ApproverName,
                    r.Comment,
                    r.ApproveAt,
                    r.CreateAt
                FROM LateEarlyRequests r
                INNER JOIN Employees e ON r.EmployeeId = e.Id
                LEFT JOIN Employees l ON r.LeadApproverId = l.Id
                LEFT JOIN Employees h ON r.HCNSApproverId = h.Id
                LEFT JOIN Employees b ON r.BGDApproverId = b.Id
                LEFT JOIN Employees a ON r.AdminApproverId = a.Id
                LEFT JOIN Employees ap ON r.ApproverId = ap.Id
                WHERE {whereSql}
                ORDER BY r.CreateAt DESC
                OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY";

            using var con = GetConnection();
            var total = await con.ExecuteScalarAsync<int>(countSql, new { EmployeeId = employeeId, Status = status, FromDate = fromDate, ToDate = toDate });
            var data = await con.QueryAsync<LateEarlyIndexModel>(dataSql, new { EmployeeId = employeeId, Status = status, FromDate = fromDate, ToDate = toDate, Offset = offset, Limit = limit });

            return new BaseList
            {
                Total = total,
                Data = data?.ToList() ?? new List<LateEarlyIndexModel>()
            };
        }

        public async Task<LateEarlyIndexModel?> GetById(int id)
        {
            var sql = @"
                SELECT
                    r.Id,
                    r.EmployeeId,
                    e.FullName AS EmployeeName,
                    r.RequestType,
                    r.RequestDate,
                    r.StartTime,
                    r.EndTime,
                    r.Reason,
                    r.Status,
                    r.LeadApproverId,
                    r.HCNSApproverId,
                    r.BGDApproverId,
                    r.AdminApproverId,
                    r.ApproverId,
                    r.Comment,
                    r.ApproveAt,
                    r.CreateAt
                FROM LateEarlyRequests r
                INNER JOIN Employees e ON r.EmployeeId = e.Id
                WHERE r.Id = @Id AND ISNULL(r.Deleted, 0) = 0";

            using var con = GetConnection();
            return await con.QueryFirstOrDefaultAsync<LateEarlyIndexModel>(sql, new { Id = id });
        }

        public async Task<int> Save(LateEarlyAddUpdate model, int userId, int status)
        {
            using var con = GetConnection();
            if (model.Id <= 0)
            {
                var sql = @"
                    INSERT INTO LateEarlyRequests
                        (EmployeeId, RequestType, RequestDate, StartTime, EndTime, Reason, Status, CreateAt, CreatedBy, UpdateAt, UpdatedBy, Deleted)
                    VALUES
                        (@EmployeeId, @RequestType, @RequestDate, @StartTime, @EndTime, @Reason, @Status, GETDATE(), @UserId, GETDATE(), @UserId, 0);
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                return await con.ExecuteScalarAsync<int>(sql, new
                {
                    model.EmployeeId,
                    model.RequestType,
                    model.RequestDate,
                    model.StartTime,
                    model.EndTime,
                    model.Reason,
                    Status = status,
                    UserId = userId
                });
            }

            var updateSql = @"
                UPDATE LateEarlyRequests
                SET RequestType = @RequestType,
                    RequestDate = @RequestDate,
                    StartTime = @StartTime,
                    EndTime = @EndTime,
                    Reason = @Reason,
                    UpdateAt = GETDATE(),
                    UpdatedBy = @UserId
                WHERE Id = @Id";

            await con.ExecuteAsync(updateSql, new
            {
                model.Id,
                model.RequestType,
                model.RequestDate,
                model.StartTime,
                model.EndTime,
                model.Reason,
                UserId = userId
            });

            return model.Id;
        }

        public async Task<bool> ApproveWorkflow(int id, string action, int approverId, string roleCode, string? comment)
        {
            using var con = GetConnection();
            using var tran = con.BeginTransaction();

            var current = await con.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT Status FROM LateEarlyRequests WHERE Id = @Id AND ISNULL(Deleted, 0) = 0",
                new { Id = id }, tran);

            if (current == null)
            {
                return false;
            }

            var currentStatus = (int)current.Status;
            var statusAfter = currentStatus;
            var actionText = action ?? string.Empty;

            if (actionText.Equals("Reject", StringComparison.OrdinalIgnoreCase))
            {
                statusAfter = 5;
                var rejectSql = @"
                    UPDATE LateEarlyRequests
                    SET Status = @Status,
                        Comment = @Comment,
                        ApproverId = @ApproverId,
                        ApproveAt = GETDATE(),
                        UpdateAt = GETDATE(),
                        UpdatedBy = @ApproverId
                    WHERE Id = @Id";

                await con.ExecuteAsync(rejectSql, new { Status = statusAfter, Comment = comment, ApproverId = approverId, Id = id }, tran);
            }
            else
            {
                if (roleCode == "1")
                {
                    statusAfter = 4;
                    var sql = @"
                        UPDATE LateEarlyRequests
                        SET Status = @Status,
                            AdminApproverId = @ApproverId,
                            AdminApproveAt = GETDATE(),
                            AdminComment = @Comment,
                            ApproverId = @ApproverId,
                            ApproveAt = GETDATE(),
                            UpdateAt = GETDATE(),
                            UpdatedBy = @ApproverId
                        WHERE Id = @Id";

                    await con.ExecuteAsync(sql, new { Status = statusAfter, ApproverId = approverId, Comment = comment, Id = id }, tran);
                }
                else if (currentStatus == 0 && (roleCode == "3" || roleCode == "TL"))
                {
                    statusAfter = 1;
                    var sql = @"
                        UPDATE LateEarlyRequests
                        SET Status = @Status,
                            LeadApproverId = @ApproverId,
                            LeadApproveAt = GETDATE(),
                            LeadComment = @Comment,
                            UpdateAt = GETDATE(),
                            UpdatedBy = @ApproverId
                        WHERE Id = @Id";

                    await con.ExecuteAsync(sql, new { Status = statusAfter, ApproverId = approverId, Comment = comment, Id = id }, tran);
                }
                else if (currentStatus == 1 && (roleCode == "2" || roleCode == "9" || roleCode == "HCNS"))
                {
                    statusAfter = 2;
                    var sql = @"
                        UPDATE LateEarlyRequests
                        SET Status = @Status,
                            HCNSApproverId = @ApproverId,
                            HCNSApproveAt = GETDATE(),
                            HCNSComment = @Comment,
                            UpdateAt = GETDATE(),
                            UpdatedBy = @ApproverId
                        WHERE Id = @Id";

                    await con.ExecuteAsync(sql, new { Status = statusAfter, ApproverId = approverId, Comment = comment, Id = id }, tran);
                }
                else if (currentStatus == 2 && (roleCode == "8" || roleCode == "BGD"))
                {
                    statusAfter = 3;
                    var sql = @"
                        UPDATE LateEarlyRequests
                        SET Status = @Status,
                            BGDApproverId = @ApproverId,
                            BGDApproveAt = GETDATE(),
                            BGDComment = @Comment,
                            UpdateAt = GETDATE(),
                            UpdatedBy = @ApproverId
                        WHERE Id = @Id";

                    await con.ExecuteAsync(sql, new { Status = statusAfter, ApproverId = approverId, Comment = comment, Id = id }, tran);
                }
                else if (currentStatus == 3 && (roleCode == "1" || roleCode == "ADMIN"))
                {
                    statusAfter = 4;
                    var sql = @"
                        UPDATE LateEarlyRequests
                        SET Status = @Status,
                            AdminApproverId = @ApproverId,
                            AdminApproveAt = GETDATE(),
                            AdminComment = @Comment,
                            ApproverId = @ApproverId,
                            ApproveAt = GETDATE(),
                            UpdateAt = GETDATE(),
                            UpdatedBy = @ApproverId
                        WHERE Id = @Id";

                    await con.ExecuteAsync(sql, new { Status = statusAfter, ApproverId = approverId, Comment = comment, Id = id }, tran);
                }
                else
                {
                    return false;
                }
            }

            var historySql = @"
                INSERT INTO LateEarlyHistory (RequestId, Action, ActionBy, ActionTime, Comment, StatusAfter, CreateAt, CreatedBy, UpdateAt, UpdatedBy, Deleted)
                VALUES (@RequestId, @Action, @ActionBy, GETDATE(), @Comment, @StatusAfter, GETDATE(), @ActionBy, GETDATE(), @ActionBy, 0)";

            await con.ExecuteAsync(historySql, new
            {
                RequestId = id,
                Action = actionText,
                ActionBy = approverId,
                Comment = comment ?? string.Empty,
                StatusAfter = statusAfter
            }, tran);

            tran.Commit();
            return true;
        }

        public async Task<List<LateEarlyHistory>> GetHistory(int requestId)
        {
            var sql = @"
                SELECT h.Id,
                       h.RequestId,
                       h.Action,
                       h.ActionBy,
                       e.FullName AS ActionByName,
                       h.ActionTime,
                       h.Comment,
                       h.StatusAfter
                FROM LateEarlyHistory h
                LEFT JOIN Employees e ON h.ActionBy = e.Id
                WHERE h.RequestId = @RequestId AND ISNULL(h.Deleted, 0) = 0
                ORDER BY h.ActionTime DESC";

            using var con = GetConnection();
            var result = await con.QueryAsync<LateEarlyHistory>(sql, new { RequestId = requestId });
            return result?.ToList() ?? new List<LateEarlyHistory>();
        }

        public async Task<bool> Delete(int id, int userId)
        {
            var sql = @"
                UPDATE LateEarlyRequests
                SET Deleted = 1,
                    UpdateAt = GETDATE(),
                    UpdatedBy = @UserId
                WHERE Id = @Id";

            return await ExecuteSQL(sql, new { Id = id, UserId = userId }, CommandType.Text);
        }
    }
}
