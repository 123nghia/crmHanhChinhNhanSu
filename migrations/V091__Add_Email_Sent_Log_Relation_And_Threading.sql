PRINT 'Applying migration V091: add EmailSentLogs relation/thread columns...';

IF COL_LENGTH('dbo.EmailSentLogs', 'RelatedEntityType') IS NULL
BEGIN
    ALTER TABLE [dbo].[EmailSentLogs]
    ADD [RelatedEntityType] [nvarchar](50) NULL;
END
GO

IF COL_LENGTH('dbo.EmailSentLogs', 'RelatedEntityId') IS NULL
BEGIN
    ALTER TABLE [dbo].[EmailSentLogs]
    ADD [RelatedEntityId] [int] NULL;
END
GO

IF COL_LENGTH('dbo.EmailSentLogs', 'ParentEmailSentLogId') IS NULL
BEGIN
    ALTER TABLE [dbo].[EmailSentLogs]
    ADD [ParentEmailSentLogId] [int] NULL;
END
GO

IF COL_LENGTH('dbo.EmailSentLogs', 'ParentMessageId') IS NULL
BEGIN
    ALTER TABLE [dbo].[EmailSentLogs]
    ADD [ParentMessageId] [nvarchar](255) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmailSentLogs_RelatedEntity' AND object_id = OBJECT_ID(N'[dbo].[EmailSentLogs]'))
BEGIN
    CREATE INDEX [IX_EmailSentLogs_RelatedEntity]
    ON [dbo].[EmailSentLogs]([RelatedEntityType], [RelatedEntityId], [CreateAt] DESC);
END
GO

PRINT 'Migration V091 completed successfully.';
