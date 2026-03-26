-- =============================================
-- Migration: V068__Add_User_Theme_And_Branding
-- Description: Add user theme settings and system branding tables
-- =============================================

PRINT 'Applying migration V068: User theme settings and branding...';

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserThemeSettings]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[UserThemeSettings](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NOT NULL,
        [PrimaryColor] [nvarchar](20) NULL,
        [ButtonColor] [nvarchar](20) NULL,
        [BackgroundColor] [nvarchar](20) NULL,
        [IsActive] [bit] NOT NULL CONSTRAINT [DF_UserThemeSettings_IsActive] DEFAULT(1),
        [Deleted] [bit] NOT NULL CONSTRAINT [DF_UserThemeSettings_Deleted] DEFAULT(0),
        [CreatedBy] [int] NULL,
        [UpdatedBy] [int] NULL,
        [CreateAt] [datetime] NULL CONSTRAINT [DF_UserThemeSettings_CreateAt] DEFAULT(GETDATE()),
        [UpdateAt] [datetime] NULL CONSTRAINT [DF_UserThemeSettings_UpdateAt] DEFAULT(GETDATE()),
        CONSTRAINT [PK_UserThemeSettings] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_UserThemeSettings_UserId' AND object_id = OBJECT_ID(N'[dbo].[UserThemeSettings]'))
BEGIN
    CREATE UNIQUE INDEX [UX_UserThemeSettings_UserId] ON [dbo].[UserThemeSettings]([UserId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SystemBranding]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[SystemBranding](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [LogoPath] [nvarchar](400) NULL,
        [IsActive] [bit] NOT NULL CONSTRAINT [DF_SystemBranding_IsActive] DEFAULT(1),
        [Deleted] [bit] NOT NULL CONSTRAINT [DF_SystemBranding_Deleted] DEFAULT(0),
        [CreatedBy] [int] NULL,
        [UpdatedBy] [int] NULL,
        [CreateAt] [datetime] NULL CONSTRAINT [DF_SystemBranding_CreateAt] DEFAULT(GETDATE()),
        [UpdateAt] [datetime] NULL CONSTRAINT [DF_SystemBranding_UpdateAt] DEFAULT(GETDATE()),
        CONSTRAINT [PK_SystemBranding] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

PRINT 'Migration V068 completed successfully.';
