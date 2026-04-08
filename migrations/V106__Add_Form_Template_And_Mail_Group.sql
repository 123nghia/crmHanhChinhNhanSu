PRINT 'Applying migration V106: form templates, mail groups, and internal news recipients...';
GO

IF COL_LENGTH('dbo.InternalNews', 'DirectRecipientEmails') IS NULL
BEGIN
    ALTER TABLE dbo.InternalNews
    ADD DirectRecipientEmails NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FormTemplates]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FormTemplates]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Title] NVARCHAR(250) NOT NULL,
        [Content] NVARCHAR(MAX) NULL,
        [FileName] NVARCHAR(255) NULL,
        [FilePath] NVARCHAR(500) NULL,
        [FileSize] BIGINT NULL,
        [CreateAt] DATETIME NOT NULL CONSTRAINT [DF_FormTemplates_CreateAt] DEFAULT(GETDATE()),
        [CreatedBy] INT NULL,
        [UpdateAt] DATETIME NULL,
        [UpdatedBy] INT NULL,
        [Deleted] BIT NOT NULL CONSTRAINT [DF_FormTemplates_Deleted] DEFAULT(0),
        [IsActive] INT NOT NULL CONSTRAINT [DF_FormTemplates_IsActive] DEFAULT(1)
    );

    CREATE INDEX [IX_FormTemplates_Title] ON [dbo].[FormTemplates]([Title]);
    CREATE INDEX [IX_FormTemplates_CreateAt] ON [dbo].[FormTemplates]([CreateAt] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MailGroups]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MailGroups]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [IncludeAllCompanyEmails] BIT NOT NULL CONSTRAINT [DF_MailGroups_IncludeAllCompanyEmails] DEFAULT(0),
        [IncludeAllPersonalEmails] BIT NOT NULL CONSTRAINT [DF_MailGroups_IncludeAllPersonalEmails] DEFAULT(0),
        [IsBlocked] BIT NOT NULL CONSTRAINT [DF_MailGroups_IsBlocked] DEFAULT(0),
        [CreateAt] DATETIME NOT NULL CONSTRAINT [DF_MailGroups_CreateAt] DEFAULT(GETDATE()),
        [CreatedBy] INT NULL,
        [UpdateAt] DATETIME NULL,
        [UpdatedBy] INT NULL,
        [Deleted] BIT NOT NULL CONSTRAINT [DF_MailGroups_Deleted] DEFAULT(0),
        [IsActive] INT NOT NULL CONSTRAINT [DF_MailGroups_IsActive] DEFAULT(1)
    );

    CREATE INDEX [IX_MailGroups_Name] ON [dbo].[MailGroups]([Name]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MailGroupRecipients]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MailGroupRecipients]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [MailGroupId] INT NOT NULL,
        [Email] NVARCHAR(320) NOT NULL,
        [DisplayName] NVARCHAR(200) NULL,
        [IsBlocked] BIT NOT NULL CONSTRAINT [DF_MailGroupRecipients_IsBlocked] DEFAULT(0),
        [CreateAt] DATETIME NOT NULL CONSTRAINT [DF_MailGroupRecipients_CreateAt] DEFAULT(GETDATE()),
        [CreatedBy] INT NULL,
        [UpdateAt] DATETIME NULL,
        [UpdatedBy] INT NULL,
        [Deleted] BIT NOT NULL CONSTRAINT [DF_MailGroupRecipients_Deleted] DEFAULT(0),
        [IsActive] INT NOT NULL CONSTRAINT [DF_MailGroupRecipients_IsActive] DEFAULT(1)
    );

    CREATE INDEX [IX_MailGroupRecipients_Group] ON [dbo].[MailGroupRecipients]([MailGroupId], [Deleted], [IsBlocked]);
    CREATE INDEX [IX_MailGroupRecipients_Email] ON [dbo].[MailGroupRecipients]([Email]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InternalNewsMailGroups]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[InternalNewsMailGroups]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [NewsId] INT NOT NULL,
        [MailGroupId] INT NOT NULL,
        [CreateAt] DATETIME NOT NULL CONSTRAINT [DF_InternalNewsMailGroups_CreateAt] DEFAULT(GETDATE()),
        [CreatedBy] INT NULL,
        [UpdateAt] DATETIME NULL,
        [UpdatedBy] INT NULL,
        [Deleted] BIT NOT NULL CONSTRAINT [DF_InternalNewsMailGroups_Deleted] DEFAULT(0),
        [IsActive] INT NOT NULL CONSTRAINT [DF_InternalNewsMailGroups_IsActive] DEFAULT(1)
    );

    CREATE INDEX [IX_InternalNewsMailGroups_News] ON [dbo].[InternalNewsMailGroups]([NewsId], [Deleted]);
    CREATE INDEX [IX_InternalNewsMailGroups_Group] ON [dbo].[InternalNewsMailGroups]([MailGroupId], [Deleted]);
END
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AppPages]') AND type in (N'U'))
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.AppPages WHERE Code = 'FormTemplate')
    BEGIN
        INSERT INTO dbo.AppPages (Code, Name, [Group], OrderIndex, IsActive)
        VALUES ('FormTemplate', N'Bieu mau', N'Thong tin chung', 70, 1);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.AppPages WHERE Code = 'MailGroup')
    BEGIN
        INSERT INTO dbo.AppPages (Code, Name, [Group], OrderIndex, IsActive)
        VALUES ('MailGroup', N'Nhom mail', N'He thong', 71, 1);
    END
END
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type in (N'U'))
BEGIN
    DECLARE @FormViewerRoles TABLE (RoleCode VARCHAR(20), IsAdd BIT, IsEdit BIT, IsDelete BIT);
    INSERT INTO @FormViewerRoles (RoleCode, IsAdd, IsEdit, IsDelete)
    VALUES
        ('1', 1, 1, 1),
        ('2', 0, 0, 0),
        ('3', 0, 0, 0),
        ('4', 0, 0, 0),
        ('6', 0, 0, 0),
        ('7', 0, 0, 0),
        ('8', 0, 0, 0),
        ('9', 1, 1, 1);

    INSERT INTO dbo.RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT r.RoleCode, 'FormTemplate', 1, r.IsAdd, r.IsEdit, r.IsDelete, 0
    FROM @FormViewerRoles r
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.RoleAllow ra
        WHERE ra.RoleCode = r.RoleCode
          AND ra.PageCode = 'FormTemplate'
    );

    DECLARE @MailGroupRoles TABLE (RoleCode VARCHAR(20));
    INSERT INTO @MailGroupRoles (RoleCode)
    VALUES ('1'), ('9');

    INSERT INTO dbo.RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT r.RoleCode, 'MailGroup', 1, 1, 1, 1, 0
    FROM @MailGroupRoles r
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.RoleAllow ra
        WHERE ra.RoleCode = r.RoleCode
          AND ra.PageCode = 'MailGroup'
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.EmailTemplates WHERE Code = 'INTERNAL_NEWS_NOTIFY' AND ISNULL(Deleted, 0) = 0)
BEGIN
    INSERT INTO dbo.EmailTemplates
    (
        Code,
        Name,
        Subject,
        Body,
        SenderType,
        CcManager,
        CcEmails,
        BccEmails,
        IsActive,
        Deleted,
        CreatedBy,
        UpdatedBy,
        CreateAt,
        UpdateAt
    )
    VALUES
    (
        'INTERNAL_NEWS_NOTIFY',
        N'Thong bao tin noi bo',
        N'[Tin noi bo] {{NewsTitle}}',
        N'<p>Xin chao,</p><p>He thong vua dang bai viet noi bo moi.</p><p><strong>Tieu de:</strong> {{NewsTitle}}</p><p><strong>Nguoi dang:</strong> {{NewsAuthor}}</p><p><strong>Thoi gian:</strong> {{NewsCreatedAt}}</p><div>{{NewsContent}}</div><p><a href="{{NewsUrl}}">Xem chi tiet bai viet</a></p>',
        'HR',
        0,
        NULL,
        NULL,
        1,
        0,
        1,
        1,
        GETDATE(),
        GETDATE()
    );
END
GO

PRINT 'Migration V106 completed successfully.';
GO
