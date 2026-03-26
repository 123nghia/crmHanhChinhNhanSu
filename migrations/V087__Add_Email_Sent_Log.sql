-- =============================================
-- Migration: V087__Add_Email_Sent_Log
-- Description: Store rendered system emails for admin/history viewing
-- =============================================

PRINT 'Applying migration V087: Add EmailSentLogs table...';

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmailSentLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EmailSentLogs](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [TemplateId] [int] NULL,
        [TemplateCode] [varchar](50) NULL,
        [SenderType] [varchar](20) NULL,
        [FromEmail] [nvarchar](200) NULL,
        [FromName] [nvarchar](200) NULL,
        [ToEmails] [nvarchar](max) NULL,
        [CcEmails] [nvarchar](max) NULL,
        [BccEmails] [nvarchar](max) NULL,
        [Subject] [nvarchar](500) NULL,
        [BodyHtml] [nvarchar](max) NULL,
        [SendSuccess] [bit] NOT NULL CONSTRAINT [DF_EmailSentLogs_SendSuccess] DEFAULT(0),
        [ErrorMessage] [nvarchar](max) NULL,
        [TriggeredByUserId] [int] NULL,
        [TriggeredByUserName] [nvarchar](200) NULL,
        [TriggeredByFullName] [nvarchar](200) NULL,
        [SenderEmployeeId] [int] NULL,
        [ManagerId] [int] NULL,
        [MessageId] [nvarchar](255) NULL,
        [Deleted] [bit] NULL CONSTRAINT [DF_EmailSentLogs_Deleted] DEFAULT(0),
        [IsActive] [int] NULL CONSTRAINT [DF_EmailSentLogs_IsActive] DEFAULT(1),
        [CreatedBy] [int] NULL,
        [UpdatedBy] [int] NULL,
        [CreateAt] [datetime] NULL CONSTRAINT [DF_EmailSentLogs_CreateAt] DEFAULT(GETDATE()),
        [UpdateAt] [datetime] NULL CONSTRAINT [DF_EmailSentLogs_UpdateAt] DEFAULT(GETDATE()),
        CONSTRAINT [PK_EmailSentLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmailSentLogs_CreateAt' AND object_id = OBJECT_ID(N'[dbo].[EmailSentLogs]'))
BEGIN
    CREATE INDEX [IX_EmailSentLogs_CreateAt] ON [dbo].[EmailSentLogs]([CreateAt] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmailSentLogs_TriggeredByUserId' AND object_id = OBJECT_ID(N'[dbo].[EmailSentLogs]'))
BEGIN
    CREATE INDEX [IX_EmailSentLogs_TriggeredByUserId] ON [dbo].[EmailSentLogs]([TriggeredByUserId], [CreateAt] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmailSentLogs_SenderEmployeeId' AND object_id = OBJECT_ID(N'[dbo].[EmailSentLogs]'))
BEGIN
    CREATE INDEX [IX_EmailSentLogs_SenderEmployeeId] ON [dbo].[EmailSentLogs]([SenderEmployeeId], [CreateAt] DESC);
END
GO

PRINT 'Migration V087 completed successfully.';
