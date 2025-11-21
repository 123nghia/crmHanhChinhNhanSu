-- =============================================
-- Migration: V001__Initial_Schema
-- Author: System
-- Date: 2024-11-21
-- Description: Tạo bảng __MigrationHistory để theo dõi migrations
-- Note: Base schema đã tồn tại trong sqlscript.sql
-- =============================================

BEGIN TRANSACTION;

-- Tạo bảng __MigrationHistory nếu chưa có
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__MigrationHistory')
BEGIN
    PRINT 'Creating __MigrationHistory table...';
    
    CREATE TABLE [dbo].[__MigrationHistory](
        [MigrationId] INT IDENTITY(1,1) PRIMARY KEY,
        [Version] VARCHAR(10) NOT NULL UNIQUE,
        [Description] NVARCHAR(255) NOT NULL,
        [FileName] VARCHAR(255) NOT NULL,
        [AppliedOn] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ExecutionTime] INT NULL, -- milliseconds
        [Success] BIT NOT NULL DEFAULT 1,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        CONSTRAINT UQ_MigrationVersion UNIQUE (Version)
    );
    
    PRINT '__MigrationHistory table created successfully.';
END
ELSE
BEGIN
    PRINT '__MigrationHistory table already exists.';
END

COMMIT TRANSACTION;

