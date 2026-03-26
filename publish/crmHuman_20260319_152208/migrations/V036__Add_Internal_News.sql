-- =============================================
-- Migration: V036__Add_Internal_News
-- Description: Add internal news posts with list/detail procedures.
-- =============================================

IF OBJECT_ID(N'dbo.InternalNews', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InternalNews](
        [Title] [nvarchar](200) NOT NULL,
        [Content] [nvarchar](max) NULL,
        [IsSendMail] [bit] NOT NULL CONSTRAINT [DF_InternalNews_IsSendMail] DEFAULT(0),
        [Deleted] [bit] NULL CONSTRAINT [DF_InternalNews_Deleted] DEFAULT(0),
        [IsActive] [int] NULL CONSTRAINT [DF_InternalNews_IsActive] DEFAULT(1),
        [CreatedBy] [int] NULL,
        [UpdatedBy] [int] NULL,
        [CreateAt] [datetime] NULL CONSTRAINT [DF_InternalNews_CreateAt] DEFAULT(GETDATE()),
        [UpdateAt] [datetime] NULL CONSTRAINT [DF_InternalNews_UpdateAt] DEFAULT(GETDATE()),
        [Id] [int] IDENTITY(1,1) NOT NULL,
        CONSTRAINT [PK_InternalNews] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.InternalNews', 'Title') IS NULL
        ALTER TABLE [dbo].[InternalNews] ADD [Title] [nvarchar](200) NOT NULL CONSTRAINT [DF_InternalNews_Title] DEFAULT('');
    IF COL_LENGTH('dbo.InternalNews', 'Content') IS NULL
        ALTER TABLE [dbo].[InternalNews] ADD [Content] [nvarchar](max) NULL;
    IF COL_LENGTH('dbo.InternalNews', 'IsSendMail') IS NULL
        ALTER TABLE [dbo].[InternalNews] ADD [IsSendMail] [bit] NOT NULL CONSTRAINT [DF_InternalNews_IsSendMail_Alt] DEFAULT(0);
    IF COL_LENGTH('dbo.InternalNews', 'Deleted') IS NULL
        ALTER TABLE [dbo].[InternalNews] ADD [Deleted] [bit] NULL CONSTRAINT [DF_InternalNews_Deleted_Alt] DEFAULT(0);
    IF COL_LENGTH('dbo.InternalNews', 'IsActive') IS NULL
        ALTER TABLE [dbo].[InternalNews] ADD [IsActive] [int] NULL CONSTRAINT [DF_InternalNews_IsActive_Alt] DEFAULT(1);
    IF COL_LENGTH('dbo.InternalNews', 'CreatedBy') IS NULL
        ALTER TABLE [dbo].[InternalNews] ADD [CreatedBy] [int] NULL;
    IF COL_LENGTH('dbo.InternalNews', 'UpdatedBy') IS NULL
        ALTER TABLE [dbo].[InternalNews] ADD [UpdatedBy] [int] NULL;
    IF COL_LENGTH('dbo.InternalNews', 'CreateAt') IS NULL
        ALTER TABLE [dbo].[InternalNews] ADD [CreateAt] [datetime] NULL CONSTRAINT [DF_InternalNews_CreateAt_Alt] DEFAULT(GETDATE());
    IF COL_LENGTH('dbo.InternalNews', 'UpdateAt') IS NULL
        ALTER TABLE [dbo].[InternalNews] ADD [UpdateAt] [datetime] NULL CONSTRAINT [DF_InternalNews_UpdateAt_Alt] DEFAULT(GETDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM AppPages WHERE Code = 'InternalNews')
BEGIN
    INSERT INTO AppPages (Code, Name, [Group], OrderIndex, IsActive)
    VALUES ('InternalNews', N'Tin noi bo', 'Common', 9, 1);
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type = 'U')
BEGIN
    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '1', 'InternalNews', 1, 1, 1, 1, 1
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '1' AND PageCode = 'InternalNews');

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '2', 'InternalNews', 1, 0, 0, 0, 0
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '2' AND PageCode = 'InternalNews');

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '3', 'InternalNews', 1, 0, 0, 0, 0
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '3' AND PageCode = 'InternalNews');

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '4', 'InternalNews', 1, 0, 0, 0, 0
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '4' AND PageCode = 'InternalNews');

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '6', 'InternalNews', 1, 0, 0, 0, 0
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '6' AND PageCode = 'InternalNews');

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '8', 'InternalNews', 1, 0, 0, 0, 0
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '8' AND PageCode = 'InternalNews');
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_InternalNews_getAll]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_InternalNews_getAll];
GO

CREATE PROCEDURE [dbo].[sp_InternalNews_getAll]
(
    @Token nvarchar(200) = '',
    @OrderBy varchar(30) = '',
    @Page int = 1,
    @Limit int = 10,
    @From datetime = null,
    @To datetime = null
)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @offset int = (@Page - 1) * @Limit;

    SELECT COUNT(d.Id) OVER() AS TotalRecord,
           d.Id,
           d.Title,
           d.IsSendMail,
           d.CreateAt,
           d.CreatedBy,
           d.UpdateAt,
           d.UpdatedBy,
           u.FullName AS AuthorName
    FROM InternalNews d
    LEFT JOIN Employees u ON d.CreatedBy = u.Id
    WHERE ISNULL(d.Deleted, 0) = 0
      AND (@Token = '' OR d.Title LIKE N'%' + @Token + '%')
      AND (@From IS NULL OR d.CreateAt >= @From)
      AND (@To IS NULL OR d.CreateAt <= @To)
    ORDER BY d.CreateAt DESC
    OFFSET @offset ROWS FETCH NEXT @Limit ROWS ONLY;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_InternalNews_GetById]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_InternalNews_GetById];
GO

CREATE PROCEDURE [dbo].[sp_InternalNews_GetById]
(
    @Id int
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT d.*, u.FullName AS AuthorName
    FROM InternalNews d
    LEFT JOIN Employees u ON d.CreatedBy = u.Id
    WHERE d.Id = @Id
      AND ISNULL(d.Deleted, 0) = 0;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_InternalNews_insert]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_InternalNews_insert];
GO

CREATE PROCEDURE [dbo].[sp_InternalNews_insert]
(
    @Title nvarchar(200),
    @Content nvarchar(max),
    @IsSendMail bit,
    @CreatedBy int
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO InternalNews
    (
        Title,
        Content,
        IsSendMail,
        CreatedBy,
        UpdatedBy,
        CreateAt,
        UpdateAt,
        IsActive,
        Deleted
    )
    VALUES
    (
        @Title,
        @Content,
        @IsSendMail,
        @CreatedBy,
        @CreatedBy,
        GETDATE(),
        GETDATE(),
        1,
        0
    );
    SELECT SCOPE_IDENTITY();
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_InternalNews_update]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_InternalNews_update];
GO

CREATE PROCEDURE [dbo].[sp_InternalNews_update]
(
    @Id int,
    @Title nvarchar(200),
    @Content nvarchar(max),
    @IsSendMail bit,
    @UpdatedBy int
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE InternalNews
    SET Title = @Title,
        Content = @Content,
        IsSendMail = @IsSendMail,
        UpdatedBy = @UpdatedBy,
        UpdateAt = GETDATE()
    WHERE Id = @Id;
END
GO
