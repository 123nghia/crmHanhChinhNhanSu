PRINT 'Applying migration V099: support request workflow...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SupportRequests]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SupportRequests]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RequesterId] INT NOT NULL,
        [Title] NVARCHAR(250) NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [TargetDepartmentCode] VARCHAR(50) NOT NULL,
        [AssignedToId] INT NULL,
        [Status] INT NOT NULL CONSTRAINT [DF_SupportRequests_Status] DEFAULT(0),
        [ProcessorComment] NVARCHAR(MAX) NULL,
        [AssignedAt] DATETIME NULL,
        [CompletedAt] DATETIME NULL,
        [CreateAt] DATETIME NOT NULL CONSTRAINT [DF_SupportRequests_CreateAt] DEFAULT(GETDATE()),
        [CreatedBy] INT NULL,
        [UpdateAt] DATETIME NULL,
        [UpdatedBy] INT NULL,
        [Deleted] BIT NOT NULL CONSTRAINT [DF_SupportRequests_Deleted] DEFAULT(0),
        [IsActive] INT NOT NULL CONSTRAINT [DF_SupportRequests_IsActive] DEFAULT(1)
    );

    CREATE INDEX [IX_SupportRequests_Requester] ON [dbo].[SupportRequests]([RequesterId], [CreateAt] DESC);
    CREATE INDEX [IX_SupportRequests_AssignedTo] ON [dbo].[SupportRequests]([AssignedToId], [Status], [CreateAt] DESC);
    CREATE INDEX [IX_SupportRequests_Department] ON [dbo].[SupportRequests]([TargetDepartmentCode], [Status]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SupportRequestAttachments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SupportRequestAttachments]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RequestId] INT NOT NULL,
        [FileName] NVARCHAR(255) NOT NULL,
        [FilePath] NVARCHAR(500) NOT NULL,
        [FileSize] BIGINT NULL,
        [CreateAt] DATETIME NOT NULL CONSTRAINT [DF_SupportRequestAttachments_CreateAt] DEFAULT(GETDATE()),
        [CreatedBy] INT NULL,
        [UpdateAt] DATETIME NULL,
        [UpdatedBy] INT NULL,
        [Deleted] BIT NOT NULL CONSTRAINT [DF_SupportRequestAttachments_Deleted] DEFAULT(0),
        [IsActive] INT NOT NULL CONSTRAINT [DF_SupportRequestAttachments_IsActive] DEFAULT(1)
    );

    CREATE INDEX [IX_SupportRequestAttachments_Request] ON [dbo].[SupportRequestAttachments]([RequestId], [Id] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SupportRequestHistories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SupportRequestHistories]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RequestId] INT NOT NULL,
        [Action] NVARCHAR(50) NOT NULL,
        [ActionBy] INT NOT NULL,
        [ActionTime] DATETIME NOT NULL CONSTRAINT [DF_SupportRequestHistories_ActionTime] DEFAULT(GETDATE()),
        [Comment] NVARCHAR(MAX) NULL,
        [StatusAfter] INT NOT NULL,
        [CreateAt] DATETIME NOT NULL CONSTRAINT [DF_SupportRequestHistories_CreateAt] DEFAULT(GETDATE()),
        [CreatedBy] INT NULL,
        [UpdateAt] DATETIME NULL,
        [UpdatedBy] INT NULL,
        [Deleted] BIT NOT NULL CONSTRAINT [DF_SupportRequestHistories_Deleted] DEFAULT(0),
        [IsActive] INT NOT NULL CONSTRAINT [DF_SupportRequestHistories_IsActive] DEFAULT(1)
    );

    CREATE INDEX [IX_SupportRequestHistories_Request] ON [dbo].[SupportRequestHistories]([RequestId], [ActionTime] DESC, [Id] DESC);
END
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AppPages]') AND type in (N'U'))
BEGIN
    IF NOT EXISTS (SELECT 1 FROM AppPages WHERE Code = 'SupportRequest')
    BEGIN
        INSERT INTO AppPages (Code, Name, [Group], OrderIndex, IsActive)
        VALUES ('SupportRequest', N'Yeu cau ho tro', N'Thong tin chung', 68, 1);
    END

    IF NOT EXISTS (SELECT 1 FROM AppPages WHERE Code = 'SupportRequestProcessing')
    BEGIN
        INSERT INTO AppPages (Code, Name, [Group], OrderIndex, IsActive)
        VALUES ('SupportRequestProcessing', N'Xu ly yeu cau ho tro', N'Thong tin chung', 69, 1);
    END
END
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type in (N'U'))
BEGIN
    DECLARE @Roles TABLE (RoleCode VARCHAR(20));
    INSERT INTO @Roles (RoleCode) VALUES ('1'), ('2'), ('3'), ('4'), ('6'), ('7'), ('8'), ('9');

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT r.RoleCode, 'SupportRequest', 1, 1, 1, 1, 0
    FROM @Roles r
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM RoleAllow ra
        WHERE ra.RoleCode = r.RoleCode
          AND ra.PageCode = 'SupportRequest'
    );

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT r.RoleCode, 'SupportRequestProcessing', 1, 0, 0, 0, 1
    FROM @Roles r
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM RoleAllow ra
        WHERE ra.RoleCode = r.RoleCode
          AND ra.PageCode = 'SupportRequestProcessing'
    );
END
GO

PRINT 'Migration V099 completed successfully.';
GO
