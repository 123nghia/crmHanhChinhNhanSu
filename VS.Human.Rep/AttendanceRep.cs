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
    public class AttendanceRep : RepositoryBase<AttendanceRecord>, IAttendanceRep
    {
        public AttendanceRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<bool> UpsertAsync(AttendanceRecord record, int userId)
        {
            var p = new DynamicParameters();
            p.Add("@EmployeeId", record.EmployeeId);
            p.Add("@FingerprintCode", record.FingerprintCode);
            p.Add("@EmployeeName", record.EmployeeName);
            p.Add("@DepartmentName", record.DepartmentName);
            p.Add("@PositionName", record.PositionName);
            p.Add("@WorkDate", record.WorkDate);
            p.Add("@DayName", record.DayName);
            p.Add("@CheckIn", record.CheckIn);
            p.Add("@CheckOut", record.CheckOut);
            p.Add("@WorkDay", record.WorkDay);
            p.Add("@WorkHours", record.WorkHours);
            p.Add("@WorkDayPlus", record.WorkDayPlus);
            p.Add("@WorkHoursPlus", record.WorkHoursPlus);
            p.Add("@LateMinutes", record.LateMinutes);
            p.Add("@EarlyMinutes", record.EarlyMinutes);
            p.Add("@Shift1", record.Shift1);
            p.Add("@Shift2", record.Shift2);
            p.Add("@Shift3", record.Shift3);
            p.Add("@ShiftName", record.ShiftName);
            p.Add("@Symbol", record.Symbol);
            p.Add("@SymbolPlus", record.SymbolPlus);
            p.Add("@TotalHours", record.TotalHours);
            p.Add("@SourceFile", record.SourceFile);
            p.Add("@RowIndex", record.RowIndex);
            p.Add("@UserId", userId);

            return await ExecuteSQL("sp_Attendance_Upsert", p, CommandType.StoredProcedure);
        }

        public async Task<BaseList> GetSummary(AttendanceRequest request)
        {
            var page = request.Page;
            var limit = request.Limit;
            ProcessInputPaging(ref page, ref limit, out var offset);

            request.Page = page;
            request.Limit = limit;

            var parameters = new
            {
                FromDate = request.From,
                ToDate = request.To,
                EmployeeId = request.EmployeeId,
                Token = request.Token ?? string.Empty,
                offset,
                limit,
                UserId = request.UserId
            };

            return await GetBaseAll<AttendanceSummaryIndexModel>(request, parameters, "sp_Attendance_GetSummary");
        }

        public async Task<List<AttendanceDetailModel>> GetDetails(int? employeeId, string? fingerprintCode, DateTime? fromDate, DateTime? toDate, int? userId)
        {
            var p = new DynamicParameters();
            p.Add("@EmployeeId", employeeId);
            p.Add("@FingerprintCode", fingerprintCode);
            p.Add("@FromDate", fromDate);
            p.Add("@ToDate", toDate);
            p.Add("@UserId", userId);

            return await ExecuteSQL<AttendanceDetailModel>("sp_Attendance_GetDetails", p);
        }

        public async Task<List<AttendanceDepartmentRuleModel>> GetDepartmentRulesAsync()
        {
            const string sql = @"
SELECT
    r.Id,
    r.DepartmentCode,
    md.Name AS DepartmentText,
    r.WorkStartTime,
    r.LunchStartTime,
    r.LunchEndTime,
    r.WorkEndTime,
    CAST(r.IsActive AS bit) AS IsActive,
    r.CreateAt,
    r.CreatedBy,
    r.UpdateAt,
    r.UpdatedBy,
    CAST(r.Deleted AS bit) AS Deleted,
    CAST(
        ROUND(
            (
                DATEDIFF(MINUTE, r.WorkStartTime, r.LunchStartTime)
                + DATEDIFF(MINUTE, r.LunchEndTime, r.WorkEndTime)
            ) / 60.0,
            2
        ) AS decimal(10,2)
    ) AS ExpectedWorkHours
FROM AttendanceDepartmentRules r
LEFT JOIN MasterData md
    ON md.Code = r.DepartmentCode
   AND md.TypeData = 5
   AND ISNULL(md.Deleted, 0) = 0
WHERE ISNULL(r.Deleted, 0) = 0
ORDER BY ISNULL(md.Name, r.DepartmentCode), r.DepartmentCode;";

            try
            {
                using var con = GetConnection();
                var result = await con.QueryAsync<AttendanceDepartmentRuleModel>(sql);
                return result.ToList();
            }
            catch
            {
                return new List<AttendanceDepartmentRuleModel>();
            }
        }

        public async Task<bool> UpsertDepartmentRuleAsync(AttendanceDepartmentRuleModel rule, int userId)
        {
            const string sql = @"
IF EXISTS (
    SELECT 1
    FROM AttendanceDepartmentRules
    WHERE Id = @Id
      AND ISNULL(Deleted, 0) = 0
)
BEGIN
    UPDATE AttendanceDepartmentRules
    SET DepartmentCode = @DepartmentCode,
        WorkStartTime = @WorkStartTime,
        LunchStartTime = @LunchStartTime,
        LunchEndTime = @LunchEndTime,
        WorkEndTime = @WorkEndTime,
        IsActive = @IsActive,
        UpdatedBy = @UserId,
        UpdateAt = GETDATE()
    WHERE Id = @Id;
END
ELSE
BEGIN
    INSERT INTO AttendanceDepartmentRules
    (
        DepartmentCode,
        WorkStartTime,
        LunchStartTime,
        LunchEndTime,
        WorkEndTime,
        IsActive,
        Deleted,
        CreatedBy,
        CreateAt,
        UpdatedBy,
        UpdateAt
    )
    VALUES
    (
        @DepartmentCode,
        @WorkStartTime,
        @LunchStartTime,
        @LunchEndTime,
        @WorkEndTime,
        @IsActive,
        0,
        @UserId,
        GETDATE(),
        @UserId,
        GETDATE()
    );
END";

            return await ExecuteSQL(
                sql,
                new
                {
                    rule.Id,
                    rule.DepartmentCode,
                    rule.WorkStartTime,
                    rule.LunchStartTime,
                    rule.LunchEndTime,
                    rule.WorkEndTime,
                    rule.IsActive,
                    UserId = userId
                },
                CommandType.Text);
        }

        public async Task<bool> DeleteDepartmentRuleAsync(int id, int userId)
        {
            const string sql = @"
UPDATE AttendanceDepartmentRules
SET Deleted = 1,
    IsActive = 0,
    UpdatedBy = @UserId,
    UpdateAt = GETDATE()
WHERE Id = @Id
  AND ISNULL(Deleted, 0) = 0;";

            return await ExecuteSQL(sql, new { Id = id, UserId = userId }, CommandType.Text);
        }

        public async Task<List<AttendanceHolidayModel>> GetHolidaysAsync()
        {
            const string sql = @"
SELECT
    Id,
    HolidayName,
    CAST(FromDate AS date) AS FromDate,
    CAST(ToDate AS date) AS ToDate,
    CAST(IsActive AS bit) AS IsActive,
    CreateAt,
    CreatedBy,
    UpdateAt,
    UpdatedBy,
    CAST(Deleted AS bit) AS Deleted
FROM AttendanceHolidays
WHERE ISNULL(Deleted, 0) = 0
ORDER BY FromDate DESC, ToDate DESC, HolidayName;";

            try
            {
                using var con = GetConnection();
                var result = await con.QueryAsync<AttendanceHolidayModel>(sql);
                return result.ToList();
            }
            catch
            {
                return new List<AttendanceHolidayModel>();
            }
        }

        public async Task<bool> UpsertHolidayAsync(AttendanceHolidayModel holiday, int userId)
        {
            const string sql = @"
IF EXISTS (
    SELECT 1
    FROM AttendanceHolidays
    WHERE Id = @Id
      AND ISNULL(Deleted, 0) = 0
)
BEGIN
    UPDATE AttendanceHolidays
    SET HolidayName = @HolidayName,
        FromDate = @FromDate,
        ToDate = @ToDate,
        IsActive = @IsActive,
        UpdatedBy = @UserId,
        UpdateAt = GETDATE()
    WHERE Id = @Id;
END
ELSE
BEGIN
    INSERT INTO AttendanceHolidays
    (
        HolidayName,
        FromDate,
        ToDate,
        IsActive,
        Deleted,
        CreatedBy,
        CreateAt,
        UpdatedBy,
        UpdateAt
    )
    VALUES
    (
        @HolidayName,
        @FromDate,
        @ToDate,
        @IsActive,
        0,
        @UserId,
        GETDATE(),
        @UserId,
        GETDATE()
    );
END";

            return await ExecuteSQL(
                sql,
                new
                {
                    holiday.Id,
                    holiday.HolidayName,
                    FromDate = holiday.FromDate.Date,
                    ToDate = holiday.ToDate.Date,
                    holiday.IsActive,
                    UserId = userId
                },
                CommandType.Text);
        }

        public async Task<bool> DeleteHolidayAsync(int id, int userId)
        {
            const string sql = @"
UPDATE AttendanceHolidays
SET Deleted = 1,
    IsActive = 0,
    UpdatedBy = @UserId,
    UpdateAt = GETDATE()
WHERE Id = @Id
  AND ISNULL(Deleted, 0) = 0;";

            return await ExecuteSQL(sql, new { Id = id, UserId = userId }, CommandType.Text);
        }

        public async Task<List<AttendanceHolidayModel>> GetHolidaysForEvaluationAsync(DateTime fromDate, DateTime toDate)
        {
            const string sql = @"
SELECT
    Id,
    HolidayName,
    CAST(FromDate AS date) AS FromDate,
    CAST(ToDate AS date) AS ToDate,
    CAST(IsActive AS bit) AS IsActive
FROM AttendanceHolidays
WHERE ISNULL(Deleted, 0) = 0
  AND ISNULL(IsActive, 0) = 1
  AND CAST(FromDate AS date) <= @ToDate
  AND CAST(ToDate AS date) >= @FromDate
ORDER BY FromDate, ToDate, HolidayName;";

            try
            {
                using var con = GetConnection();
                var result = await con.QueryAsync<AttendanceHolidayModel>(sql, new
                {
                    FromDate = fromDate.Date,
                    ToDate = toDate.Date
                });

                return result.ToList();
            }
            catch
            {
                return new List<AttendanceHolidayModel>();
            }
        }

        public async Task<List<AttendanceEvaluationRecord>> GetRecordsForEvaluationAsync(DateTime fromDate, DateTime toDate)
        {
            const string sql = @"
SELECT
    a.Id,
    a.EmployeeId,
    a.FingerprintCode,
    a.WorkDate,
    a.CheckIn,
    a.CheckOut,
    a.WorkDay,
    a.WorkHours,
    a.TotalHours,
    a.LateMinutes,
    a.EarlyMinutes,
    a.Symbol,
    a.SymbolPlus,
    emp.DepartmentCode,
    dbo.getDisplayMasterdata(emp.DepartmentCode) AS DepartmentText
FROM AttendanceRecords a
OUTER APPLY
(
    SELECT TOP 1 e.DepartmentCode
    FROM Employees e
    WHERE ISNULL(e.Deleted, 0) = 0
      AND (
            (a.EmployeeId IS NOT NULL AND e.Id = a.EmployeeId)
            OR (a.EmployeeId IS NULL AND ISNULL(e.FingerprintCode, '') = a.FingerprintCode)
          )
    ORDER BY CASE WHEN a.EmployeeId IS NOT NULL AND e.Id = a.EmployeeId THEN 0 ELSE 1 END, e.Id
) emp
WHERE a.WorkDate >= @FromDate
  AND a.WorkDate <= @ToDate
ORDER BY a.WorkDate, a.FingerprintCode;";

            try
            {
                using var con = GetConnection();
                var result = await con.QueryAsync<AttendanceEvaluationRecord>(sql, new
                {
                    FromDate = fromDate.Date,
                    ToDate = toDate.Date
                });

                return result.ToList();
            }
            catch
            {
                return new List<AttendanceEvaluationRecord>();
            }
        }

        public async Task<List<AttendanceEvaluationEmployee>> GetEmployeesForEvaluationAsync(DateTime fromDate, DateTime toDate)
        {
            const string sql = @"
SELECT
    e.Id AS EmployeeId,
    e.FingerprintCode,
    e.FullName AS EmployeeName,
    e.DepartmentCode,
    dbo.getDisplayMasterdata(e.DepartmentCode) AS DepartmentText,
    dbo.getDisplayMasterdata(e.PositionCode) AS PositionText,
    CAST(e.Onboard AS date) AS OnboardDate,
    CAST(e.ResignationDate AS date) AS ResignationDate
FROM Employees e
WHERE ISNULL(e.Deleted, 0) = 0
  AND ISNULL(e.IsActive, 0) = 1
  AND ISNULL(e.FingerprintCode, '') <> ''
  AND (e.Onboard IS NULL OR CAST(e.Onboard AS date) <= @ToDate)
  AND (e.ResignationDate IS NULL OR CAST(e.ResignationDate AS date) >= @FromDate)
ORDER BY e.DepartmentCode, e.FullName, e.Id;";

            try
            {
                using var con = GetConnection();
                var result = await con.QueryAsync<AttendanceEvaluationEmployee>(sql, new
                {
                    FromDate = fromDate.Date,
                    ToDate = toDate.Date
                });

                return result.ToList();
            }
            catch
            {
                return new List<AttendanceEvaluationEmployee>();
            }
        }

        public async Task<List<AttendanceApprovedLeave>> GetApprovedLeavesForEvaluationAsync(DateTime fromDate, DateTime toDate)
        {
            const string sql = @"
SELECT
    l.EmployeeId,
    CAST(l.FromDate AS date) AS FromDate,
    CAST(l.ToDate AS date) AS ToDate,
    l.LeaveTypeCode,
    md.Name AS LeaveTypeName
FROM LeaveRequests l
LEFT JOIN MasterData md
    ON md.Code = l.LeaveTypeCode
   AND md.TypeData = 30
   AND ISNULL(md.Deleted, 0) = 0
WHERE ISNULL(l.Deleted, 0) = 0
  AND l.Status IN (3, 4)
  AND CAST(l.FromDate AS date) <= @ToDate
  AND CAST(l.ToDate AS date) >= @FromDate
ORDER BY l.EmployeeId, l.FromDate, l.Id;";

            try
            {
                using var con = GetConnection();
                var result = await con.QueryAsync<AttendanceApprovedLeave>(sql, new
                {
                    FromDate = fromDate.Date,
                    ToDate = toDate.Date
                });

                return result.ToList();
            }
            catch
            {
                return new List<AttendanceApprovedLeave>();
            }
        }

        public async Task<bool> UpdateEvaluationAsync(AttendanceEvaluationUpdate update, int userId)
        {
            const string sql = @"
UPDATE AttendanceRecords
SET WorkDay = @WorkDay,
    WorkHours = @WorkHours,
    TotalHours = @TotalHours,
    LateMinutes = @LateMinutes,
    EarlyMinutes = @EarlyMinutes,
    Symbol = @Symbol,
    ShiftName = COALESCE(@ShiftName, ShiftName),
    UpdatedBy = @UserId,
    UpdateAt = GETDATE()
WHERE Id = @Id
  AND ISNULL(IsLocked, 0) = 0
  AND NOT EXISTS (
      SELECT 1
      FROM AttendanceLocks l
      WHERE l.IsLocked = 1
        AND ISNULL(l.Deleted, 0) = 0
        AND AttendanceRecords.WorkDate >= l.RangeFrom
        AND AttendanceRecords.WorkDate <= l.RangeTo
  );";
            return await ExecuteSQL(sql, new
            {
                update.Id,
                update.WorkDay,
                update.WorkHours,
                update.TotalHours,
                update.LateMinutes,
                update.EarlyMinutes,
                update.Symbol,
                update.ShiftName,
                UserId = userId
            }, CommandType.Text);
        }
    }
}
