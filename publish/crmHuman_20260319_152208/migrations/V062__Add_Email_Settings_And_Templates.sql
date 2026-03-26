-- =============================================
-- Migration: V062__Add_Email_Settings_And_Templates
-- Description: Add email settings and templates for system mail flow
-- =============================================

PRINT 'Applying migration V062: Email settings and templates...';

-- 1. EmailSettings table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmailSettings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EmailSettings](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [SmtpHost] [nvarchar](200) NULL,
        [SmtpPort] [int] NULL,
        [EnableSsl] [bit] NOT NULL CONSTRAINT [DF_EmailSettings_EnableSsl] DEFAULT(0),
        [SmtpUser] [nvarchar](200) NULL,
        [SmtpPassword] [nvarchar](500) NULL,
        [FromEmail] [nvarchar](200) NULL,
        [FromName] [nvarchar](200) NULL,
        [IsActive] [int] NULL CONSTRAINT [DF_EmailSettings_IsActive] DEFAULT(1),
        [Deleted] [bit] NULL CONSTRAINT [DF_EmailSettings_Deleted] DEFAULT(0),
        [CreatedBy] [int] NULL,
        [UpdatedBy] [int] NULL,
        [CreateAt] [datetime] NULL CONSTRAINT [DF_EmailSettings_CreateAt] DEFAULT(GETDATE()),
        [UpdateAt] [datetime] NULL CONSTRAINT [DF_EmailSettings_UpdateAt] DEFAULT(GETDATE()),
        CONSTRAINT [PK_EmailSettings] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- 2. EmailTemplates table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmailTemplates]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EmailTemplates](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Code] [varchar](50) NOT NULL,
        [Name] [nvarchar](200) NOT NULL,
        [Subject] [nvarchar](300) NOT NULL,
        [Body] [nvarchar](max) NULL,
        [CcManager] [bit] NOT NULL CONSTRAINT [DF_EmailTemplates_CcManager] DEFAULT(0),
        [CcEmails] [nvarchar](max) NULL,
        [BccEmails] [nvarchar](max) NULL,
        [IsActive] [int] NULL CONSTRAINT [DF_EmailTemplates_IsActive] DEFAULT(1),
        [Deleted] [bit] NULL CONSTRAINT [DF_EmailTemplates_Deleted] DEFAULT(0),
        [CreatedBy] [int] NULL,
        [UpdatedBy] [int] NULL,
        [CreateAt] [datetime] NULL CONSTRAINT [DF_EmailTemplates_CreateAt] DEFAULT(GETDATE()),
        [UpdateAt] [datetime] NULL CONSTRAINT [DF_EmailTemplates_UpdateAt] DEFAULT(GETDATE()),
        CONSTRAINT [PK_EmailTemplates] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_EmailTemplates_Code' AND object_id = OBJECT_ID(N'[dbo].[EmailTemplates]'))
BEGIN
    CREATE UNIQUE INDEX [UX_EmailTemplates_Code] ON [dbo].[EmailTemplates]([Code]);
END
GO

-- 3. Seed default templates for Leave flow
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE Code = 'LEAVE_CREATE')
BEGIN
    INSERT INTO EmailTemplates (Code, Name, Subject, Body, CcManager, CcEmails, BccEmails, IsActive, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt)
    VALUES (
        'LEAVE_CREATE',
        N'Leave - New Request',
        N'[Leave] {EmployeeName} request {FromDate} - {ToDate}',
        N'<p>Hello,</p><p>{EmployeeName} has submitted a leave request.</p><p>Type: {LeaveTypeName} ({LeaveTypeCode})</p><p>From: {FromDate} - {ToDate}</p><p>Days: {NumDays}</p><p>Reason: {Reason}</p><p>Status: {StatusText}</p>',
        0,
        NULL,
        NULL,
        1,
        0,
        0,
        0,
        GETDATE(),
        GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE Code = 'LEAVE_APPROVE')
BEGIN
    INSERT INTO EmailTemplates (Code, Name, Subject, Body, CcManager, CcEmails, BccEmails, IsActive, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt)
    VALUES (
        'LEAVE_APPROVE',
        N'Leave - Approved',
        N'[Leave] Approved - {EmployeeName}',
        N'<p>Hello {EmployeeName},</p><p>Your leave request has been approved.</p><p>Type: {LeaveTypeName} ({LeaveTypeCode})</p><p>From: {FromDate} - {ToDate}</p><p>Days: {NumDays}</p><p>Approved by: {ApproverName} ({ApproverRole})</p><p>Status: {StatusText}</p>',
        1,
        NULL,
        NULL,
        1,
        0,
        0,
        0,
        GETDATE(),
        GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE Code = 'LEAVE_REJECT')
BEGIN
    INSERT INTO EmailTemplates (Code, Name, Subject, Body, CcManager, CcEmails, BccEmails, IsActive, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt)
    VALUES (
        'LEAVE_REJECT',
        N'Leave - Rejected',
        N'[Leave] Rejected - {EmployeeName}',
        N'<p>Hello {EmployeeName},</p><p>Your leave request has been rejected.</p><p>Type: {LeaveTypeName} ({LeaveTypeCode})</p><p>From: {FromDate} - {ToDate}</p><p>Days: {NumDays}</p><p>Rejected by: {ApproverName} ({ApproverRole})</p><p>Reason: {Comment}</p><p>Status: {StatusText}</p>',
        1,
        NULL,
        NULL,
        1,
        0,
        0,
        0,
        GETDATE(),
        GETDATE()
    );
END
GO

-- 4. Seed AppPages and RoleAllow
IF NOT EXISTS (SELECT 1 FROM AppPages WHERE Code = 'MailSetting')
BEGIN
    INSERT INTO AppPages (Code, Name, [Group], OrderIndex, IsActive)
    VALUES ('MailSetting', N'Cau hinh mail', 'System', 101, 1);
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type in (N'U'))
BEGIN
    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '1', 'MailSetting', 1, 1, 1, 1, 1
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '1' AND PageCode = 'MailSetting');
END
GO

PRINT 'Migration V062 completed successfully.';
