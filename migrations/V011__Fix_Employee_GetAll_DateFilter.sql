-- =============================================
-- Migration: V011__Fix_Employee_GetAll_DateFilter
-- Author: System
-- Date: 2026-01-04
-- Description: Fix date filter and IsDeleted semantics in sp_Employee_getAll
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V011: Fix Employee GetAll DateFilter...';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('sp_Employee_getAll') AND type = 'P')
BEGIN
    PRINT '  Dropping existing sp_Employee_getAll...';
    DROP PROCEDURE sp_Employee_getAll;
END

PRINT '  Creating sp_Employee_getAll...';
EXEC(N'
CREATE PROCEDURE [dbo].[sp_Employee_getAll]
(
    @offset INT = 0,
    @limit INT = 20,
    @fromDate DATETIME2 = NULL,
    @toDate DATETIME2 = NULL,
    @Status INT = NULL,
    @Token NVARCHAR(500) = NULL,
    @GroupId INT = 0,
    @MemberId INT = 0,
    @IsDeleted BIT = 0,
    @userId INT = -1,
    @OrderBy NVARCHAR(50) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @where NVARCHAR(MAX) = '' WHERE 1=1 '';
    DECLARE @mainClause NVARCHAR(MAX);
    DECLARE @params NVARCHAR(MAX);

    SET @mainClause = N''
        SELECT
            COUNT(1) OVER() AS TotalRecord,
            d.*,
            dbo.getDisplayMasterData(d.Status) as StatusText,
            dbo.getDisplayMasterData(d.DocumentStatus) as DocumentStatusText,
            dbo.getDisplayMasterData(d.DepartmentCode) as DepartmentText,
            dbo.getDisplayMasterData(d.PositionCode) as PositionText,
            dbo.getDisplayMasterData(d.Religion) as ReligionText,
            dbo.getDisplayMasterData(d.EducationLevel) as EducationLevelText,
            dbo.getDisplayMasterData(d.Maritalstatus) as MaritalstatusText,
            dbo.getFullName(d.ManagerId) as GroupName
        FROM Employees d '';

    IF (ISNULL(@IsDeleted, 0) < 1)
    BEGIN
        SET @where += '' AND ISNULL(d.Deleted, 0) = 0 '';
    END

    IF (@Token IS NOT NULL AND @Token <> '''')
    BEGIN
        SET @where += '' AND (d.UserName LIKE N''''%'' + @Token + ''%''''
                           OR d.FullName LIKE N''''%'' + @Token + ''%''''
                           OR d.Phone LIKE N''''%'' + @Token + ''%'''')'';
    END

    IF (@Status IS NOT NULL AND @Status > -1)
    BEGIN
        SET @where += '' AND d.IsActive = @Status '';
    END

    IF (@fromDate IS NOT NULL)
        SET @where += '' AND d.CreateAt >= @fromDate '';

    IF (@toDate IS NOT NULL)
        SET @where += '' AND d.CreateAt <= @toDate '';

    IF (@userId > 0)
    BEGIN
        SET @where += '' AND d.Id IN (SELECT Id FROM getAllUserByUserId(@userId))'';
    END

    IF (@OrderBy IS NULL OR @OrderBy = '''')
        SET @where += '' ORDER BY d.UpdateAt DESC'';
    ELSE
        SET @where += '' ORDER BY '' + @OrderBy;

    SET @mainClause = @mainClause + @where + '' OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY'';

    SET @params = N''@offset INT, @limit INT, @fromDate DATETIME2, @toDate DATETIME2, @Status INT, @Token NVARCHAR(500), @userId INT'';

    EXEC sp_executesql
        @mainClause,
        @params,
        @offset = @offset,
        @limit = @limit,
        @fromDate = @fromDate,
        @toDate = @toDate,
        @Status = @Status,
        @Token = @Token,
        @userId = @userId;
END
');

PRINT 'Migration V011 completed successfully';

COMMIT TRANSACTION;
