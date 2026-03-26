-- =============================================
-- Migration: V076__Add_Employee_AvatarFile
-- Description: Add AvatarFile column to Employees table
-- =============================================

PRINT 'Applying migration V076: Add AvatarFile column...';

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Employees]')
      AND name = N'AvatarFile'
)
BEGIN
    ALTER TABLE [dbo].[Employees] ADD [AvatarFile] NVARCHAR(500) NULL;
END

PRINT '✓ Migration V076 completed successfully';
