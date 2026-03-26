using Microsoft.Extensions.Configuration;
using Dapper;
using System.Data;
using System.Linq;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class CandidateRep : RepositoryBase<Candidate>, ICandidateRep
    {

        public CandidateRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "Candidate";
        }

        private async Task<bool> Update(Candidate item)
        {
            var parameter = new
            {
                item.Id,
                item.Name,
                item.Dob,
                item.Email,

                item.CVLink,
                item.Source,
                item.Phone,
                item.Status,
                item.IsActive,
                item.UpdatedBy,
                item.ShortDes,
                item.DepartmentId,
                item.Position,
                item.StatusHuman,
                item.ManagerId,
                item.Referrer,
                item.Noted,
                item.NationalId,
                item.Address,
                item.UserName,
                item.Pass,
                item.IsEmployee,
                item.EmployeeId,
                item.ExpectedOnboardDate
            };
            return await this.ExecuteSQL("sp_candidate_update", parameter);
        }
        private async Task<bool> Add(Candidate item)
        {

            item.Status = 91;

            var parameter = new
            {
                item.Name,
                item.ShortDes,
                item.Noted,
                item.Position,
                item.DepartmentId,
                item.Phone,
                item.Email,
                item.Dob,
                item.Source,
                item.ManagerId,
                item.Status,
                item.CreatedBy,
                item.CVLink,
                item.IsActive,
                item.NationalId,
                item.Address,
                item.UserName,
                item.Pass,
                item.ExpectedOnboardDate
            };
            return await this.ExecuteSQL("sp_candidate_insert", parameter);
        }

        public async Task<bool> AddOrUpdate(Candidate item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null)
                {
                    itemUpdate.Name = item.Name;
                    itemUpdate.CVLink = item.CVLink;
                    itemUpdate.Status = item.Status;
                    itemUpdate.ShortDes = item.ShortDes;
                    itemUpdate.Noted = item.Noted;
                    itemUpdate.Status = item.Status;
                    itemUpdate.Email = item.Email;
                    itemUpdate.Source = item.Source;
                    itemUpdate.ManagerId = item.ManagerId;
                    itemUpdate.Dob = item.Dob;
                    itemUpdate.IsActive = item.IsActive;
                    itemUpdate.Phone = item.Phone;
                    itemUpdate.UpdatedBy = item.UpdatedBy;
                    itemUpdate.StatusHuman = item.StatusHuman;
                    itemUpdate.DepartmentId = item.DepartmentId;
                    itemUpdate.Position = item.Position;
                    itemUpdate.UpdatedBy = item.UpdatedBy;
                    itemUpdate.Referrer = item.Referrer;
                    itemUpdate.NationalId = item.NationalId;
                    itemUpdate.Address = item.Address;
                    if (!string.IsNullOrWhiteSpace(item.UserName))
                    {
                        itemUpdate.UserName = item.UserName;
                    }
                    if (!string.IsNullOrWhiteSpace(item.Pass))
                    {
                        itemUpdate.Pass = item.Pass;
                    }
                    if (item.IsEmployee.HasValue)
                    {
                        itemUpdate.IsEmployee = item.IsEmployee;
                    }
                    if (item.EmployeeId.HasValue)
                    {
                        itemUpdate.EmployeeId = item.EmployeeId;
                    }
                    itemUpdate.ExpectedOnboardDate = item.ExpectedOnboardDate;

                    return await Update(itemUpdate);
                }
            }
            return await Add(item);


        }

        public async Task<BaseList> GetAll(CandidateRequest request)
        {
            var page = request.Page;
            var limit = request.Limit;
            ProcessInputPaging(ref page, ref limit, out var offset);

            if (request.LoadAll == 1)
            {
                page = 1;
                limit = 10000;
                offset = 0;
            }

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

;WITH CandidateSource AS
(
    SELECT
        COUNT(1) OVER() AS TotalRecord,
        dbo.getUserName(d.CreatedBy) AS AuthorName,
        dbo.getFullNameSorce(d.CreatedBy) AS SourceName,
        dbo.getDisplayMasterdata(d.Position) AS PostionName,
        dbo.getDisplayMasterdata(d.Status) AS StatusName,
        dbo.getFullName(d.ManagerId) AS ManagerName,
        dbo.getDisplayMasterdata(d.DepartmentId) AS DepartmentName,
        d.*
    FROM Candidate d
    WHERE ISNULL(d.Deleted, 0) = 0
      AND (@Token = '' OR ISNULL(d.Code, '') LIKE N'%' + @Token + '%'
           OR ISNULL(d.Name, '') LIKE N'%' + @Token + '%'
           OR ISNULL(d.Email, '') LIKE N'%' + @Token + '%'
           OR ISNULL(d.Phone, '') LIKE N'%' + @Token + '%'
           OR ISNULL(d.UserName, '') LIKE N'%' + @Token + '%')
      AND (@CandidateStatus <= 0 OR ISNULL(d.Status, 0) = @CandidateStatus)
      AND (@DocumentStatus <= 0 OR ISNULL(d.StatusHuman, 0) = @DocumentStatus)
      AND (@ManagerId <= 0 OR ISNULL(d.ManagerId, 0) = @ManagerId)
      AND (@IsEmployee = 0 OR ISNULL(d.IsEmployee, 0) = 1)
      AND (@FromDate IS NULL OR d.CreateAt >= @FromDate)
      AND (@ToDate IS NULL OR d.CreateAt <= @ToDate)
      AND (
            ISNULL(@UserId, 0) <= 0
            OR @EffectiveRoleCode IN ('1', '8', '9')
            OR (@EffectiveRoleCode = 'CANDIDATE' AND d.Id = @UserId)
            OR (@EffectiveRoleCode IN ('3', '6') AND (
                    ISNULL(d.CreatedBy, 0) = @UserId
                    OR ISNULL(d.CreatedBy, 0) IN (SELECT Id FROM dbo.getAllUserByUserId(@UserId))
                    OR EXISTS (
                        SELECT 1
                        FROM ScheduleInterview si
                        WHERE ISNULL(si.Deleted, 0) = 0
                          AND si.RelId = d.Id
                          AND ISNULL(si.InterviewerId, 0) = @UserId
                    )
                ))
            OR (@EffectiveRoleCode NOT IN ('1', '8', '9', '3', '6', 'CANDIDATE') AND (
                    ISNULL(d.CreatedBy, 0) = @UserId
                    OR EXISTS (
                        SELECT 1
                        FROM ScheduleInterview si
                        WHERE ISNULL(si.Deleted, 0) = 0
                          AND si.RelId = d.Id
                          AND ISNULL(si.InterviewerId, 0) = @UserId
                    )
                ))
          )
)
SELECT *
FROM CandidateSource
ORDER BY UpdateAt DESC
OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;";

            using var con = GetConnection();
            var data = (await con.QueryAsync<CandidateIndexModel>(sql, new
            {
                Token = (request.Token ?? string.Empty).Trim(),
                UserId = request.UserId,
                RoleCodeInput = request.RoleCode,
                CandidateStatus = request.CandidateStatus,
                DocumentStatus = request.DocumentStatus,
                ManagerId = request.ManagerId,
                IsEmployee = request.IsEmployee ? 1 : 0,
                FromDate = request.From,
                ToDate = request.To,
                offset,
                limit
            }, commandType: CommandType.Text)).ToList();

            return new BaseList
            {
                Total = data.FirstOrDefault()?.TotalRecord ?? 0,
                Data = data
            };
        }

        public async Task<BaseList> GetAlLCandidateOfMember(CandidateRequest request)
        {
            var sqlText = "sp_candidateOfMember_getAll";
            if (request.RoleCode == "4")
            {
                sqlText = "sp_candidateOfMember_getAllMarketting";
            }
            var result = await GetBaseAll<CandidateIndexModel>(request,
            new
            {
                request.Token,
                request.UserId,
                request.Limit,
                request.LoadAll,
                request.GroupId,
                request.MemberId,

                request.Page

            }, sqlText);
            return result;
        }
        public async Task<bool> AddCandidateWidthOrder(dynamic request)
        {
            var sqlText = "sp_AddCandidateAndOrder";

            return await this.ExecuteSQL(sqlText, request);
        }

        public async Task<Candidate> Login(string userName, string password)
        {
            var modelCheck = new
            {
                userName,
                password
            };
            var result = await ExecuteSQL<Candidate>("sp_candidate_login", modelCheck);
            return result;
        }

        public async Task<bool> ChangePassword(string password, int id)
        {
            var parameter = new
            {
                password,
                id
            };

            return await this.ExecuteSQL("sp_candidate_changePassword", parameter);
        }

        public async Task<Candidate> GetByUserName(string userName)
        {
            var parameter = new { userName };
            var sql = "SELECT TOP 1 * FROM Candidate WHERE UserName = @userName AND ISNULL(Deleted,0)=0";
            return await ExecuteSQL<Candidate>(sql, parameter);
        }

        public async Task<Candidate?> FindDuplicateForCreate(string? name, string? phone, string? email, int? position, int? departmentId)
        {
            var normalizedName = (name ?? string.Empty).Trim();
            var normalizedPhone = (phone ?? string.Empty).Trim();
            var normalizedEmail = (email ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedName) || string.IsNullOrWhiteSpace(normalizedPhone))
            {
                return null;
            }

            const string sql = @"
SELECT TOP 1 *
FROM Candidate
WHERE ISNULL(Deleted, 0) = 0
  AND LTRIM(RTRIM(ISNULL(Name, ''))) = @name
  AND LTRIM(RTRIM(ISNULL(Phone, ''))) = @phone
  AND ISNULL(NULLIF(LTRIM(RTRIM(Email)), ''), '') = @email
  AND ISNULL(Position, -1) = @position
  AND ISNULL(DepartmentId, -1) = @departmentId
ORDER BY Id DESC";

            using var con = GetConnection();
            return await con.QueryFirstOrDefaultAsync<Candidate>(sql, new
            {
                name = normalizedName,
                phone = normalizedPhone,
                email = normalizedEmail,
                position = position ?? -1,
                departmentId = departmentId ?? -1
            });
        }

        public async Task<bool> Delete(int id)
        {
            return await this.DeleteBase(id, tableDelete: "", delete: 1);
        }

        public async Task<bool> Delete(int id, bool reactive)
        {
            return await this.DeleteBase(id, tableDelete: "", delete: reactive ? 0 : 1);
        }

        public async Task<bool> HasViewAccess(int candidateId, int userId, string? roleCode)
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
    FROM Candidate d
    WHERE d.Id = @CandidateId
      AND ISNULL(d.Deleted, 0) = 0
      AND @UserId > 0
      AND (
            @EffectiveRoleCode IN ('1', '8', '9')
            OR (@EffectiveRoleCode = 'CANDIDATE' AND d.Id = @UserId)
            OR (@EffectiveRoleCode IN ('3', '6') AND (
                    ISNULL(d.CreatedBy, 0) = @UserId
                    OR ISNULL(d.CreatedBy, 0) IN (SELECT Id FROM dbo.getAllUserByUserId(@UserId))
                    OR EXISTS (
                        SELECT 1
                        FROM ScheduleInterview si
                        WHERE ISNULL(si.Deleted, 0) = 0
                          AND si.RelId = d.Id
                          AND ISNULL(si.InterviewerId, 0) = @UserId
                    )
                ))
            OR (@EffectiveRoleCode NOT IN ('1', '8', '9', '3', '6', 'CANDIDATE') AND (
                    ISNULL(d.CreatedBy, 0) = @UserId
                    OR EXISTS (
                        SELECT 1
                        FROM ScheduleInterview si
                        WHERE ISNULL(si.Deleted, 0) = 0
                          AND si.RelId = d.Id
                          AND ISNULL(si.InterviewerId, 0) = @UserId
                    )
                ))
          )
) THEN 1 ELSE 0 END AS bit);";

            return await ExecuteSQLScalar<bool>(sql, new
            {
                CandidateId = candidateId,
                UserId = userId,
                RoleCodeInput = roleCode
            });
        }

        public async Task<bool> HasManageAccess(int candidateId, int userId, string? roleCode)
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
    FROM Candidate d
    WHERE d.Id = @CandidateId
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
                CandidateId = candidateId,
                UserId = userId,
                RoleCodeInput = roleCode
            });
        }

    }
}
