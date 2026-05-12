using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Linq;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class ScheduleInterviewRep : RepositoryBase<ScheduleInterview>, IScheduleInterviewRep
    {
        public ScheduleInterviewRep(IConfiguration configuration)
            : base(configuration)
        {
            tableName = "ScheduleInterview";
            sqlGetALl = "sp_ScheduleInterview_getAll";
        }

        private async Task<bool> Update(ScheduleInterview item)
        {
            var parameter = new
            {
                item.Id,
                item.RelId,
                item.RelCode,
                item.Type,
                item.ScheduleDate,
                item.AddressInfo,
                item.Noted,
                item.Status,
                item.InterviewerId,
                item.InterviewMode,
                item.InterviewResult,
                item.UpdatedBy
            };
            return await ExecuteSQL("sp_ScheduleInterview_update", parameter);
        }

        private async Task<bool> Add(ScheduleInterview item)
        {
            var parameter = new
            {
                item.RelId,
                item.RelCode,
                item.AddressInfo,
                item.ScheduleDate,
                item.Noted,
                item.Type,
                item.Status,
                item.InterviewerId,
                item.InterviewMode,
                item.InterviewResult,
                item.CreatedBy
            };
            return await ExecuteSQL("sp_ScheduleInterview_insert", parameter);
        }

        public async Task<bool> AddOrUpdate(ScheduleInterview item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null && itemUpdate.Id > 0)
                {
                    return await Update(item);
                }
            }

            return await Add(item);
        }

        public async Task<int> SaveAndGetId(ScheduleInterview item)
        {
            if (item.Id > 0)
            {
                var updated = await Update(item);
                return updated ? item.Id : 0;
            }

            var added = await Add(item);
            if (!added)
            {
                return 0;
            }

            using var con = GetConnection();
            var sql = @"
                SELECT TOP 1 Id
                FROM ScheduleInterview
                WHERE ISNULL(Deleted, 0) = 0
                    AND RelId = @RelId
                    AND Type = @Type
                    AND ScheduleDate = @ScheduleDate
                    AND ISNULL(InterviewerId, 0) = ISNULL(@InterviewerId, 0)
                    AND ISNULL(InterviewMode, 0) = ISNULL(@InterviewMode, 0)
                    AND CreatedBy = @CreatedBy
                ORDER BY Id DESC";

            return await con.ExecuteScalarAsync<int>(sql, new
            {
                item.RelId,
                item.Type,
                item.ScheduleDate,
                item.InterviewerId,
                item.InterviewMode,
                item.CreatedBy
            });
        }

        public async Task<BaseList> GetAll(ScheduleInterviewRquest request)
        {
            var page = request.Page;
            var limit = request.Limit;
            ProcessInputPaging(ref page, ref limit, out var offset);

            const string sql = @"
DECLARE @EffectiveRoleCode varchar(20) = NULLIF(LTRIM(RTRIM(@RoleCodeInput)), '');
IF ((@EffectiveRoleCode IS NULL OR @EffectiveRoleCode = '') AND ISNULL(@UserId, 0) > 0)
BEGIN
    SELECT TOP 1 @EffectiveRoleCode = RoleCode
    FROM Employees
    WHERE Id = @UserId AND ISNULL(Deleted, 0) = 0;
END
IF ((@EffectiveRoleCode IS NULL OR @EffectiveRoleCode = '') AND ISNULL(@UserId, 0) > 0
    AND EXISTS (SELECT 1 FROM Candidate WHERE Id = @UserId AND ISNULL(Deleted, 0) = 0))
BEGIN
    SET @EffectiveRoleCode = 'CANDIDATE';
END
SET @EffectiveRoleCode = ISNULL(@EffectiveRoleCode, '');

;WITH ScheduleSource AS
(
    SELECT
        COUNT(1) OVER() AS TotalRecord,
        c.Name AS CandidateFullName,
        dbo.getDisplayMasterdata(c.Position) AS PositionText,
        d.*
    FROM ScheduleInterview d
    LEFT JOIN Candidate c ON d.RelId = c.Id AND ISNULL(c.Deleted, 0) = 0
    WHERE ISNULL(d.Deleted, 0) = 0
      AND (@RelId <= 0 OR d.RelId = @RelId)
      AND (@RelCode = '' OR ISNULL(d.RelCode, '') = @RelCode)
      AND (@Type < 0 OR ISNULL(d.Type, -1) = @Type)
      AND (@Status < 0 OR ISNULL(d.Status, -1) = @Status)
      AND (@InterviewerId <= 0 OR ISNULL(d.InterviewerId, 0) = @InterviewerId)
      AND (@InterviewMode < 0 OR ISNULL(d.InterviewMode, -1) = @InterviewMode)
      AND (@FromDate IS NULL OR d.ScheduleDate >= @FromDate)
      AND (@ToDate IS NULL OR d.ScheduleDate <= @ToDate)
      AND (@UpcomingOnly = 0 OR d.ScheduleDate >= GETDATE())
      AND (@Token = '' OR ISNULL(c.Name, '') LIKE N'%' + @Token + '%'
           OR ISNULL(c.Email, '') LIKE N'%' + @Token + '%'
           OR ISNULL(c.Phone, '') LIKE N'%' + @Token + '%')
      AND (
            ISNULL(@UserId, 0) <= 0
            OR @EffectiveRoleCode IN ('1', '8', '9')
            OR (@EffectiveRoleCode = 'CANDIDATE' AND d.RelId = @UserId)
            OR (@EffectiveRoleCode IN ('3', '6') AND (
                    ISNULL(d.CreatedBy, 0) = @UserId
                    OR ISNULL(d.CreatedBy, 0) IN (SELECT Id FROM dbo.getAllUserByUserId(@UserId))
                    OR ISNULL(d.InterviewerId, 0) = @UserId
                ))
            OR (@EffectiveRoleCode NOT IN ('1', '8', '9', '3', '6', 'CANDIDATE') AND (
                    ISNULL(d.CreatedBy, 0) = @UserId
                    OR ISNULL(d.InterviewerId, 0) = @UserId
                ))
          )
)
SELECT *
FROM ScheduleSource
ORDER BY
    CASE WHEN @OrderBy = 'schedule-asc' THEN ISNULL(ScheduleDate, CAST('9999-12-31' AS datetime)) END ASC,
    CASE WHEN @OrderBy = 'schedule-desc' THEN ISNULL(ScheduleDate, CAST('1900-01-01' AS datetime)) END DESC,
    UpdateAt DESC
OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;";

            using var con = GetConnection();
            var data = (await con.QueryAsync<ScheduleInterviewIndexModel>(sql, new
            {
                Token = (request.Token ?? string.Empty).Trim(),
                FromDate = request.From,
                ToDate = request.To,
                Type = request.Type ?? -1,
                Status = request.Status ?? -1,
                InterviewerId = request.InterviewerId ?? -1,
                InterviewMode = request.InterviewMode ?? -1,
                RelId = request.RelId ?? -1,
                RelCode = request.RelCode ?? string.Empty,
                UpcomingOnly = request.UpcomingOnly ? 1 : 0,
                OrderBy = (request.OrderBy ?? string.Empty).Trim().ToLowerInvariant(),
                UserId = request.UserId,
                RoleCodeInput = string.Empty,
                offset,
                limit
            }, commandType: CommandType.Text)).ToList();

            return new BaseList
            {
                Total = data.FirstOrDefault()?.TotalRecord ?? 0,
                Data = data
            };
        }

        public async Task<bool> HasViewAccess(int scheduleId, int userId, string? roleCode)
        {
            const string sql = @"
DECLARE @EffectiveRoleCode varchar(20) = NULLIF(LTRIM(RTRIM(@RoleCodeInput)), '');
IF ((@EffectiveRoleCode IS NULL OR @EffectiveRoleCode = '') AND @UserId > 0)
BEGIN
    SELECT TOP 1 @EffectiveRoleCode = RoleCode
    FROM Employees
    WHERE Id = @UserId AND ISNULL(Deleted, 0) = 0;
END
IF ((@EffectiveRoleCode IS NULL OR @EffectiveRoleCode = '') AND @UserId > 0
    AND EXISTS (SELECT 1 FROM Candidate WHERE Id = @UserId AND ISNULL(Deleted, 0) = 0))
BEGIN
    SET @EffectiveRoleCode = 'CANDIDATE';
END
SET @EffectiveRoleCode = ISNULL(@EffectiveRoleCode, '');

SELECT CAST(CASE WHEN EXISTS
(
    SELECT 1
    FROM ScheduleInterview d
    WHERE d.Id = @ScheduleId
      AND ISNULL(d.Deleted, 0) = 0
      AND @UserId > 0
      AND (
            @EffectiveRoleCode IN ('1', '8', '9')
            OR (@EffectiveRoleCode = 'CANDIDATE' AND d.RelId = @UserId)
            OR (@EffectiveRoleCode IN ('3', '6') AND (
                    ISNULL(d.CreatedBy, 0) = @UserId
                    OR ISNULL(d.CreatedBy, 0) IN (SELECT Id FROM dbo.getAllUserByUserId(@UserId))
                    OR ISNULL(d.InterviewerId, 0) = @UserId
                ))
            OR (@EffectiveRoleCode NOT IN ('1', '8', '9', '3', '6', 'CANDIDATE') AND (
                    ISNULL(d.CreatedBy, 0) = @UserId
                    OR ISNULL(d.InterviewerId, 0) = @UserId
                ))
          )
) THEN 1 ELSE 0 END AS bit);";

            return await ExecuteSQLScalar<bool>(sql, new
            {
                ScheduleId = scheduleId,
                UserId = userId,
                RoleCodeInput = roleCode
            });
        }

        public async Task<bool> HasManageAccess(int scheduleId, int userId, string? roleCode)
        {
            const string sql = @"
DECLARE @EffectiveRoleCode varchar(20) = NULLIF(LTRIM(RTRIM(@RoleCodeInput)), '');
IF ((@EffectiveRoleCode IS NULL OR @EffectiveRoleCode = '') AND @UserId > 0)
BEGIN
    SELECT TOP 1 @EffectiveRoleCode = RoleCode
    FROM Employees
    WHERE Id = @UserId AND ISNULL(Deleted, 0) = 0;
END
SET @EffectiveRoleCode = ISNULL(@EffectiveRoleCode, '');

SELECT CAST(CASE WHEN EXISTS
(
    SELECT 1
    FROM ScheduleInterview d
    WHERE d.Id = @ScheduleId
      AND ISNULL(d.Deleted, 0) = 0
      AND @UserId > 0
      AND (
            @EffectiveRoleCode IN ('1', '8', '9')
            OR (@EffectiveRoleCode IN ('3', '6') AND (
                    ISNULL(d.CreatedBy, 0) = @UserId
                    OR ISNULL(d.CreatedBy, 0) IN (SELECT Id FROM dbo.getAllUserByUserId(@UserId))
                ))
            OR (@EffectiveRoleCode NOT IN ('1', '8', '9', '3', '6', 'CANDIDATE') AND ISNULL(d.CreatedBy, 0) = @UserId)
          )
) THEN 1 ELSE 0 END AS bit);";

            return await ExecuteSQLScalar<bool>(sql, new
            {
                ScheduleId = scheduleId,
                UserId = userId,
                RoleCodeInput = roleCode
            });
        }

        public async Task<bool> Delete(int id)
        {
            return await DeleteBase(id, tableDelete: "ScheduleInterview");
        }

        public async Task<ScheduleInterview> GetById(int id)
        {
            const string sql = @"
SELECT TOP 1 *
FROM ScheduleInterview
WHERE Id = @id
  AND ISNULL(Deleted, 0) = 0;";

            return await ExecuteSQL2<ScheduleInterview>(sql, new { id });
        }
    }
}
