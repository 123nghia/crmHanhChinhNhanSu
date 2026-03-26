-- =============================================
-- Migration: V060__Add_ExpiredLeaveDays_And_Filters
-- Description: Add ExpiredLeaveDays column, update stored procedure with new filters
-- =============================================

PRINT 'Applying migration V060: Add ExpiredLeaveDays and filters...';

-- Add ExpiredLeaveDays column to Employees table if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'ExpiredLeaveDays')
BEGIN
    ALTER TABLE [dbo].[Employees] ADD [ExpiredLeaveDays] DECIMAL(18,2) DEFAULT 0;
    PRINT 'Added ExpiredLeaveDays column to Employees table.';
END
GO

-- Update stored procedure sp_Employee_GetLeaveBalances
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
            d.AllowedLeaveDays,
            d.CarryOverLeaveDays,
            ISNULL(d.ExpiredLeaveDays, 0) AS ExpiredLeaveDays,
            d.UsedLeaveDays
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
        e.StatusWork,
        e.StatusWorkText,
        e.AllowedLeaveDays,
        e.CarryOverLeaveDays,
        e.ExpiredLeaveDays,
        e.UsedLeaveDays,
        -- Formula: RemainingLeaveDays = AllowedLeaveDays + CarryOverLeaveDays - ExpiredLeaveDays - UsedLeaveDays
        ISNULL(e.AllowedLeaveDays, 0) + ISNULL(e.CarryOverLeaveDays, 0) - ISNULL(e.ExpiredLeaveDays, 0) - ISNULL(e.UsedLeaveDays, 0) AS RemainingLeaveDays,
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

-- Update stored procedure sp_Employee_UpdateLeaveBalance to include ExpiredLeaveDays
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Employee_UpdateLeaveBalance]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Employee_UpdateLeaveBalance];
GO

CREATE PROCEDURE [dbo].[sp_Employee_UpdateLeaveBalance]
(
    @EmployeeId int,
    @AllowedLeaveDays decimal(18,2) = NULL,
    @CarryOverLeaveDays decimal(18,2) = NULL,
    @UsedLeaveDays decimal(18,2) = NULL,
    @ExpiredLeaveDays decimal(18,2) = NULL,
    @UpdatedBy int = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE Employees
    SET 
        AllowedLeaveDays = COALESCE(@AllowedLeaveDays, AllowedLeaveDays),
        CarryOverLeaveDays = COALESCE(@CarryOverLeaveDays, CarryOverLeaveDays),
        UsedLeaveDays = COALESCE(@UsedLeaveDays, UsedLeaveDays),
        ExpiredLeaveDays = COALESCE(@ExpiredLeaveDays, ExpiredLeaveDays),
        UpdateAt = GETDATE(),
        UpdatedBy = @UpdatedBy
    WHERE Id = @EmployeeId;
    
    SELECT @@ROWCOUNT AS AffectedRows;
END
GO

PRINT 'Migration V060 completed successfully.';
