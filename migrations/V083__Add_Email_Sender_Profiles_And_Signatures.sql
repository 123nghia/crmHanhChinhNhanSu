-- =============================================
-- Migration: V083__Add_Email_Sender_Profiles_And_Signatures
-- Description: Add HR/Employee sender profiles, signatures, and template sender type
-- =============================================

PRINT 'Applying migration V083: add email sender profiles and signatures...';

IF COL_LENGTH('dbo.EmailSettings', 'HrFromEmail') IS NULL
BEGIN
    ALTER TABLE dbo.EmailSettings ADD HrFromEmail nvarchar(200) NULL;
END
GO

IF COL_LENGTH('dbo.EmailSettings', 'HrFromName') IS NULL
BEGIN
    ALTER TABLE dbo.EmailSettings ADD HrFromName nvarchar(200) NULL;
END
GO

IF COL_LENGTH('dbo.EmailSettings', 'HrSignature') IS NULL
BEGIN
    ALTER TABLE dbo.EmailSettings ADD HrSignature nvarchar(max) NULL;
END
GO

IF COL_LENGTH('dbo.EmailSettings', 'EmployeeFromEmail') IS NULL
BEGIN
    ALTER TABLE dbo.EmailSettings ADD EmployeeFromEmail nvarchar(200) NULL;
END
GO

IF COL_LENGTH('dbo.EmailSettings', 'EmployeeFromName') IS NULL
BEGIN
    ALTER TABLE dbo.EmailSettings ADD EmployeeFromName nvarchar(200) NULL;
END
GO

IF COL_LENGTH('dbo.EmailSettings', 'EmployeeSignature') IS NULL
BEGIN
    ALTER TABLE dbo.EmailSettings ADD EmployeeSignature nvarchar(max) NULL;
END
GO

IF COL_LENGTH('dbo.EmailTemplates', 'SenderType') IS NULL
BEGIN
    ALTER TABLE dbo.EmailTemplates ADD SenderType varchar(20) NOT NULL CONSTRAINT DF_EmailTemplates_SenderType DEFAULT('HR');
END
GO

UPDATE dbo.EmailSettings
SET HrFromEmail = ISNULL(NULLIF(HrFromEmail, ''), FromEmail),
    HrFromName = ISNULL(NULLIF(HrFromName, ''), FromName)
WHERE ISNULL(Deleted, 0) = 0;
GO

UPDATE dbo.EmailSettings
SET EmployeeFromEmail = ISNULL(NULLIF(EmployeeFromEmail, ''), FromEmail),
    EmployeeFromName = ISNULL(NULLIF(EmployeeFromName, ''), FromName)
WHERE ISNULL(Deleted, 0) = 0;
GO

UPDATE dbo.EmailTemplates
SET SenderType = 'EMPLOYEE'
WHERE Code IN ('LEAVE_CREATE')
  AND ISNULL(Deleted, 0) = 0;
GO

UPDATE dbo.EmailTemplates
SET SenderType = 'HR'
WHERE Code IN ('INTERVIEW_SCHEDULE', 'LEAVE_APPROVE', 'LEAVE_REJECT', 'LEAVE_PENDING_HCNS', 'LEAVE_PENDING_BGD')
  AND ISNULL(Deleted, 0) = 0;
GO

PRINT 'Migration V083 completed successfully.';
