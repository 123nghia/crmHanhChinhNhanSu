-- =============================================
-- Migration: V072__Add_Internal_Signature_For_DocumentData
-- Description: Add internal signature support for all DocumentData files
-- =============================================

PRINT 'Applying migration V072: add internal signature for DocumentData...';

IF COL_LENGTH('dbo.DocumentData', 'IsSignedInternal') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData
    ADD IsSignedInternal bit NOT NULL CONSTRAINT DF_DocumentData_IsSignedInternal DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignedAt') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignedAt datetime NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignedBy') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignedBy int NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignatureHash') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignatureHash nvarchar(128) NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'FileHash') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD FileHash nvarchar(128) NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignNote') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignNote nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignMethod') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignMethod nvarchar(50) NULL;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_getAll]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_getAll];
GO

CREATE PROCEDURE [dbo].[sp_DocumentData_getAll]
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

    SELECT
        count(d.id) over() as TotalRecord,
        d.*,
        u.FullName as AuthorName,
        signer.UserName as SignedByUserName,
        signer.FullName as SignedByFullName
    FROM DocumentData d
    LEFT JOIN Employees u ON d.CreatedBy = u.Id
    LEFT JOIN Employees signer ON d.SignedBy = signer.Id
    WHERE (ISNULL(d.Deleted, 0) = 0)
      AND (@RelId IS NULL OR @RelId = -1 OR d.RelId = @RelId)
      AND (@RelCode IS NULL OR @RelCode = '' OR d.RelCode = @RelCode)
      AND (@DataType = 0 OR d.dataType = @DataType)
      AND (
          @ParentId = -2
          OR (@ParentId = -1 AND d.ParentId IS NULL)
          OR (d.ParentId = @ParentId)
      )
      AND (
          (@RelId IS NOT NULL AND @RelId > 0)
          OR d.CreatedBy = @CurrentUserId
          OR d.AccessLevel = 1
          OR (d.AccessLevel = 2 AND EXISTS (
              SELECT 1 FROM DocumentShares s WHERE s.DocumentId = d.Id AND s.UserId = @CurrentUserId
          ))
      )
    ORDER BY d.IsFolder DESC, d.UpdateAt DESC, d.CreateAt DESC
    OFFSET @offset ROWS FETCH NEXT @Limit ROWS ONLY;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_GetById]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_GetById];
GO

CREATE PROCEDURE [dbo].[sp_DocumentData_GetById]
(
    @Id int
)
AS
BEGIN
    SELECT
        d.*,
        u.FullName as AuthorName,
        signer.UserName as SignedByUserName,
        signer.FullName as SignedByFullName
    FROM DocumentData d
    LEFT JOIN Employees u ON d.CreatedBy = u.Id
    LEFT JOIN Employees signer ON d.SignedBy = signer.Id
    WHERE d.Id = @Id;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_signInternal]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_signInternal];
GO

CREATE PROCEDURE [dbo].[sp_DocumentData_signInternal]
(
    @Id int,
    @SignedBy int,
    @SignedAt datetime = NULL,
    @SignatureHash nvarchar(128),
    @FileHash nvarchar(128),
    @SignNote nvarchar(500) = NULL,
    @SignMethod nvarchar(50) = N'INTERNAL_SHA256'
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE DocumentData
    SET
        IsSignedInternal = 1,
        SignedAt = ISNULL(@SignedAt, GETDATE()),
        SignedBy = @SignedBy,
        SignatureHash = @SignatureHash,
        FileHash = @FileHash,
        SignNote = @SignNote,
        SignMethod = @SignMethod,
        UpdatedBy = @SignedBy,
        UpdateAt = GETDATE()
    WHERE Id = @Id
      AND ISNULL(Deleted, 0) = 0
      AND ISNULL(IsFolder, 0) = 0
      AND ISNULL(IsSignedInternal, 0) = 0;

    SELECT @@ROWCOUNT;
END
GO

PRINT 'Migration V072 completed successfully.';
