PRINT 'Applying migration V098: fix leave balance total logic...';
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Employee_GetLeaveBalances]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Employee_GetLeaveBalances];
GO

CREATE PROCEDURE [dbo].[sp_Employee_GetLeaveBalances]
(
    @Token nvarchar(255) = '',
    @StatusWork nvarchar(50) = NULL,
    @DepartmentCode nvarchar(50) = NULL,
    @PositionCode nvarchar(50) = NULL,
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
            d.StatusWork,
            dbo.getDisplayMasterdata(d.StatusWork) AS StatusWorkText,
            ISNULL(d.AllowedLeaveDays, 0) AS AllowedLeaveDays,
            ISNULL(d.CarryOverLeaveDays, 0) AS CarryOverLeaveDays,
            ISNULL(d.ExpiredLeaveDays, 0) AS ExpiredLeaveDays,
            ISNULL(d.UsedLeaveDays, 0) AS UsedLeaveDays
        FROM Employees d
        WHERE ISNULL(d.Deleted, 0) = 0
          AND (@Token = '' OR d.UserName LIKE N'%' + @Token + '%' OR d.FullName LIKE N'%' + @Token + '%' OR d.Phone LIKE N'%' + @Token + '%')
          AND (@StatusWork IS NULL OR @StatusWork = '' OR @StatusWork = '-1' OR d.StatusWork = @StatusWork)
          AND (@DepartmentCode IS NULL OR @DepartmentCode = '' OR @DepartmentCode = '-1' OR d.DepartmentCode = @DepartmentCode)
          AND (@PositionCode IS NULL OR @PositionCode = '' OR @PositionCode = '-1' OR d.PositionCode = @PositionCode)
    ),
    LeaveAgg AS
    (
        SELECT
            l.EmployeeId,
            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NP' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedAnnualLeaveDays,
            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NB' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedSickLeaveDays,
            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NVR' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedPersonalLeaveDays,
            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NTS' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedMaternityLeaveDays,
            SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NKL' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedUnpaidLeaveDays
        FROM LeaveRequests l
        WHERE ISNULL(l.Deleted, 0) = 0
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
        e.StatusWork,
        e.StatusWorkText,
        e.AllowedLeaveDays,
        e.CarryOverLeaveDays,
        e.ExpiredLeaveDays,
        e.UsedLeaveDays,
        e.AllowedLeaveDays + e.CarryOverLeaveDays - e.ExpiredLeaveDays - e.UsedLeaveDays AS RemainingLeaveDays,
        CASE
            WHEN e.UsedLeaveDays > ISNULL(la.UsedAnnualLeaveDays, 0) THEN e.UsedLeaveDays
            ELSE ISNULL(la.UsedAnnualLeaveDays, 0)
        END AS UsedAnnualLeaveDays,
        ISNULL(la.UsedSickLeaveDays, 0) AS UsedSickLeaveDays,
        ISNULL(la.UsedPersonalLeaveDays, 0) AS UsedPersonalLeaveDays,
        ISNULL(la.UsedMaternityLeaveDays, 0) AS UsedMaternityLeaveDays,
        ISNULL(la.UsedUnpaidLeaveDays, 0) AS UsedUnpaidLeaveDays,
        CASE
            WHEN e.UsedLeaveDays > ISNULL(la.UsedAnnualLeaveDays, 0) THEN e.UsedLeaveDays
            ELSE ISNULL(la.UsedAnnualLeaveDays, 0)
        END
        + ISNULL(la.UsedSickLeaveDays, 0)
        + ISNULL(la.UsedPersonalLeaveDays, 0)
        + ISNULL(la.UsedMaternityLeaveDays, 0)
        + ISNULL(la.UsedUnpaidLeaveDays, 0) AS TotalApprovedLeaveDays
    FROM EmployeeBase e
    LEFT JOIN LeaveAgg la ON e.Id = la.EmployeeId
    ORDER BY e.FullName
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END
GO

PRINT 'Migration V098 completed successfully.';
