PRINT 'Applying migration V075: employee document sign request workflow...';

IF COL_LENGTH('dbo.DocumentData', 'IsSignatureRequested') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData
    ADD IsSignatureRequested bit NOT NULL CONSTRAINT DF_DocumentData_IsSignatureRequested DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignatureRequestedAt') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignatureRequestedAt datetime NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignatureRequestedBy') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignatureRequestedBy int NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'TermsAcceptedAt') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD TermsAcceptedAt datetime NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignedByUserNameSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignedByUserNameSnapshot nvarchar(100) NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignedByFullNameSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignedByFullNameSnapshot nvarchar(250) NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignatureIntentText') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignatureIntentText nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignedIpAddress') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignedIpAddress nvarchar(100) NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignedUserAgent') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignedUserAgent nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignedFileArchivePath') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignedFileArchivePath nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.DocumentData', 'SignatureImagePath') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentData ADD SignatureImagePath nvarchar(500) NULL;
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
        signer.FullName as SignedByFullName,
        requester.UserName as SignatureRequestedByUserName,
        requester.FullName as SignatureRequestedByFullName
    FROM DocumentData d
    LEFT JOIN Employees u ON d.CreatedBy = u.Id
    LEFT JOIN Employees signer ON d.SignedBy = signer.Id
    LEFT JOIN Employees requester ON d.SignatureRequestedBy = requester.Id
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
        signer.FullName as SignedByFullName,
        requester.UserName as SignatureRequestedByUserName,
        requester.FullName as SignatureRequestedByFullName
    FROM DocumentData d
    LEFT JOIN Employees u ON d.CreatedBy = u.Id
    LEFT JOIN Employees signer ON d.SignedBy = signer.Id
    LEFT JOIN Employees requester ON d.SignatureRequestedBy = requester.Id
    WHERE d.Id = @Id;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_requestInternalSign]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_requestInternalSign];
GO

CREATE PROCEDURE [dbo].[sp_DocumentData_requestInternalSign]
(
    @Id int,
    @RequestedBy int,
    @RequestedAt datetime = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE DocumentData
    SET
        IsSignatureRequested = 1,
        SignatureRequestedAt = ISNULL(@RequestedAt, GETDATE()),
        SignatureRequestedBy = @RequestedBy,
        UpdatedBy = @RequestedBy,
        UpdateAt = GETDATE()
    WHERE Id = @Id
      AND ISNULL(Deleted, 0) = 0
      AND ISNULL(IsFolder, 0) = 0
      AND ISNULL(IsSignedInternal, 0) = 0
      AND ISNULL(IsSignatureRequested, 0) = 0;

    SELECT @@ROWCOUNT;
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
    @SignMethod nvarchar(50) = N'INTERNAL_SHA256',
    @SignedByUserNameSnapshot nvarchar(100) = NULL,
    @SignedByFullNameSnapshot nvarchar(250) = NULL,
    @SignedIpAddress nvarchar(100) = NULL,
    @SignedUserAgent nvarchar(500) = NULL,
    @SignedFileArchivePath nvarchar(500) = NULL,
    @SignatureImagePath nvarchar(500) = NULL,
    @SignatureIntentText nvarchar(500) = NULL,
    @TermsAcceptedAt datetime = NULL
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
        SignedByUserNameSnapshot = @SignedByUserNameSnapshot,
        SignedByFullNameSnapshot = @SignedByFullNameSnapshot,
        SignedIpAddress = @SignedIpAddress,
        SignedUserAgent = @SignedUserAgent,
        SignedFileArchivePath = @SignedFileArchivePath,
        SignatureImagePath = @SignatureImagePath,
        SignatureIntentText = @SignatureIntentText,
        TermsAcceptedAt = ISNULL(@TermsAcceptedAt, ISNULL(@SignedAt, GETDATE())),
        UpdatedBy = @SignedBy,
        UpdateAt = GETDATE()
    WHERE Id = @Id
      AND ISNULL(Deleted, 0) = 0
      AND ISNULL(IsFolder, 0) = 0
      AND ISNULL(IsSignedInternal, 0) = 0
      AND ISNULL(IsSignatureRequested, 0) = 1;

    SELECT @@ROWCOUNT;
END
GO

PRINT 'Migration V075 completed successfully.';
