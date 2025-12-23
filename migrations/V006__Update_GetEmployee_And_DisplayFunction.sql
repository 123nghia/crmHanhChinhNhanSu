-- =============================================
-- Migration: V006__Update_GetEmployee_And_DisplayFunction
-- Author: System
-- Date: 2025-12-23
-- Description: 1. Ensure getDisplayMasterData function exists and works correctly
--              2. Update sp_Employee_getAll to select display text for new coded fields
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V006: Update GetEmployee And DisplayFunction...';

-- ============================================
-- 1. Create/Update getDisplayMasterData function
-- ============================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getDisplayMasterData]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  → Function getDisplayMasterData exists. Dropping to recreate/update matches...';
    DROP FUNCTION [dbo].[getDisplayMasterData];
END

PRINT '  → Creating function getDisplayMasterData...';
EXEC('
CREATE FUNCTION [dbo].[getDisplayMasterData]
(
    @Code NVARCHAR(50)
)
RETURNS NVARCHAR(255)
AS
BEGIN
    DECLARE @Result NVARCHAR(255);
    
    -- Try to match by Code first
    SELECT TOP 1 @Result = Name 
    FROM MasterData 
    WHERE Code = @Code AND ISNULL(Deleted, 0) = 0;

    -- If no match by Code, it might be a direct ID or needs TypeData context, 
    -- but this function is generic. If @Code is not found, return the Code itself (fallback) or NULL
    
    RETURN ISNULL(@Result, @Code);
END
');
PRINT '  ✓ getDisplayMasterData function updated.';

-- ============================================
-- 2. Update sp_Employee_getAll
-- ============================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('sp_Employee_getAll') AND type = 'P')
BEGIN
    PRINT '  → Updating sp_Employee_getAll...';
    
    -- We use ALTER here or DROP/CREATE. Using EXEC string for robust script.
    DROP PROCEDURE sp_Employee_getAll;
END

PRINT '  → Creating sp_Employee_getAll with new columns...';
EXEC('
CREATE PROCEDURE [dbo].[sp_Employee_getAll]
(
    @offset INT = 0,
    @limit INT = 20,
    @fromDate DATETIME = NULL,
    @toDate DATETIME = NULL,
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

    DECLARE @where NVARCHAR(MAX) = '' WHERE (ISNULL(d.Deleted, 0) = @IsDeleted) '';
    DECLARE @mainClause NVARCHAR(MAX);
    DECLARE @params NVARCHAR(MAX);
    
    SET @mainClause = N''
    SELECT 
        count(1) OVER() AS TotalRecord,
        d.*,
        dbo.getDisplayMasterData(d.Status) as StatusText,
        dbo.getDisplayMasterData(d.DocumentStatus) as DocumentStatusText,
        dbo.getDisplayMasterData(d.DepartmentCode) as DepartmentText,
        dbo.getDisplayMasterData(d.PositionCode) as PositionText,
        
        -- New Code -> Text mappings
        dbo.getDisplayMasterData(d.Religion) as ReligionText,
        dbo.getDisplayMasterData(d.EducationLevel) as EducationLevelText,
        dbo.getDisplayMasterData(d.Maritalstatus) as MaritalstatusText,
        
        dbo.getFullName(d.ManagerId) as GroupName 
    FROM Employees d '';

    IF (@Token IS NOT NULL)
    BEGIN
        SET @where += '' AND (d.UserName LIKE N''''%'' + @Token + ''%'''' 
                           OR d.FullName LIKE N''''%'' + @Token + ''%''''
                           OR d.Phone LIKE N''''%'' + @Token + ''%'''')'';
    END;

    IF (@IsDeleted < 1)
    BEGIN
        -- Add date filtering logic if needed, copied from original SP context
        -- Assuming original logic filtered by CreateAt if columns exist
        IF (@fromDate IS NOT NULL)
            SET @where += '' AND (d.CreateAt >= @fromDate)''; 
            
        IF (@toDate IS NOT NULL)
            SET @where += '' AND (d.CreateAt <= @toDate)''; 
    END

    IF (@userId > 0)
    BEGIN 
        SET @where += '' AND d.id IN (SELECT id FROM getAllUserByUserId(@userId))''; 
    END 
    
    IF (@OrderBy IS NULL OR @OrderBy = '''')
    BEGIN
        SET @where += '' ORDER BY d.UpdateAt DESC'';
    END
    ELSE
    BEGIN
        SET @where += '' ORDER BY '' + @OrderBy;
    END

    SET @mainClause = @mainClause + @where + '' OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY'';
    
    SET @params = N''@offset INT, @limit INT, @fromDate DATETIME, @toDate DATETIME, @Status INT, @Token NVARCHAR(500), @IsDeleted BIT, @userId INT'';

    EXECUTE sp_executesql @mainClause, @params, 
        @offset = @offset, 
        @limit = @limit,
        @fromDate = @fromDate,
        @toDate = @toDate, 
        @Status = @Status, 
        @Token = @Token,
        @IsDeleted = @IsDeleted,
        @userId = @userId;

END
');

PRINT '  ✓ sp_Employee_getAll updated successfully';

PRINT '✓ Migration V006 completed successfully';

COMMIT TRANSACTION;
