-- =============================================
-- Migration: V034__Add_Leave_Balance_Management
-- Description: Add leave balance page and stored procedures.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM AppPages WHERE Code = 'LeaveBalance')
BEGIN
    INSERT INTO [dbo].[AppPages] (Code, Name, [Group], OrderIndex)
    VALUES ('LeaveBalance', N'Quan ly so ngay nghi phep', 'Human Resource', 12);
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type in (N'U'))
BEGIN
    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '1', 'LeaveBalance', 1, 1, 1, 1, 1
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '1' AND PageCode = 'LeaveBalance');

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '2', 'LeaveBalance', 1, 0, 1, 0, 0
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '2' AND PageCode = 'LeaveBalance');
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Employee_GetLeaveBalances]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Employee_GetLeaveBalances];
GO

CREATE PROCEDURE [dbo].[sp_Employee_GetLeaveBalances]
(
    @Token nvarchar(255) = '',
    @offset int = 0,
    @limit int = 20
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(d.Id) OVER() AS TotalRecord,
        d.Id,
        d.UserName,
        d.FullName,
        d.DepartmentCode,
        dbo.getDisplayMasterdata(d.DepartmentCode) AS DepartmentText,
        d.PositionCode,
        dbo.getDisplayMasterdata(d.PositionCode) AS PositionText,
        d.AllowedLeaveDays,
        d.UsedLeaveDays,
        ISNULL(d.AllowedLeaveDays, 0) - ISNULL(d.UsedLeaveDays, 0) AS RemainingLeaveDays
    FROM Employees d
    WHERE ISNULL(d.Deleted, 0) = 0
      AND (@Token = '' OR d.UserName LIKE N'%' + @Token + '%' OR d.FullName LIKE N'%' + @Token + '%' OR d.Phone LIKE N'%' + @Token + '%')
    ORDER BY d.FullName
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Employee_UpdateLeaveBalance]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Employee_UpdateLeaveBalance];
GO

CREATE PROCEDURE [dbo].[sp_Employee_UpdateLeaveBalance]
(
    @Id int,
    @AllowedLeaveDays decimal(18,2) = NULL,
    @UsedLeaveDays decimal(18,2) = NULL,
    @UpdatedBy int
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Employees
    SET
        AllowedLeaveDays = COALESCE(@AllowedLeaveDays, AllowedLeaveDays),
        UsedLeaveDays = COALESCE(@UsedLeaveDays, UsedLeaveDays),
        UpdateAt = GETDATE(),
        UpdatedBy = @UpdatedBy
    WHERE Id = @Id;
END
GO
