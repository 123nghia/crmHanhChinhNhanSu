-- =============================================
-- Migration: V048__Fix_DocumentData_Admin_View
-- Author: System
-- Date: 2026-01-16
-- Description: Fix sp_DocumentData_getAll to allow admin to view candidate documents
--              by skipping CreatedBy check when RelId is specified
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V048: Fix sp_DocumentData_getAll for admin view...';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_getAll]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_getAll];

EXEC(N'
CREATE PROCEDURE [dbo].[sp_DocumentData_getAll]
(
    @Token nvarchar(30) = '''',
    @OrderBy varchar(30) = '''',
    @RelId int = -1,
    @RelCode varchar(10) = '''',
    @DataType int = 0,
    @ParentId int = -1,
    @Page int = 1,
    @Limit int = 1000,
    @CurrentUserId int = -1
)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @offset int = (@Page - 1) * @Limit;

    SELECT count(d.id) over() as TotalRecord,
           d.*,
           u.FullName as AuthorName
    FROM DocumentData d
    LEFT JOIN Employees u ON d.CreatedBy = u.Id
    WHERE (ISNULL(d.Deleted, 0) = 0)
      AND (@RelId IS NULL OR @RelId = -1 OR d.RelId = @RelId)
      AND (@RelCode IS NULL OR @RelCode = '''' OR d.RelCode = @RelCode)
      AND (@DataType = 0 OR d.dataType = @DataType)
      AND (
          @ParentId = -2
          OR (@ParentId = -1 AND d.ParentId IS NULL)
          OR (d.ParentId = @ParentId)
      )
      -- If a specific RelId is provided (not NULL and not -1), 
      -- skip the CreatedBy/AccessLevel check to allow admin view
      AND (
          (@RelId IS NOT NULL AND @RelId > 0)  -- Admin viewing by RelId - skip permission check
          OR d.CreatedBy = @CurrentUserId      -- User created the document
          OR d.AccessLevel = 1                 -- Public document
          OR (d.AccessLevel = 2 AND EXISTS (   -- Shared document
              SELECT 1 FROM DocumentShares s WHERE s.DocumentId = d.Id AND s.UserId = @CurrentUserId
          ))
      )
    ORDER BY d.IsFolder DESC, d.UpdateAt DESC, d.CreateAt DESC
    OFFSET @offset ROWS FETCH NEXT @Limit ROWS ONLY;
END
');

PRINT 'Migration V048 completed successfully.';

COMMIT TRANSACTION;
