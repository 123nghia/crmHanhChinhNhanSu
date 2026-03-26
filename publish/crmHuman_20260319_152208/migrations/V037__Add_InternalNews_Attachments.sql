-- =============================================
-- Migration: V037__Add_InternalNews_Attachments
-- Description: Add attachments for internal news posts.
-- =============================================

IF OBJECT_ID(N'dbo.InternalNewsAttachments', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InternalNewsAttachments](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [NewsId] [int] NOT NULL,
        [FileName] [nvarchar](255) NOT NULL,
        [FilePath] [nvarchar](500) NOT NULL,
        [FileSize] [bigint] NULL,
        [Deleted] [bit] NULL CONSTRAINT [DF_InternalNewsAttachments_Deleted] DEFAULT(0),
        [IsActive] [int] NULL CONSTRAINT [DF_InternalNewsAttachments_IsActive] DEFAULT(1),
        [CreatedBy] [int] NULL,
        [UpdatedBy] [int] NULL,
        [CreateAt] [datetime] NULL CONSTRAINT [DF_InternalNewsAttachments_CreateAt] DEFAULT(GETDATE()),
        [UpdateAt] [datetime] NULL CONSTRAINT [DF_InternalNewsAttachments_UpdateAt] DEFAULT(GETDATE()),
        CONSTRAINT [PK_InternalNewsAttachments] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.InternalNewsAttachments', 'NewsId') IS NULL
        ALTER TABLE [dbo].[InternalNewsAttachments] ADD [NewsId] [int] NOT NULL DEFAULT(0);
    IF COL_LENGTH('dbo.InternalNewsAttachments', 'FileName') IS NULL
        ALTER TABLE [dbo].[InternalNewsAttachments] ADD [FileName] [nvarchar](255) NOT NULL CONSTRAINT [DF_InternalNewsAttachments_FileName] DEFAULT('');
    IF COL_LENGTH('dbo.InternalNewsAttachments', 'FilePath') IS NULL
        ALTER TABLE [dbo].[InternalNewsAttachments] ADD [FilePath] [nvarchar](500) NOT NULL CONSTRAINT [DF_InternalNewsAttachments_FilePath] DEFAULT('');
    IF COL_LENGTH('dbo.InternalNewsAttachments', 'FileSize') IS NULL
        ALTER TABLE [dbo].[InternalNewsAttachments] ADD [FileSize] [bigint] NULL;
    IF COL_LENGTH('dbo.InternalNewsAttachments', 'Deleted') IS NULL
        ALTER TABLE [dbo].[InternalNewsAttachments] ADD [Deleted] [bit] NULL CONSTRAINT [DF_InternalNewsAttachments_Deleted_Alt] DEFAULT(0);
    IF COL_LENGTH('dbo.InternalNewsAttachments', 'IsActive') IS NULL
        ALTER TABLE [dbo].[InternalNewsAttachments] ADD [IsActive] [int] NULL CONSTRAINT [DF_InternalNewsAttachments_IsActive_Alt] DEFAULT(1);
    IF COL_LENGTH('dbo.InternalNewsAttachments', 'CreatedBy') IS NULL
        ALTER TABLE [dbo].[InternalNewsAttachments] ADD [CreatedBy] [int] NULL;
    IF COL_LENGTH('dbo.InternalNewsAttachments', 'UpdatedBy') IS NULL
        ALTER TABLE [dbo].[InternalNewsAttachments] ADD [UpdatedBy] [int] NULL;
    IF COL_LENGTH('dbo.InternalNewsAttachments', 'CreateAt') IS NULL
        ALTER TABLE [dbo].[InternalNewsAttachments] ADD [CreateAt] [datetime] NULL CONSTRAINT [DF_InternalNewsAttachments_CreateAt_Alt] DEFAULT(GETDATE());
    IF COL_LENGTH('dbo.InternalNewsAttachments', 'UpdateAt') IS NULL
        ALTER TABLE [dbo].[InternalNewsAttachments] ADD [UpdateAt] [datetime] NULL CONSTRAINT [DF_InternalNewsAttachments_UpdateAt_Alt] DEFAULT(GETDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InternalNewsAttachments_NewsId' AND object_id = OBJECT_ID(N'dbo.InternalNewsAttachments'))
BEGIN
    CREATE INDEX [IX_InternalNewsAttachments_NewsId] ON [dbo].[InternalNewsAttachments]([NewsId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InternalNewsAttachments_InternalNews')
BEGIN
    ALTER TABLE [dbo].[InternalNewsAttachments]
    ADD CONSTRAINT [FK_InternalNewsAttachments_InternalNews]
    FOREIGN KEY ([NewsId]) REFERENCES [dbo].[InternalNews]([Id]);
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_InternalNewsAttachment_getAllByNewsId]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_InternalNewsAttachment_getAllByNewsId];
GO

CREATE PROCEDURE [dbo].[sp_InternalNewsAttachment_getAllByNewsId]
(
    @NewsId int
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM InternalNewsAttachments
    WHERE NewsId = @NewsId
      AND ISNULL(Deleted, 0) = 0
    ORDER BY Id;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_InternalNewsAttachment_insert]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_InternalNewsAttachment_insert];
GO

CREATE PROCEDURE [dbo].[sp_InternalNewsAttachment_insert]
(
    @NewsId int,
    @FileName nvarchar(255),
    @FilePath nvarchar(500),
    @FileSize bigint = NULL,
    @CreatedBy int
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO InternalNewsAttachments
    (
        NewsId,
        FileName,
        FilePath,
        FileSize,
        CreatedBy,
        UpdatedBy,
        CreateAt,
        UpdateAt,
        IsActive,
        Deleted
    )
    VALUES
    (
        @NewsId,
        @FileName,
        @FilePath,
        @FileSize,
        @CreatedBy,
        @CreatedBy,
        GETDATE(),
        GETDATE(),
        1,
        0
    );
END
GO
