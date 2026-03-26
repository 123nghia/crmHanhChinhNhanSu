-- =============================================
-- Migration: V028__Fix_DocumentData_GetAll_Null_Params
-- Author: System
-- Date: 2026-01-09
-- Description: Allow NULL RelId/RelCode in sp_DocumentData_getAll
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V028: Fix sp_DocumentData_getAll null params...';

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
      AND (
          d.CreatedBy = @CurrentUserId
          OR d.AccessLevel = 1
          OR (d.AccessLevel = 2 AND EXISTS (
              SELECT 1 FROM DocumentShares s WHERE s.DocumentId = d.Id AND s.UserId = @CurrentUserId
          ))
      )
    ORDER BY d.IsFolder DESC, d.UpdateAt DESC, d.CreateAt DESC
    OFFSET @offset ROWS FETCH NEXT @Limit ROWS ONLY;
END
');

PRINT 'Migration V028 completed successfully.';

COMMIT TRANSACTION;
