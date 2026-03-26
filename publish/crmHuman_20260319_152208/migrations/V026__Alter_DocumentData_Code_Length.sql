-- =============================================
-- Migration: V026__Alter_DocumentData_Code_Length
-- Author: System
-- Date: 2026-01-09
-- Description: Expand DocumentData.Code length to support "FOLDER"
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V026: Alter DocumentData.Code length...';

IF OBJECT_ID(N'dbo.DocumentData', N'U') IS NOT NULL
BEGIN
    DECLARE @currentLength INT;

    SELECT @currentLength = CHARACTER_MAXIMUM_LENGTH
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'DocumentData' AND COLUMN_NAME = 'Code';

    IF @currentLength IS NOT NULL AND @currentLength < 10
    BEGIN
        ALTER TABLE [dbo].[DocumentData] ALTER COLUMN [Code] varchar(10) NULL;
        PRINT '  DocumentData.Code length updated to varchar(10).';
    END
    ELSE
    BEGIN
        PRINT '  DocumentData.Code length is already >= 10. Skipping.';
    END
END
ELSE
BEGIN
    PRINT '  DocumentData table not found. Skipping.';
END

COMMIT TRANSACTION;
