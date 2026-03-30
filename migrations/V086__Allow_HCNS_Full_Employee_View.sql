-- =============================================
-- Migration: V086__Allow_HCNS_Full_Employee_View
-- Author: System
-- Date: 2026-03-26
-- Description: Cho phep role HCNS (9) xem toan bo danh sach nhan vien
--              trong sp_Employee_getAll_Extended, tuong tu Admin va BGD.
-- =============================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Employee_getAll_Extended]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  Dropping existing procedure sp_Employee_getAll_Extended...';
    DROP PROCEDURE [dbo].[sp_Employee_getAll_Extended];
END
GO

PRINT '  Creating procedure sp_Employee_getAll_Extended (HCNS full access)...';
GO

CREATE PROCEDURE [dbo].[sp_Employee_getAll_Extended]
(
    @Token nvarchar(255) = '',
    @OrderBy nvarchar(255) = '',
    @UserId int = null,
    @RoleCode nvarchar(50) = null,
    @MemberId int = null,
    @GroupId int = null,
    @offset int = 0,
    @limit int = 20,
    @Status int = -1,
    @StatusWork nvarchar(50) = null,
    @DocumentStatus nvarchar(50) = null,
    @fromDate DATETIME2 = null,
    @toDate DATETIME2 = null,
    @IsDeleted bit = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @where nvarchar(max) = ' WHERE 1=1 ';
    DECLARE @mainClause nvarchar(max);
    DECLARE @params nvarchar(2200);

    SET @mainClause = '
    SELECT
        COUNT(d.Id) OVER() AS TotalRecord,
        d.*,
        dbo.getDisplayMasterData(d.Status) AS StatusText,
        dbo.getDisplayMasterData(d.StatusWork) AS StatusWorkText,
        dbo.getDisplayMasterData(d.DocumentStatus) AS DocumentStatusText,
        dbo.getDisplayMasterdata(d.DepartmentCode) AS DepartmentText,
        dbo.getDisplayMasterdata(d.PositionCode) AS PositionText,
        dbo.getDisplayMasterdata(d.EducationLevel) AS EducationLevelText,
        dbo.getDisplayMasterdata(d.Maritalstatus) AS MaritalstatusText,
        dbo.getDisplayMasterdata(d.Religion) AS ReligionText,
        CONVERT(varchar(10), d.Dob, 103) AS DobDisplay,
        CONVERT(varchar(10), d.Onboard, 103) AS OnboardDisplay,
        CONVERT(varchar(10), d.NationalDate, 103) AS NationalDateDisplay,
        gm.GroupId,
        g.Name AS GroupName
    FROM Employees d
    LEFT JOIN GroupMember gm ON d.Id = gm.MemberId AND ISNULL(gm.Deleted, 0) = 0
    LEFT JOIN [Group] g ON gm.GroupId = g.Id AND ISNULL(g.Deleted, 0) = 0 ';

    IF (@Token IS NOT NULL AND @Token <> '')
    BEGIN
        SET @where += ' AND (d.UserName LIKE N''%'' + @Token + ''%'' OR d.FullName LIKE N''%'' + @Token + ''%'' OR d.Phone LIKE N''%'' + @Token + ''%'') ';
    END

    IF (@GroupId > 0)
    BEGIN
        SET @where += ' AND gm.GroupId = @GroupId ';
    END

    SET @where += ' AND ISNULL(d.Deleted, 0) = 0 ';

    IF (@Status > -1)
    BEGIN
        SET @where += ' AND d.IsActive = @Status ';
    END

    IF (@StatusWork IS NOT NULL AND @StatusWork <> '' AND @StatusWork <> '-1')
    BEGIN
        SET @where += ' AND d.StatusWork = @StatusWork ';
    END

    IF (@DocumentStatus IS NOT NULL AND @DocumentStatus <> '' AND @DocumentStatus <> '-1')
    BEGIN
        SET @where += ' AND d.DocumentStatus = @DocumentStatus ';
    END

    IF (@fromDate IS NOT NULL)
    BEGIN
        SET @where += ' AND d.CreateAt >= @fromDate ';
    END

    IF (@toDate IS NOT NULL)
    BEGIN
        SET @where += ' AND d.CreateAt <= @toDate ';
    END

    IF (@UserId > 0 AND ISNULL(@RoleCode, '') NOT IN ('1', '8', '9'))
    BEGIN
        SET @where += ' AND d.Id IN (SELECT Id FROM getAllUserByUserId(@UserId)) ';
        SET @where += ' AND d.Id <> @UserId ';
    END

    SET @where += ' ORDER BY ';
    IF (@OrderBy IS NOT NULL AND @OrderBy <> '')
    BEGIN
        SET @where += @OrderBy;
    END
    ELSE
    BEGIN
        SET @where += ' d.UpdateAt DESC ';
    END

    SET @where += ' OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY ';

    SET @mainClause = @mainClause + @where;

    SET @params = N'@offset int, @limit int, @fromDate DATETIME2, @toDate DATETIME2, @Status int, @UserId int, @RoleCode nvarchar(50), @Token nvarchar(255), @GroupId int, @StatusWork nvarchar(50), @DocumentStatus nvarchar(50)';

    EXECUTE sp_executesql @mainClause, @params,
        @offset = @offset,
        @limit = @limit,
        @fromDate = @fromDate,
        @toDate = @toDate,
        @Status = @Status,
        @UserId = @UserId,
        @RoleCode = @RoleCode,
        @Token = @Token,
        @GroupId = @GroupId,
        @StatusWork = @StatusWork,
        @DocumentStatus = @DocumentStatus;
END
GO

PRINT '  Procedure sp_Employee_getAll_Extended updated successfully (HCNS full access)';
GO
