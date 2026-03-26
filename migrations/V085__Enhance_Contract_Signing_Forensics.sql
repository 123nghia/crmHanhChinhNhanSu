-- =============================================
-- Migration: V085__Enhance_Contract_Signing_Forensics
-- Description: Add forensics columns for contract signing (IP, UA, archive, signature image, etc.)
--              Add OriginalFileHash for upload-time tamper detection
--              Add email templates for signing notifications
-- =============================================

PRINT 'Applying migration V085: enhance contract signing forensics...';

-- =======================================================
-- 1. Contracts table: Employee sign forensics
-- =======================================================
IF COL_LENGTH('dbo.Contracts', 'SignedIpAddress') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD SignedIpAddress nvarchar(100) NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'SignedUserAgent') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD SignedUserAgent nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'SignedFileArchivePath') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD SignedFileArchivePath nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'SignatureImagePath') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD SignatureImagePath nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'SignatureIntentText') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD SignatureIntentText nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'SignedByUserNameSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD SignedByUserNameSnapshot nvarchar(100) NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'SignedByFullNameSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD SignedByFullNameSnapshot nvarchar(250) NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'TermsAcceptedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD TermsAcceptedAt datetime NULL;
END
GO

-- =======================================================
-- 2. Contracts table: HR sign forensics
-- =======================================================
IF COL_LENGTH('dbo.Contracts', 'HrSignedIpAddress') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD HrSignedIpAddress nvarchar(100) NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'HrSignedUserAgent') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD HrSignedUserAgent nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'HrSignedByUserNameSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD HrSignedByUserNameSnapshot nvarchar(100) NULL;
END
GO

IF COL_LENGTH('dbo.Contracts', 'HrSignedByFullNameSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD HrSignedByFullNameSnapshot nvarchar(250) NULL;
END
GO

-- =======================================================
-- 3. Contracts table: OriginalFileHash for tamper detection
-- =======================================================
IF COL_LENGTH('dbo.Contracts', 'OriginalFileHash') IS NULL
BEGIN
    ALTER TABLE dbo.Contracts ADD OriginalFileHash nvarchar(128) NULL;
END
GO

-- =======================================================
-- 4. ContractHistory table: Employee sign forensics
-- =======================================================
IF COL_LENGTH('dbo.ContractHistory', 'SignedIpAddress') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD SignedIpAddress nvarchar(100) NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'SignedUserAgent') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD SignedUserAgent nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'SignedFileArchivePath') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD SignedFileArchivePath nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'SignatureImagePath') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD SignatureImagePath nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'SignatureIntentText') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD SignatureIntentText nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'SignedByUserNameSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD SignedByUserNameSnapshot nvarchar(100) NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'SignedByFullNameSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD SignedByFullNameSnapshot nvarchar(250) NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'TermsAcceptedAt') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD TermsAcceptedAt datetime NULL;
END
GO

-- =======================================================
-- 5. ContractHistory table: HR sign forensics
-- =======================================================
IF COL_LENGTH('dbo.ContractHistory', 'HrSignedIpAddress') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD HrSignedIpAddress nvarchar(100) NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'HrSignedUserAgent') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD HrSignedUserAgent nvarchar(500) NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'HrSignedByUserNameSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD HrSignedByUserNameSnapshot nvarchar(100) NULL;
END
GO

IF COL_LENGTH('dbo.ContractHistory', 'HrSignedByFullNameSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.ContractHistory ADD HrSignedByFullNameSnapshot nvarchar(250) NULL;
END
GO

-- =======================================================
-- 6. SP: sp_Contract_setOriginalFileHash
-- =======================================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Contract_setOriginalFileHash]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Contract_setOriginalFileHash];
GO

CREATE PROCEDURE [dbo].[sp_Contract_setOriginalFileHash]
(
    @Id int,
    @OriginalFileHash nvarchar(128)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Contracts
    SET OriginalFileHash = @OriginalFileHash
    WHERE Id = @Id
      AND ISNULL(Deleted, 0) = 0
      AND OriginalFileHash IS NULL;

    SELECT @@ROWCOUNT;
END
GO

-- =======================================================
-- 7. SP: sp_Contract_signInternal (updated with forensics)
-- =======================================================
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

    UPDATE Contracts
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
      AND ISNULL(IsHrSigned, 0) = 1
      AND ISNULL(IsSignedInternal, 0) = 0;

    SELECT @@ROWCOUNT;
END
GO

-- =======================================================
-- 8. SP: sp_Contract_signInternalHr (updated with forensics)
-- =======================================================
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
    @SignMethod nvarchar(50) = N'INTERNAL_SHA256',
    @HrSignedByUserNameSnapshot nvarchar(100) = NULL,
    @HrSignedByFullNameSnapshot nvarchar(250) = NULL,
    @HrSignedIpAddress nvarchar(100) = NULL,
    @HrSignedUserAgent nvarchar(500) = NULL
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
        HrSignedByUserNameSnapshot = @HrSignedByUserNameSnapshot,
        HrSignedByFullNameSnapshot = @HrSignedByFullNameSnapshot,
        HrSignedIpAddress = @HrSignedIpAddress,
        HrSignedUserAgent = @HrSignedUserAgent,
        UpdatedBy = @SignedBy,
        UpdateAt = GETDATE()
    WHERE Id = @Id
      AND ISNULL(Deleted, 0) = 0
      AND ISNULL(IsSignedInternal, 0) = 0
      AND ISNULL(IsHrSigned, 0) = 0;

    SELECT @@ROWCOUNT;
END
GO

-- =======================================================
-- 9. SP: sp_Contract_getAll (updated)
-- =======================================================
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
        c.OriginalFileHash,
        c.IsHrSigned,
        c.HrSignedAt,
        c.HrSignedBy,
        hrSigner.UserName AS HrSignedByUserName,
        hrSigner.FullName AS HrSignedByFullName,
        c.HrSignatureHash,
        c.HrFileHash,
        c.HrSignNote,
        c.HrSignMethod,
        c.HrSignedIpAddress,
        c.HrSignedUserAgent,
        c.HrSignedByUserNameSnapshot,
        c.HrSignedByFullNameSnapshot,
        c.IsSignedInternal,
        c.SignedAt,
        c.SignedBy,
        signer.UserName AS SignedByUserName,
        signer.FullName AS SignedByFullName,
        c.SignatureHash,
        c.FileHash,
        c.SignNote,
        c.SignMethod,
        c.SignedIpAddress,
        c.SignedUserAgent,
        c.SignedFileArchivePath,
        c.SignatureImagePath,
        c.SignatureIntentText,
        c.SignedByUserNameSnapshot,
        c.SignedByFullNameSnapshot,
        c.TermsAcceptedAt,
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

-- =======================================================
-- 10. SP: sp_Contract_getById (updated)
-- =======================================================
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

-- =======================================================
-- 11. SP: sp_ContractHistory_insert (updated)
-- =======================================================
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
    @HrSignedIpAddress nvarchar(100) = NULL,
    @HrSignedUserAgent nvarchar(500) = NULL,
    @HrSignedByUserNameSnapshot nvarchar(100) = NULL,
    @HrSignedByFullNameSnapshot nvarchar(250) = NULL,
    @IsSignedInternal bit = NULL,
    @SignedAt datetime = NULL,
    @SignedBy int = NULL,
    @SignatureHash nvarchar(128) = NULL,
    @FileHash nvarchar(128) = NULL,
    @SignNote nvarchar(500) = NULL,
    @SignMethod nvarchar(50) = NULL,
    @SignedIpAddress nvarchar(100) = NULL,
    @SignedUserAgent nvarchar(500) = NULL,
    @SignedFileArchivePath nvarchar(500) = NULL,
    @SignatureImagePath nvarchar(500) = NULL,
    @SignatureIntentText nvarchar(500) = NULL,
    @SignedByUserNameSnapshot nvarchar(100) = NULL,
    @SignedByFullNameSnapshot nvarchar(250) = NULL,
    @TermsAcceptedAt datetime = NULL,
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
        HrSignedIpAddress, HrSignedUserAgent, HrSignedByUserNameSnapshot, HrSignedByFullNameSnapshot,
        IsSignedInternal, SignedAt, SignedBy, SignatureHash, FileHash, SignNote, SignMethod,
        SignedIpAddress, SignedUserAgent, SignedFileArchivePath, SignatureImagePath,
        SignatureIntentText, SignedByUserNameSnapshot, SignedByFullNameSnapshot, TermsAcceptedAt,
        CreateAt, CreatedBy
    )
    VALUES
    (
        @ContractId, @EmployeeId, @Action, @ContractTypeCode,
        @StartDate, @EndDate, @Status, @FileUrl, @Note,
        @IsHrSigned, @HrSignedAt, @HrSignedBy, @HrSignatureHash, @HrFileHash, @HrSignNote, @HrSignMethod,
        @HrSignedIpAddress, @HrSignedUserAgent, @HrSignedByUserNameSnapshot, @HrSignedByFullNameSnapshot,
        @IsSignedInternal, @SignedAt, @SignedBy, @SignatureHash, @FileHash, @SignNote, @SignMethod,
        @SignedIpAddress, @SignedUserAgent, @SignedFileArchivePath, @SignatureImagePath,
        @SignatureIntentText, @SignedByUserNameSnapshot, @SignedByFullNameSnapshot, @TermsAcceptedAt,
        GETDATE(), @CreatedBy
    );
END
GO

-- =======================================================
-- 12. SP: sp_ContractHistory_getByContract (updated)
-- =======================================================
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
        h.HrSignedIpAddress,
        h.HrSignedUserAgent,
        h.HrSignedByUserNameSnapshot,
        h.HrSignedByFullNameSnapshot,
        h.IsSignedInternal,
        h.SignedAt,
        h.SignedBy,
        signer.UserName AS SignedByUserName,
        signer.FullName AS SignedByFullName,
        h.SignatureHash,
        h.FileHash,
        h.SignNote,
        h.SignMethod,
        h.SignedIpAddress,
        h.SignedUserAgent,
        h.SignedFileArchivePath,
        h.SignatureImagePath,
        h.SignatureIntentText,
        h.SignedByUserNameSnapshot,
        h.SignedByFullNameSnapshot,
        h.TermsAcceptedAt,
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

-- =======================================================
-- 13. Email templates for signing notifications
-- =======================================================
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE Code = 'SIGN_CONTRACT_HR_DONE')
BEGIN
    INSERT INTO EmailTemplates (Code, Name, Subject, Body, IsActive, SenderType, CcManager)
    VALUES (
        'SIGN_CONTRACT_HR_DONE',
        N'Thong bao HR da ky hop dong',
        N'[HR] Hop dong cua ban da duoc HR ky noi bo',
        N'<p>Xin chao <strong>{{EmployeeFullName}}</strong>,</p><p>Hop dong lao dong cua ban (loai: <strong>{{ContractType}}</strong>) da duoc HR ky noi bo thanh cong.</p><p>Ngay ky HR: <strong>{{HrSignedAt}}</strong></p><p>Nguoi ky HR: <strong>{{HrSignerName}}</strong></p><p>Vui long dang nhap he thong de xem va ky xac nhan hop dong.</p><p>{{SenderSignature}}</p>',
        1,
        'HR',
        0
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE Code = 'SIGN_DOCUMENT_REQUESTED')
BEGIN
    INSERT INTO EmailTemplates (Code, Name, Subject, Body, IsActive, SenderType, CcManager)
    VALUES (
        'SIGN_DOCUMENT_REQUESTED',
        N'Yeu cau ky tai lieu noi bo',
        N'[HR] Ban co tai lieu can ky xac nhan',
        N'<p>Xin chao <strong>{{EmployeeFullName}}</strong>,</p><p>Ban co tai lieu <strong>{{DocumentName}}</strong> can ky xac nhan noi bo.</p><p>Ngay yeu cau: <strong>{{RequestedAt}}</strong></p><p>Nguoi yeu cau: <strong>{{RequestedBy}}</strong></p><p>Vui long dang nhap he thong de xem va ky xac nhan tai lieu.</p><p>{{SenderSignature}}</p>',
        1,
        'HR',
        0
    );
END
GO

PRINT 'Migration V085 completed successfully.';
