-- =============================================
-- Migration: V084__Add_Employee_Mail_Signature
-- Description: Add per-employee mail signature field
-- =============================================

PRINT 'Applying migration V084: add employee mail signature...';

IF COL_LENGTH('dbo.Employees', 'MailSignature') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD MailSignature nvarchar(max) NULL;
END
GO

PRINT 'Migration V084 completed successfully.';
