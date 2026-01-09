-- =============================================
-- Migration: V009__Cloud_Storage_And_Sharing
-- Author: System
-- Date: 2025-12-23
-- Description: Add cloud storage and sharing support to DocumentData
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V009: Cloud Storage And Sharing...';

IF OBJECT_ID(N'dbo.DocumentData', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.DocumentData', 'ParentId') IS NULL
        ALTER TABLE [dbo].[DocumentData] ADD [ParentId] [int] NULL;

    IF COL_LENGTH('dbo.DocumentData', 'IsFolder') IS NULL
        ALTER TABLE [dbo].[DocumentData] ADD [IsFolder] [bit] NULL DEFAULT 0;

    IF COL_LENGTH('dbo.DocumentData', 'AccessLevel') IS NULL
        ALTER TABLE [dbo].[DocumentData] ADD [AccessLevel] [int] NULL DEFAULT 0;

    IF COL_LENGTH('dbo.DocumentData', 'ShareToken') IS NULL
        ALTER TABLE [dbo].[DocumentData] ADD [ShareToken] [nvarchar](50) NULL;
END
ELSE
BEGIN
    PRINT '  DocumentData table not found. Skipping column updates.';
END

-- Create DocumentShares table for Restricted sharing
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentShares]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[DocumentShares](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [DocumentId] [int] NOT NULL,
        [UserId] [int] NOT NULL,
        [CreateAt] [datetime] NULL DEFAULT (getdate()),
        PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END

-- Update sp_DocumentData_getAll to support folder navigation and access control
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_getAll]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_getAll];

EXEC(N'
CREATE PROCEDURE [dbo].[sp_DocumentData_getAll]
(
    @Token nvarchar(30) = '''',
    @OrderBy varchar(30) = '''',
    @RelId int = -1,
    @RelCode varchar(10) = '''',
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
      AND (@RelCode = '''' OR d.RelCode = @RelCode)
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
');

-- Create sp_DocumentData_GetById
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_GetById]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_GetById];

EXEC(N'
CREATE PROCEDURE [dbo].[sp_DocumentData_GetById]
(
    @Id int
)
AS
BEGIN
    SELECT d.*, u.FullName as AuthorName
    FROM DocumentData d
    LEFT JOIN Employees u ON d.CreatedBy = u.Id
    WHERE d.Id = @Id;
END
');

-- Update sp_DocumentData_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_insert]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_insert];

EXEC(N'
CREATE PROCEDURE [dbo].[sp_DocumentData_insert]
(
    @RelId int,
    @RelCode varchar(10),
    @DisplayText nvarchar(500),
    @ValueFile nvarchar(1000),
    @Code varchar(10),
    @DataType int,
    @ParentId int,
    @IsFolder bit,
    @AccessLevel int,
    @CreatedBy int
)
AS
BEGIN
    INSERT INTO [dbo].[DocumentData]
    (
        [RelId], [RelCode], [DisplayText], [ValueFile], [Code], [dataType],
        [ParentId], [IsFolder], [AccessLevel], [CreatedBy], [CreateAt], [UpdateAt], [IsActive], [Deleted]
    )
    VALUES
    (
        @RelId, @RelCode, @DisplayText, @ValueFile, @Code, @DataType,
        (CASE WHEN @ParentId <= 0 THEN NULL ELSE @ParentId END), @IsFolder, @AccessLevel, @CreatedBy, GETDATE(), GETDATE(), 1, 0
    );
    SELECT SCOPE_IDENTITY();
END
');

-- Update sp_DocumentData_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_update]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_update];

EXEC(N'
CREATE PROCEDURE [dbo].[sp_DocumentData_update]
(
    @Id int,
    @DisplayText nvarchar(500),
    @ValueFile nvarchar(1000),
    @ParentId int,
    @AccessLevel int,
    @UpdatedBy int
)
AS
BEGIN
    UPDATE [dbo].[DocumentData]
    SET [DisplayText] = @DisplayText,
        [ValueFile] = @ValueFile,
        [ParentId] = (CASE WHEN @ParentId <= 0 THEN NULL ELSE @ParentId END),
        [AccessLevel] = @AccessLevel,
        [UpdatedBy] = @UpdatedBy,
        [UpdateAt] = GETDATE()
    WHERE [Id] = @Id;
END
');

PRINT 'Migration V009 completed successfully';

COMMIT TRANSACTION;
