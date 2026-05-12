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
        private static readonly TimeZoneInfo AttendanceTimeZone = ResolveAttendanceTimeZone();

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

            var fromDate = (request.From ?? DateTime.Today).Date;
            var toDate = (request.To ?? fromDate).Date;
            if (fromDate > toDate)
            {
                (fromDate, toDate) = (toDate, fromDate);
            }

            const string sql = @"
DECLARE @RoleCode varchar(10) = NULL;
DECLARE @UserName varchar(50) = NULL;
DECLARE @IsFullAccess bit = 0;

SELECT @RoleCode = RoleCode, @UserName = UserName
FROM Employees
WHERE Id = @UserId;

IF (@RoleCode IN ('1','8') OR (@RoleCode = '2' AND @UserName = 'VS061'))
    SET @IsFullAccess = 1;

;WITH EmployeeScope AS
(
    SELECT
        e.Id AS EmployeeId,
        e.FingerprintCode,
        e.FullName,
        e.UserName,
        dbo.getDisplayMasterdata(e.DepartmentCode) AS DepartmentText,
        dbo.getDisplayMasterdata(e.PositionCode) AS PositionText,
        CAST(e.Onboard AS date) AS OnboardDate,
        CAST(e.ResignationDate AS date) AS ResignationDate
    FROM Employees e
    WHERE ISNULL(e.Deleted, 0) = 0
      AND ISNULL(e.FingerprintCode, '') <> ''
      AND (e.Onboard IS NULL OR CAST(e.Onboard AS date) <= @ToDate)
      AND (e.ResignationDate IS NULL OR CAST(e.ResignationDate AS date) >= @FromDate)
      AND (@EmployeeId IS NULL OR e.Id = @EmployeeId)
      AND (@FingerprintCode = '' OR e.FingerprintCode = @FingerprintCode)
      AND (
            @Token = ''
            OR e.FullName LIKE N'%' + @Token + '%'
            OR e.UserName LIKE N'%' + @Token + '%'
            OR e.FingerprintCode LIKE N'%' + @Token + '%'
          )
      AND (
            @UserId IS NULL OR @UserId <= 0
            OR @IsFullAccess = 1
            OR (@RoleCode = '3' AND e.Id IN (SELECT id FROM getAllUserByUserId(@UserId)))
            OR (@RoleCode NOT IN ('1','2','3','8') AND e.Id = @UserId)
            OR (@RoleCode = '2' AND @IsFullAccess = 0 AND e.Id = @UserId)
          )
),
Calendar AS
(
    SELECT @FromDate AS WorkDate
    UNION ALL
    SELECT DATEADD(DAY, 1, WorkDate)
    FROM Calendar
    WHERE WorkDate < @ToDate
),
EmployeeDates AS
(
    SELECT
        e.EmployeeId,
        e.FingerprintCode,
        e.FullName,
        e.DepartmentText,
        e.PositionText,
        c.WorkDate
    FROM EmployeeScope e
    CROSS JOIN Calendar c
    WHERE (e.OnboardDate IS NULL OR c.WorkDate >= e.OnboardDate)
      AND (e.ResignationDate IS NULL OR c.WorkDate <= e.ResignationDate)
),
Daily AS
(
    SELECT
        ed.EmployeeId,
        ed.FingerprintCode,
        ed.FullName,
        ed.DepartmentText,
        ed.PositionText,
        ISNULL(att.WorkDay, 0) AS WorkDay,
        ISNULL(att.WorkHours, 0) AS WorkHours,
        ISNULL(att.WorkHoursPlus, 0) AS WorkHoursPlus,
        ISNULL(att.TotalHours, ISNULL(att.WorkHours, 0)) AS TotalHours,
        CASE
            WHEN (ISNULL(att.WorkDay, 0) > 0 OR att.CheckIn IS NOT NULL OR att.CheckOut IS NOT NULL)
                THEN CASE
                    WHEN ISNULL(att.LateMinutes, 0) > ISNULL(permit.LateApprovedMinutes, 0)
                        THEN ISNULL(att.LateMinutes, 0) - ISNULL(permit.LateApprovedMinutes, 0)
                    ELSE 0
                END
            ELSE 0
        END AS LateMinutes,
        CASE
            WHEN (ISNULL(att.WorkDay, 0) > 0 OR att.CheckIn IS NOT NULL OR att.CheckOut IS NOT NULL)
                THEN CASE
                    WHEN ISNULL(att.EarlyMinutes, 0) > ISNULL(permit.EarlyApprovedMinutes, 0)
                        THEN ISNULL(att.EarlyMinutes, 0) - ISNULL(permit.EarlyApprovedMinutes, 0)
                    ELSE 0
                END
            ELSE 0
        END AS EarlyMinutes,
        CASE
            WHEN (ISNULL(att.WorkDay, 0) > 0 OR att.CheckIn IS NOT NULL OR att.CheckOut IS NOT NULL) THEN 0
            WHEN holiday.HolidayId IS NOT NULL OR leaveData.LeaveRequestId IS NOT NULL OR offData.IsScheduledOff = 1 THEN 1
            WHEN att.Id IS NOT NULL AND (ISNULL(att.Symbol, '') <> '' OR (att.CheckIn IS NULL AND att.CheckOut IS NULL)) THEN 1
            WHEN att.Id IS NULL AND ed.WorkDate < @Today THEN 1
            ELSE 0
        END AS OffFlag
    FROM EmployeeDates ed
    CROSS APPLY
    (
        SELECT
            CASE
                WHEN DATEDIFF(DAY, '19000107', ed.WorkDate) % 7 = 0 THEN 1
                WHEN DATEDIFF(DAY, '19000107', ed.WorkDate) % 7 = 6 AND ((DAY(ed.WorkDate) - 1) / 7) + 1 > 2 THEN 1
                ELSE 0
            END AS IsScheduledOff
    ) offData
    OUTER APPLY
    (
        SELECT TOP 1
            a.Id,
            a.CheckIn,
            a.CheckOut,
            a.WorkDay,
            a.WorkHours,
            a.WorkHoursPlus,
            a.TotalHours,
            a.LateMinutes,
            a.EarlyMinutes,
            a.Symbol
        FROM AttendanceRecords a
        WHERE a.WorkDate = ed.WorkDate
          AND (
                (a.EmployeeId IS NOT NULL AND a.EmployeeId = ed.EmployeeId)
                OR (a.EmployeeId IS NULL AND a.FingerprintCode = ed.FingerprintCode)
              )
        ORDER BY CASE WHEN a.EmployeeId = ed.EmployeeId THEN 0 ELSE 1 END, a.Id DESC
    ) att
    OUTER APPLY
    (
        SELECT TOP 1
            l.Id AS LeaveRequestId
        FROM LeaveRequests l
        WHERE ISNULL(l.Deleted, 0) = 0
          AND l.Status IN (3, 4)
          AND l.EmployeeId = ed.EmployeeId
          AND ed.WorkDate >= CAST(l.FromDate AS date)
          AND ed.WorkDate <= CAST(l.ToDate AS date)
        ORDER BY l.Id DESC
    ) leaveData
    OUTER APPLY
    (
        SELECT TOP 1
            h.Id AS HolidayId
        FROM AttendanceHolidays h
        WHERE ISNULL(h.Deleted, 0) = 0
          AND ISNULL(h.IsActive, 0) = 1
          AND ed.WorkDate >= CAST(h.FromDate AS date)
          AND ed.WorkDate <= CAST(h.ToDate AS date)
        ORDER BY h.FromDate DESC, h.Id DESC
    ) holiday
    OUTER APPLY
    (
        SELECT
            SUM(CASE WHEN UPPER(ISNULL(r.RequestType, '')) = 'LATE'
                THEN CASE
                    WHEN CAST(r.EndTime AS time) > CAST(r.StartTime AS time)
                        THEN DATEDIFF(MINUTE, CAST(r.StartTime AS time), CAST(r.EndTime AS time))
                    ELSE 0
                END
                ELSE 0
            END) AS LateApprovedMinutes,
            SUM(CASE WHEN UPPER(ISNULL(r.RequestType, '')) = 'EARLY'
                THEN CASE
                    WHEN CAST(r.EndTime AS time) > CAST(r.StartTime AS time)
                        THEN DATEDIFF(MINUTE, CAST(r.StartTime AS time), CAST(r.EndTime AS time))
                    ELSE 0
                END
                ELSE 0
            END) AS EarlyApprovedMinutes
        FROM LateEarlyRequests r
        WHERE ISNULL(r.Deleted, 0) = 0
          AND r.Status = 4
          AND r.EmployeeId = ed.EmployeeId
          AND CAST(r.RequestDate AS date) = ed.WorkDate
    ) permit
),
Summary AS
(
    SELECT
        EmployeeId,
        FingerprintCode,
        FullName,
        DepartmentText,
        PositionText,
        SUM(WorkDay) AS TotalWorkDays,
        SUM(WorkHours) AS TotalWorkHours,
        SUM(WorkHoursPlus) AS TotalOvertimeHours,
        SUM(TotalHours) AS TotalHours,
        SUM(CASE WHEN LateMinutes > 0 THEN 1 ELSE 0 END) AS LateCount,
        SUM(LateMinutes) AS LateMinutes,
        SUM(CASE WHEN EarlyMinutes > 0 THEN 1 ELSE 0 END) AS EarlyCount,
        SUM(EarlyMinutes) AS EarlyMinutes,
        SUM(OffFlag) AS OffCount
    FROM Daily
    GROUP BY EmployeeId, FingerprintCode, FullName, DepartmentText, PositionText
)
SELECT
    COUNT(1) OVER() AS TotalRecord,
    EmployeeId,
    FingerprintCode,
    FullName,
    DepartmentText,
    PositionText,
    TotalWorkDays,
    TotalWorkHours,
    TotalOvertimeHours,
    TotalHours,
    LateCount,
    LateMinutes,
    EarlyCount,
    EarlyMinutes,
    OffCount
FROM Summary
ORDER BY FullName, FingerprintCode
OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY
OPTION (MAXRECURSION 400);";

            using var con = GetConnection();
            var data = (await con.QueryAsync<AttendanceSummaryIndexModel>(sql, new
            {
                FromDate = fromDate,
                ToDate = toDate,
                request.EmployeeId,
                FingerprintCode = request.FingerprintCode?.Trim() ?? string.Empty,
                Token = request.Token?.Trim() ?? string.Empty,
                Offset = offset,
                Limit = limit,
                request.UserId,
                Today = GetAttendanceToday()
            })).ToList();

            return new BaseList
            {
                Total = data.FirstOrDefault()?.TotalRecord ?? 0,
                Data = data
            };
        }

        public async Task<List<AttendanceDetailModel>> GetDetails(int? employeeId, string? fingerprintCode, DateTime? fromDate, DateTime? toDate, int? userId)
        {
            var rangeFrom = (fromDate ?? DateTime.Today).Date;
            var rangeTo = (toDate ?? rangeFrom).Date;
            if (rangeFrom > rangeTo)
            {
                (rangeFrom, rangeTo) = (rangeTo, rangeFrom);
            }

            const string sql = @"
DECLARE @RoleCode varchar(10) = NULL;
DECLARE @UserName varchar(50) = NULL;
DECLARE @IsFullAccess bit = 0;

SELECT @RoleCode = RoleCode, @UserName = UserName
FROM Employees
WHERE Id = @UserId;

IF (@RoleCode IN ('1','8') OR (@RoleCode = '2' AND @UserName = 'VS061'))
    SET @IsFullAccess = 1;

;WITH EmployeeScope AS
(
    SELECT
        e.Id AS EmployeeId,
        e.FingerprintCode,
        CAST(e.Onboard AS date) AS OnboardDate,
        CAST(e.ResignationDate AS date) AS ResignationDate
    FROM Employees e
    WHERE ISNULL(e.Deleted, 0) = 0
      AND ISNULL(e.FingerprintCode, '') <> ''
      AND (e.Onboard IS NULL OR CAST(e.Onboard AS date) <= @ToDate)
      AND (e.ResignationDate IS NULL OR CAST(e.ResignationDate AS date) >= @FromDate)
      AND (
            (@EmployeeId IS NOT NULL AND e.Id = @EmployeeId)
            OR (@EmployeeId IS NULL AND @FingerprintCode <> '' AND e.FingerprintCode = @FingerprintCode)
          )
      AND (
            @UserId IS NULL OR @UserId <= 0
            OR @IsFullAccess = 1
            OR (@RoleCode = '3' AND e.Id IN (SELECT id FROM getAllUserByUserId(@UserId)))
            OR (@RoleCode NOT IN ('1','2','3','8') AND e.Id = @UserId)
            OR (@RoleCode = '2' AND @IsFullAccess = 0 AND e.Id = @UserId)
          )
),
Calendar AS
(
    SELECT @FromDate AS WorkDate
    UNION ALL
    SELECT DATEADD(DAY, 1, WorkDate)
    FROM Calendar
    WHERE WorkDate < @ToDate
),
EmployeeDates AS
(
    SELECT
        e.EmployeeId,
        e.FingerprintCode,
        c.WorkDate
    FROM EmployeeScope e
    CROSS JOIN Calendar c
    WHERE (e.OnboardDate IS NULL OR c.WorkDate >= e.OnboardDate)
      AND (e.ResignationDate IS NULL OR c.WorkDate <= e.ResignationDate)
)
SELECT
    ed.WorkDate,
    ISNULL(att.DayName, '') AS DayName,
    att.CheckIn,
    att.CheckOut,
    ISNULL(att.WorkDay, 0) AS WorkDay,
    ISNULL(att.WorkHours, 0) AS WorkHours,
    ISNULL(att.WorkDayPlus, 0) AS WorkDayPlus,
    ISNULL(att.WorkHoursPlus, 0) AS WorkHoursPlus,
    CASE
        WHEN (ISNULL(att.WorkDay, 0) > 0 OR att.CheckIn IS NOT NULL OR att.CheckOut IS NOT NULL)
            THEN CASE
                WHEN ISNULL(att.LateMinutes, 0) > ISNULL(permit.LateApprovedMinutes, 0)
                    THEN ISNULL(att.LateMinutes, 0) - ISNULL(permit.LateApprovedMinutes, 0)
                ELSE 0
            END
        ELSE 0
    END AS LateMinutes,
    CASE
        WHEN (ISNULL(att.WorkDay, 0) > 0 OR att.CheckIn IS NOT NULL OR att.CheckOut IS NOT NULL)
            THEN CASE
                WHEN ISNULL(att.EarlyMinutes, 0) > ISNULL(permit.EarlyApprovedMinutes, 0)
                    THEN ISNULL(att.EarlyMinutes, 0) - ISNULL(permit.EarlyApprovedMinutes, 0)
                ELSE 0
            END
        ELSE 0
    END AS EarlyMinutes,
    att.ShiftName,
    CASE
        WHEN NULLIF(att.Symbol, '') IS NOT NULL THEN att.Symbol
        WHEN (ISNULL(att.WorkDay, 0) > 0 OR att.CheckIn IS NOT NULL OR att.CheckOut IS NOT NULL) THEN ''
        WHEN holiday.HolidayId IS NOT NULL THEN ISNULL(NULLIF(holiday.HolidayName, ''), N'Nghi le')
        WHEN leaveData.LeaveRequestId IS NOT NULL THEN ISNULL(NULLIF(leaveData.LeaveTypeName, ''), N'Nghi phep')
        WHEN offData.IsScheduledOff = 1 THEN offData.ScheduledOffLabel
        WHEN att.Id IS NULL AND ed.WorkDate < @Today THEN N'Nghi khong phep'
        ELSE ''
    END AS Symbol,
    CASE
        WHEN NULLIF(att.SymbolPlus, '') IS NOT NULL THEN att.SymbolPlus
        WHEN (ISNULL(att.WorkDay, 0) > 0 OR att.CheckIn IS NOT NULL OR att.CheckOut IS NOT NULL) THEN ''
        WHEN holiday.HolidayId IS NOT NULL THEN 'HOLIDAY'
        WHEN leaveData.LeaveRequestId IS NOT NULL THEN ISNULL(leaveData.LeaveTypeCode, '')
        WHEN offData.IsScheduledOff = 1 THEN offData.ScheduledOffCode
        WHEN att.Id IS NULL AND ed.WorkDate < @Today THEN 'ABSENT_UNEXCUSED'
        ELSE ''
    END AS SymbolPlus,
    ISNULL(att.TotalHours, ISNULL(att.WorkHours, 0)) AS TotalHours,
    ed.FingerprintCode,
    CAST(offData.IsScheduledOff AS bit) AS IsScheduledOff,
    CAST(NULL AS nvarchar(max)) AS Note,
    leaveData.LeaveRequestId,
    leaveData.LeaveTypeCode,
    leaveData.LeaveTypeName,
    leaveData.LeaveReason,
    holiday.HolidayId,
    holiday.HolidayName,
    permit.LateRequestId AS ApprovedLateRequestId,
    ISNULL(permit.LateApprovedMinutes, 0) AS ApprovedLateMinutes,
    permit.LateNote AS ApprovedLateNote,
    permit.EarlyRequestId AS ApprovedEarlyRequestId,
    ISNULL(permit.EarlyApprovedMinutes, 0) AS ApprovedEarlyMinutes,
    permit.EarlyNote AS ApprovedEarlyNote
FROM EmployeeDates ed
CROSS APPLY
(
    SELECT
        CASE
            WHEN DATEDIFF(DAY, '19000107', ed.WorkDate) % 7 = 0 THEN 1
            WHEN DATEDIFF(DAY, '19000107', ed.WorkDate) % 7 = 6 AND ((DAY(ed.WorkDate) - 1) / 7) + 1 > 2 THEN 1
            ELSE 0
        END AS IsScheduledOff,
        CASE
            WHEN DATEDIFF(DAY, '19000107', ed.WorkDate) % 7 = 0 THEN N'Nghi Chu nhat'
            WHEN DATEDIFF(DAY, '19000107', ed.WorkDate) % 7 = 6 THEN N'Nghi thu 7'
            ELSE N'Nghi'
        END AS ScheduledOffLabel,
        CASE
            WHEN DATEDIFF(DAY, '19000107', ed.WorkDate) % 7 = 0 THEN 'SUNDAY_OFF'
            WHEN DATEDIFF(DAY, '19000107', ed.WorkDate) % 7 = 6 THEN 'SATURDAY_OFF'
            ELSE 'SCHEDULED_OFF'
        END AS ScheduledOffCode
) offData
OUTER APPLY
(
    SELECT TOP 1
        a.Id,
        a.DayName,
        a.CheckIn,
        a.CheckOut,
        a.WorkDay,
        a.WorkHours,
        a.WorkDayPlus,
        a.WorkHoursPlus,
        a.LateMinutes,
        a.EarlyMinutes,
        a.ShiftName,
        a.Symbol,
        a.SymbolPlus,
        a.TotalHours
    FROM AttendanceRecords a
    WHERE a.WorkDate = ed.WorkDate
      AND (
            (a.EmployeeId IS NOT NULL AND a.EmployeeId = ed.EmployeeId)
            OR (a.EmployeeId IS NULL AND a.FingerprintCode = ed.FingerprintCode)
          )
    ORDER BY CASE WHEN a.EmployeeId = ed.EmployeeId THEN 0 ELSE 1 END, a.Id DESC
) att
OUTER APPLY
(
    SELECT TOP 1
        l.Id AS LeaveRequestId,
        l.LeaveTypeCode,
        md.Name AS LeaveTypeName,
        l.Reason AS LeaveReason
    FROM LeaveRequests l
    LEFT JOIN MasterData md
        ON md.Code = l.LeaveTypeCode
       AND md.TypeData = 30
       AND ISNULL(md.Deleted, 0) = 0
    WHERE ISNULL(l.Deleted, 0) = 0
      AND l.Status IN (3, 4)
      AND l.EmployeeId = ed.EmployeeId
      AND ed.WorkDate >= CAST(l.FromDate AS date)
      AND ed.WorkDate <= CAST(l.ToDate AS date)
    ORDER BY l.Id DESC
) leaveData
OUTER APPLY
(
    SELECT TOP 1
        h.Id AS HolidayId,
        h.HolidayName
    FROM AttendanceHolidays h
    WHERE ISNULL(h.Deleted, 0) = 0
      AND ISNULL(h.IsActive, 0) = 1
      AND ed.WorkDate >= CAST(h.FromDate AS date)
      AND ed.WorkDate <= CAST(h.ToDate AS date)
    ORDER BY h.FromDate DESC, h.Id DESC
) holiday
OUTER APPLY
(
    SELECT
        MAX(CASE WHEN UPPER(ISNULL(r.RequestType, '')) = 'LATE' THEN r.Id END) AS LateRequestId,
        SUM(CASE WHEN UPPER(ISNULL(r.RequestType, '')) = 'LATE'
            THEN CASE
                WHEN CAST(r.EndTime AS time) > CAST(r.StartTime AS time)
                    THEN DATEDIFF(MINUTE, CAST(r.StartTime AS time), CAST(r.EndTime AS time))
                ELSE 0
            END
            ELSE 0
        END) AS LateApprovedMinutes,
        MAX(CASE WHEN UPPER(ISNULL(r.RequestType, '')) = 'LATE'
            THEN N'Di tre co phep '
                + CONVERT(varchar(5), CAST(r.StartTime AS time), 108)
                + N' - '
                + CONVERT(varchar(5), CAST(r.EndTime AS time), 108)
                + CASE
                    WHEN ISNULL(LTRIM(RTRIM(r.Reason)), '') = '' THEN N''
                    ELSE N': ' + r.Reason
                END
            END) AS LateNote,
        MAX(CASE WHEN UPPER(ISNULL(r.RequestType, '')) = 'EARLY' THEN r.Id END) AS EarlyRequestId,
        SUM(CASE WHEN UPPER(ISNULL(r.RequestType, '')) = 'EARLY'
            THEN CASE
                WHEN CAST(r.EndTime AS time) > CAST(r.StartTime AS time)
                    THEN DATEDIFF(MINUTE, CAST(r.StartTime AS time), CAST(r.EndTime AS time))
                ELSE 0
            END
            ELSE 0
        END) AS EarlyApprovedMinutes,
        MAX(CASE WHEN UPPER(ISNULL(r.RequestType, '')) = 'EARLY'
            THEN N'Ve som co phep '
                + CONVERT(varchar(5), CAST(r.StartTime AS time), 108)
                + N' - '
                + CONVERT(varchar(5), CAST(r.EndTime AS time), 108)
                + CASE
                    WHEN ISNULL(LTRIM(RTRIM(r.Reason)), '') = '' THEN N''
                    ELSE N': ' + r.Reason
                END
            END) AS EarlyNote
    FROM LateEarlyRequests r
    WHERE ISNULL(r.Deleted, 0) = 0
      AND r.Status = 4
      AND r.EmployeeId = ed.EmployeeId
      AND CAST(r.RequestDate AS date) = ed.WorkDate
) permit
ORDER BY ed.WorkDate
OPTION (MAXRECURSION 400);";

            using var con = GetConnection();
            var result = await con.QueryAsync<AttendanceDetailModel>(sql, new
            {
                EmployeeId = employeeId,
                FingerprintCode = fingerprintCode?.Trim() ?? string.Empty,
                FromDate = rangeFrom,
                ToDate = rangeTo,
                UserId = userId,
                Today = GetAttendanceToday()
            });

            return result.ToList();
        }

        private static DateTime GetAttendanceToday()
        {
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, AttendanceTimeZone).Date;
        }

        private static TimeZoneInfo ResolveAttendanceTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone("UTC+07", TimeSpan.FromHours(7), "UTC+07", "UTC+07");
                }
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("UTC+07", TimeSpan.FromHours(7), "UTC+07", "UTC+07");
            }
            catch
            {
                return TimeZoneInfo.CreateCustomTimeZone("UTC+07", TimeSpan.FromHours(7), "UTC+07", "UTC+07");
            }
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
