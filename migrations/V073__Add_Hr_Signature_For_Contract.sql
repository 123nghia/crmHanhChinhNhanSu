-- =============================================
-- Migration: V073__Add_Hr_Signature_For_Contract
-- Description: Add HR signature stage for 2-step contract signing
-- =============================================

PRINT 'Applying migration V073: add HR signature stage for contracts...';

-- 1. Add HR signature columns to Contracts
IF COL_LENGTH('dbo.Contracts', 'IsHrSigned') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts
    ADD IsHrSigned bit NOT NULL CONSTRAINT DF_Contracts_IsHrSigned DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.Contracts', 'HrSignedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD HrSignedAt datetime NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'HrSignedBy') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD HrSignedBy int NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'HrSignatureHash') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD HrSignatureHash nvarchar(128) NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'HrFileHash') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD HrFileHash nvarchar(128) NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'HrSignNote') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD HrSignNote nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'HrSignMethod') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD HrSignMethod nvarchar(50) NULL;
END
GO

-- 2. Add HR signature columns to ContractHistory
IF COL_LENGTH('dbo.ContractHistory', 'IsHrSigned') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD IsHrSigned bit NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'HrSignedAt') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD HrSignedAt datetime NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'HrSignedBy') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD HrSignedBy int NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'HrSignatureHash') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD HrSignatureHash nvarchar(128) NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'HrFileHash') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD HrFileHash nvarchar(128) NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'HrSignNote') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD HrSignNote nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'HrSignMethod') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD HrSignMethod nvarchar(50) NULL;
END
GO

-- 3. Refresh stored procedures with HR signature fields
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Contract_getAll]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Contract_getAll];
GO

CREATE PROCEDURE [dbo].[sp_Contract_getAll]
(
    @Token nvarchar(255) = '',
    @Status nvarchar(50) = NULL,
    @ContractTypeCode nvarchar(50) = NULL,
    @EmployeeId int = NULL,
    @FromDate datetime = NULL,
    @ToDate datetime = NULL,
    @offset int = 0,
    @limit int = 50
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(1) OVER() AS TotalRecord,
        c.Id,
        c.EmployeeId,
        e.FullName,
        e.UserName,
        c.ContractTypeCode,
        md.Name AS ContractTypeName,
        c.StartDate,
        c.EndDate,
        c.Status,
        c.FileUrl,
        c.Note,
        c.IsHrSigned,
        c.HrSignedAt,
        c.HrSignedBy,
        hrSigner.UserName AS HrSignedByUserName,
        hrSigner.FullName AS HrSignedByFullName,
        c.HrSignatureHash,
        c.HrFileHash,
        c.HrSignNote,
        c.HrSignMethod,
        c.IsSignedInternal,
        c.SignedAt,
        c.SignedBy,
        signer.UserName AS SignedByUserName,
        signer.FullName AS SignedByFullName,
        c.SignatureHash,
        c.FileHash,
        c.SignNote,
        c.SignMethod,
        c.CreateAt,
        c.UpdateAt
    FROM Contracts c
    INNER JOIN Employees e ON c.EmployeeId = e.Id
    LEFT JOIN MasterData md ON c.ContractTypeCode = md.Code AND md.TypeData = 1
    LEFT JOIN Employees signer ON c.SignedBy = signer.Id
    LEFT JOIN Employees hrSigner ON c.HrSignedBy = hrSigner.Id
    WHERE ISNULL(c.Deleted, 0) = 0
      AND (@EmployeeId IS NULL OR c.EmployeeId = @EmployeeId)
      AND (@ContractTypeCode IS NULL OR c.ContractTypeCode = @ContractTypeCode)
      AND (@Status IS NULL OR c.Status = @Status)
      AND (@FromDate IS NULL OR c.StartDate >= @FromDate)
      AND (@ToDate IS NULL OR c.StartDate <= @ToDate)
      AND (@Token = '' OR e.UserName LIKE N'%' + @Token + '%' OR e.FullName LIKE N'%' + @Token + '%')
    ORDER BY c.UpdateAt DESC, c.Id DESC
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Contract_getById]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Contract_getById];
GO

CREATE PROCEDURE [dbo].[sp_Contract_getById]
(
    @Id int
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        c.*,
        e.FullName,
        e.UserName,
        md.Name AS ContractTypeName,
        signer.UserName AS SignedByUserName,
        signer.FullName AS SignedByFullName,
        hrSigner.UserName AS HrSignedByUserName,
        hrSigner.FullName AS HrSignedByFullName
    FROM Contracts c
    INNER JOIN Employees e ON c.EmployeeId = e.Id
    LEFT JOIN MasterData md ON c.ContractTypeCode = md.Code AND md.TypeData = 1
    LEFT JOIN Employees signer ON c.SignedBy = signer.Id
    LEFT JOIN Employees hrSigner ON c.HrSignedBy = hrSigner.Id
    WHERE c.Id = @Id AND ISNULL(c.Deleted, 0) = 0;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ContractHistory_insert]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_ContractHistory_insert];
GO

CREATE PROCEDURE [dbo].[sp_ContractHistory_insert]
(
    @ContractId int,
    @EmployeeId int,
    @Action nvarchar(50),
    @ContractTypeCode nvarchar(50) = NULL,
    @StartDate datetime = NULL,
    @EndDate datetime = NULL,
    @Status nvarchar(50) = NULL,
    @FileUrl nvarchar(500) = NULL,
    @Note nvarchar(500) = NULL,
    @IsHrSigned bit = NULL,
    @HrSignedAt datetime = NULL,
    @HrSignedBy int = NULL,
    @HrSignatureHash nvarchar(128) = NULL,
    @HrFileHash nvarchar(128) = NULL,
    @HrSignNote nvarchar(500) = NULL,
    @HrSignMethod nvarchar(50) = NULL,
    @IsSignedInternal bit = NULL,
    @SignedAt datetime = NULL,
    @SignedBy int = NULL,
    @SignatureHash nvarchar(128) = NULL,
    @FileHash nvarchar(128) = NULL,
    @SignNote nvarchar(500) = NULL,
    @SignMethod nvarchar(50) = NULL,
    @CreatedBy int = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ContractHistory
    (
        ContractId, EmployeeId, Action, ContractTypeCode,
        StartDate, EndDate, Status, FileUrl, Note,
        IsHrSigned, HrSignedAt, HrSignedBy, HrSignatureHash, HrFileHash, HrSignNote, HrSignMethod,
        IsSignedInternal, SignedAt, SignedBy, SignatureHash, FileHash, SignNote, SignMethod,
        CreateAt, CreatedBy
    )
    VALUES
    (
        @ContractId, @EmployeeId, @Action, @ContractTypeCode,
        @StartDate, @EndDate, @Status, @FileUrl, @Note,
        @IsHrSigned, @HrSignedAt, @HrSignedBy, @HrSignatureHash, @HrFileHash, @HrSignNote, @HrSignMethod,
        @IsSignedInternal, @SignedAt, @SignedBy, @SignatureHash, @FileHash, @SignNote, @SignMethod,
        GETDATE(), @CreatedBy
    );
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ContractHistory_getByContract]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_ContractHistory_getByContract];
GO

CREATE PROCEDURE [dbo].[sp_ContractHistory_getByContract]
(
    @ContractId int
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        h.Id,
        h.ContractId,
        h.EmployeeId,
        h.Action,
        h.ContractTypeCode,
        md.Name AS ContractTypeName,
        h.StartDate,
        h.EndDate,
        h.Status,
        h.FileUrl,
        h.Note,
        h.IsHrSigned,
        h.HrSignedAt,
        h.HrSignedBy,
        hrSigner.UserName AS HrSignedByUserName,
        hrSigner.FullName AS HrSignedByFullName,
        h.HrSignatureHash,
        h.HrFileHash,
        h.HrSignNote,
        h.HrSignMethod,
        h.IsSignedInternal,
        h.SignedAt,
        h.SignedBy,
        signer.UserName AS SignedByUserName,
        signer.FullName AS SignedByFullName,
        h.SignatureHash,
        h.FileHash,
        h.SignNote,
        h.SignMethod,
        h.CreateAt,
        h.CreatedBy
    FROM ContractHistory h
    LEFT JOIN MasterData md ON h.ContractTypeCode = md.Code AND md.TypeData = 1
    LEFT JOIN Employees signer ON h.SignedBy = signer.Id
    LEFT JOIN Employees hrSigner ON h.HrSignedBy = hrSigner.Id
    WHERE h.ContractId = @ContractId
    ORDER BY h.CreateAt DESC, h.Id DESC;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Contract_signInternal]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Contract_signInternal];
GO

CREATE PROCEDURE [dbo].[sp_Contract_signInternal]
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

    UPDATE Contracts
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
      AND ISNULL(IsHrSigned, 0) = 1
      AND ISNULL(IsSignedInternal, 0) = 0;

    SELECT @@ROWCOUNT;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Contract_signInternalHr]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Contract_signInternalHr];
GO

CREATE PROCEDURE [dbo].[sp_Contract_signInternalHr]
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

    UPDATE Contracts
    SET
        IsHrSigned = 1,
        HrSignedAt = ISNULL(@SignedAt, GETDATE()),
        HrSignedBy = @SignedBy,
        HrSignatureHash = @SignatureHash,
        HrFileHash = @FileHash,
        HrSignNote = @SignNote,
        HrSignMethod = @SignMethod,
        UpdatedBy = @SignedBy,
        UpdateAt = GETDATE()
    WHERE Id = @Id
      AND ISNULL(Deleted, 0) = 0
      AND ISNULL(IsSignedInternal, 0) = 0
      AND ISNULL(IsHrSigned, 0) = 0;

    SELECT @@ROWCOUNT;
END
GO

PRINT 'Migration V073 completed successfully.';
