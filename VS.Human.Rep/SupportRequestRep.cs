using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Linq;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class SupportRequestRep : RepositoryBase<SupportRequestItem>, ISupportRequestRep
    {
        public SupportRequestRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<int> Create(SupportRequestItem item, int userId)
        {
            using var con = GetConnection();
            using var tran = con.BeginTransaction();

            var sql = @"
                INSERT INTO SupportRequests
                (
                    RequesterId,
                    Title,
                    Content,
                    TargetDepartmentCode,
                    AssignedToId,
                    Status,
                    ProcessorComment,
                    AssignedAt,
                    CompletedAt,
                    CreatedBy,
                    UpdatedBy,
                    CreateAt,
                    UpdateAt,
                    Deleted,
                    IsActive
                )
                VALUES
                (
                    @RequesterId,
                    @Title,
                    @Content,
                    @TargetDepartmentCode,
                    @AssignedToId,
                    @Status,
                    @ProcessorComment,
                    @AssignedAt,
                    @CompletedAt,
                    @UserId,
                    @UserId,
                    GETDATE(),
                    GETDATE(),
                    0,
                    1
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var requestId = await con.ExecuteScalarAsync<int>(sql, new
            {
                item.RequesterId,
                item.Title,
                item.Content,
                item.TargetDepartmentCode,
                item.AssignedToId,
                item.Status,
                item.ProcessorComment,
                item.AssignedAt,
                item.CompletedAt,
                UserId = userId
            }, tran);

            if (item.Attachments != null)
            {
                foreach (var attachment in item.Attachments.Where(x => x != null && !string.IsNullOrWhiteSpace(x.FilePath)))
                {
                    await con.ExecuteAsync(@"
                        INSERT INTO SupportRequestAttachments
                        (
                            RequestId,
                            FileName,
                            FilePath,
                            FileSize,
                            CreatedBy,
                            UpdatedBy,
                            CreateAt,
                            UpdateAt,
                            Deleted,
                            IsActive
                        )
                        VALUES
                        (
                            @RequestId,
                            @FileName,
                            @FilePath,
                            @FileSize,
                            @UserId,
                            @UserId,
                            GETDATE(),
                            GETDATE(),
                            0,
                            1
                        )", new
                    {
                        RequestId = requestId,
                        attachment.FileName,
                        attachment.FilePath,
                        attachment.FileSize,
                        UserId = userId
                    }, tran);
                }
            }

            await con.ExecuteAsync(@"
                INSERT INTO SupportRequestHistories
                (
                    RequestId,
                    Action,
                    ActionBy,
                    ActionTime,
                    Comment,
                    StatusAfter,
                    CreatedBy,
                    UpdatedBy,
                    CreateAt,
                    UpdateAt,
                    Deleted,
                    IsActive
                )
                VALUES
                (
                    @RequestId,
                    'Create',
                    @UserId,
                    GETDATE(),
                    @Comment,
                    @StatusAfter,
                    @UserId,
                    @UserId,
                    GETDATE(),
                    GETDATE(),
                    0,
                    1
                )", new
            {
                RequestId = requestId,
                UserId = userId,
                Comment = item.ProcessorComment ?? string.Empty,
                StatusAfter = item.Status
            }, tran);

            tran.Commit();
            return requestId;
        }

        public async Task<BaseList> GetAll(SupportRequestListRequest request)
        {
            var page = request.Page <= 0 ? 1 : request.Page;
            var limit = request.Limit <= 0 ? 20 : request.Limit;
            var offset = (page - 1) * limit;

            var where = new List<string> { "ISNULL(r.Deleted, 0) = 0" };
            if (!string.IsNullOrWhiteSpace(request.Token))
            {
                where.Add("(r.Title LIKE N'%' + @Token + '%' OR r.Content LIKE N'%' + @Token + '%' OR req.FullName LIKE N'%' + @Token + '%')");
            }
            if (request.Status.HasValue && request.Status.Value >= 0)
            {
                where.Add("r.Status = @Status");
            }
            if (!string.IsNullOrWhiteSpace(request.TargetDepartmentCode) && request.TargetDepartmentCode != "-1")
            {
                where.Add("r.TargetDepartmentCode = @TargetDepartmentCode");
            }
            if (request.IsProcessingView)
            {
                where.Add("(@UserId > 0 AND (@IsAdmin = 1 OR r.AssignedToId = @UserId))");
            }
            else
            {
                where.Add("(@UserId > 0 AND (@IsAdmin = 1 OR r.RequesterId = @UserId))");
            }

            var whereSql = string.Join(" AND ", where);
            var sql = $@"
                SELECT COUNT(1) OVER() AS TotalRecord,
                       r.Id,
                       r.RequesterId,
                       req.FullName AS RequesterName,
                       req.DepartmentCode AS RequesterDepartmentCode,
                       dbo.getDisplayMasterData(req.DepartmentCode) AS RequesterDepartmentText,
                       r.Title,
                       r.Content,
                       r.TargetDepartmentCode,
                       dbo.getDisplayMasterData(r.TargetDepartmentCode) AS TargetDepartmentText,
                       r.AssignedToId,
                       assignee.FullName AS AssignedToName,
                       r.Status,
                       r.ProcessorComment,
                       r.AssignedAt,
                       r.CompletedAt,
                       r.CreateAt,
                       CAST(CASE WHEN @IsAdmin = 1 OR r.AssignedToId = @UserId THEN 1 ELSE 0 END AS bit) AS CanProcess
                FROM SupportRequests r
                INNER JOIN Employees req ON req.Id = r.RequesterId
                LEFT JOIN Employees assignee ON assignee.Id = r.AssignedToId
                WHERE {whereSql}
                ORDER BY r.CreateAt DESC
                OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;";

            using var con = GetConnection();
            var data = (await con.QueryAsync<SupportRequestIndexModel>(sql, new
            {
                request.Token,
                request.Status,
                request.TargetDepartmentCode,
                request.UserId,
                IsAdmin = string.Equals(request.RoleCode, "1", StringComparison.OrdinalIgnoreCase),
                Offset = offset,
                Limit = limit
            })).ToList();

            if (data.Count > 0)
            {
                var requestIds = data.Select(x => x.Id).ToList();
                var attachments = (await con.QueryAsync<SupportRequestAttachment>(@"
                    SELECT Id, RequestId, FileName, FilePath, FileSize, CreateAt
                    FROM SupportRequestAttachments
                    WHERE RequestId IN @RequestIds
                      AND ISNULL(Deleted, 0) = 0
                    ORDER BY Id DESC", new { RequestIds = requestIds })).ToList();

                var lookup = attachments
                    .GroupBy(x => x.RequestId)
                    .ToDictionary(x => x.Key, x => x.ToList());

                foreach (var item in data)
                {
                    if (lookup.TryGetValue(item.Id, out var files))
                    {
                        item.Attachments = files;
                    }
                }
            }

            return new BaseList
            {
                Total = data.FirstOrDefault()?.TotalRecord ?? 0,
                Data = data
            };
        }

        public async Task<SupportRequestIndexModel?> GetById(int id)
        {
            using var con = GetConnection();
            var sql = @"
                SELECT TOP 1
                       r.Id,
                       r.RequesterId,
                       req.FullName AS RequesterName,
                       req.DepartmentCode AS RequesterDepartmentCode,
                       dbo.getDisplayMasterData(req.DepartmentCode) AS RequesterDepartmentText,
                       r.Title,
                       r.Content,
                       r.TargetDepartmentCode,
                       dbo.getDisplayMasterData(r.TargetDepartmentCode) AS TargetDepartmentText,
                       r.AssignedToId,
                       assignee.FullName AS AssignedToName,
                       r.Status,
                       r.ProcessorComment,
                       r.AssignedAt,
                       r.CompletedAt,
                       r.CreateAt
                FROM SupportRequests r
                INNER JOIN Employees req ON req.Id = r.RequesterId
                LEFT JOIN Employees assignee ON assignee.Id = r.AssignedToId
                WHERE r.Id = @Id AND ISNULL(r.Deleted, 0) = 0";

            var item = await con.QueryFirstOrDefaultAsync<SupportRequestIndexModel>(sql, new { Id = id });
            if (item == null)
            {
                return null;
            }

            var attachments = await con.QueryAsync<SupportRequestAttachment>(@"
                SELECT Id, RequestId, FileName, FilePath, FileSize, CreateAt
                FROM SupportRequestAttachments
                WHERE RequestId = @RequestId AND ISNULL(Deleted, 0) = 0
                ORDER BY Id DESC", new { RequestId = id });

            item.Attachments = attachments?.ToList() ?? new List<SupportRequestAttachment>();
            return item;
        }

        public async Task<List<SupportRequestHistory>> GetHistory(int requestId)
        {
            using var con = GetConnection();
            var sql = @"
                SELECT h.Id,
                       h.RequestId,
                       h.Action,
                       h.ActionBy,
                       e.FullName AS ActionByName,
                       h.ActionTime,
                       h.Comment,
                       h.StatusAfter
                FROM SupportRequestHistories h
                LEFT JOIN Employees e ON e.Id = h.ActionBy
                WHERE h.RequestId = @RequestId AND ISNULL(h.Deleted, 0) = 0
                ORDER BY h.ActionTime DESC, h.Id DESC";

            var result = await con.QueryAsync<SupportRequestHistory>(sql, new { RequestId = requestId });
            return result?.ToList() ?? new List<SupportRequestHistory>();
        }

        public async Task<bool> UpdateStatus(int id, int status, string? comment, int updatedBy)
        {
            using var con = GetConnection();
            using var tran = con.BeginTransaction();

            var request = await con.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT Id FROM SupportRequests WHERE Id = @Id AND ISNULL(Deleted, 0) = 0",
                new { Id = id }, tran);

            if (request == null)
            {
                return false;
            }

            var completedAt = status == 2 || status == 3 ? DateTime.Now : (DateTime?)null;
            await con.ExecuteAsync(@"
                UPDATE SupportRequests
                SET Status = @Status,
                    ProcessorComment = @Comment,
                    CompletedAt = @CompletedAt,
                    UpdatedBy = @UpdatedBy,
                    UpdateAt = GETDATE()
                WHERE Id = @Id", new
            {
                Id = id,
                Status = status,
                Comment = comment,
                CompletedAt = completedAt,
                UpdatedBy = updatedBy
            }, tran);

            await con.ExecuteAsync(@"
                INSERT INTO SupportRequestHistories
                (
                    RequestId,
                    Action,
                    ActionBy,
                    ActionTime,
                    Comment,
                    StatusAfter,
                    CreatedBy,
                    UpdatedBy,
                    CreateAt,
                    UpdateAt,
                    Deleted,
                    IsActive
                )
                VALUES
                (
                    @RequestId,
                    @Action,
                    @ActionBy,
                    GETDATE(),
                    @Comment,
                    @StatusAfter,
                    @ActionBy,
                    @ActionBy,
                    GETDATE(),
                    GETDATE(),
                    0,
                    1
                )", new
            {
                RequestId = id,
                Action = GetActionName(status),
                ActionBy = updatedBy,
                Comment = comment ?? string.Empty,
                StatusAfter = status
            }, tran);

            tran.Commit();
            return true;
        }

        public async Task<bool> Delete(int id, int userId)
        {
            return await ExecuteSQL(@"
                UPDATE SupportRequests
                SET Deleted = 1,
                    UpdatedBy = @UserId,
                    UpdateAt = GETDATE()
                WHERE Id = @Id AND ISNULL(Deleted, 0) = 0", new
            {
                Id = id,
                UserId = userId
            }, CommandType.Text);
        }

        private static string GetActionName(int status)
        {
            return status switch
            {
                1 => "InProcess",
                2 => "Done",
                3 => "Cancel",
                _ => "Update"
            };
        }
    }
}
