-- Migration V023: Fix sp_DocumentData_getAll root folder navigation
-- Description: Fixes NULL comparison bug for ParentId

GO
/****** Object:  StoredProcedure [dbo].[sp_DocumentData_getAll] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_DocumentData_getAll]
(
    @Token nvarchar(30) = '',
    @OrderBy varchar(30) = '',
    @RelId int = -1,
    @RelCode varchar(10) = '',
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
      AND (@RelId = -1 OR d.RelId = @RelId)
      AND (@RelCode = '' OR d.RelCode = @RelCode)
      AND (@DataType = 0 OR d.dataType = @DataType)
      AND (
          @ParentId = -2
          OR (@ParentId = -1 AND d.ParentId IS NULL)
          OR (d.ParentId = @ParentId)
      )
      AND (
          d.CreatedBy = @CurrentUserId
          OR d.AccessLevel = 1
          OR (d.AccessLevel = 2 AND EXISTS (SELECT 1 FROM DocumentShares s WHERE s.DocumentId = d.Id AND s.UserId = @CurrentUserId))
      )
    ORDER BY d.IsFolder DESC, d.UpdateAt DESC, d.CreateAt DESC
    OFFSET @offset ROWS FETCH NEXT @Limit ROWS ONLY;
END
GO
