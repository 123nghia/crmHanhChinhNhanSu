-- =============================================
-- Migration: V059__Update_LeaveBalance_StatusWork_Filter
-- Description: Filter leave balance list by working status (StatusWork).
-- =============================================

PRINT 'Applying migration V059: Update leave balance status filter...';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Employee_GetLeaveBalances]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Employee_GetLeaveBalances];
GO

CREATE PROCEDURE [dbo].[sp_Employee_GetLeaveBalances]
(
    @Token nvarchar(255) = '',
    @StatusWork nvarchar(50) = NULL,
    @offset int = 0,
    @limit int = 20
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH EmployeeBase AS
    (
        SELECT
            d.Id,
            d.UserName,
            d.FullName,
            d.DepartmentCode,
            dbo.getDisplayMasterdata(d.DepartmentCode) AS DepartmentText,
            d.PositionCode,
            dbo.getDisplayMasterdata(d.PositionCode) AS PositionText,
            d.AllowedLeaveDays,
            d.CarryOverLeaveDays,
            d.UsedLeaveDays
        FROM Employees d
        WHERE ISNULL(d.Deleted, 0) = 0
          AND (@Token = '' OR d.UserName LIKE N'%' + @Token + '%' OR d.FullName LIKE N'%' + @Token + '%' OR d.Phone LIKE N'%' + @Token + '%')
          AND (@StatusWork IS NULL OR @StatusWork = '' OR @StatusWork = '-1' OR d.StatusWork = @StatusWork)
    ),
    LeaveAgg AS
    (
        SELECT
            l.EmployeeId,
            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NP' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedAnnualLeaveDays,
            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NB' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedSickLeaveDays,
            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NVR' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedPersonalLeaveDays,
            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NTS' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedMaternityLeaveDays,
            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NKL' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedUnpaidLeaveDays,
            SUM(CASE WHEN l.Status IN (3,4) THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS TotalApprovedLeaveDays
        FROM LeaveRequests l
        WHERE l.Deleted = 0
        GROUP BY l.EmployeeId
    )
    SELECT
        COUNT(1) OVER() AS TotalRecord,
        e.Id,
        e.UserName,
        e.FullName,
        e.DepartmentCode,
        e.DepartmentText,
        e.PositionCode,
        e.PositionText,
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
    FROM EmployeeBase e
    LEFT JOIN LeaveAgg la ON e.Id = la.EmployeeId
    ORDER BY e.FullName
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END
GO

PRINT 'Migration V059 completed successfully.';
