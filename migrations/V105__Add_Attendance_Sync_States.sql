PRINT 'Applying migration V105: add attendance sync states...';

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AttendanceSyncStates]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[AttendanceSyncStates]
    (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [SyncKey] [nvarchar](150) NOT NULL,
        [RangeStartDate] [date] NULL,
        [RangeEndDate] [date] NULL,
        [IsCompleted] [bit] NOT NULL CONSTRAINT [DF_AttendanceSyncStates_IsCompleted] DEFAULT (0),
        [LastRunAt] [datetime] NULL,
        [LastSuccessAt] [datetime] NULL,
        [LastMessage] [nvarchar](1000) NULL,
        [CreateAt] [datetime] NOT NULL CONSTRAINT [DF_AttendanceSyncStates_CreateAt] DEFAULT (GETDATE()),
        [UpdateAt] [datetime] NOT NULL CONSTRAINT [DF_AttendanceSyncStates_UpdateAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_AttendanceSyncStates] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AttendanceSyncStates_SyncKey' AND object_id = OBJECT_ID(N'[dbo].[AttendanceSyncStates]'))
BEGIN
    CREATE UNIQUE INDEX [UX_AttendanceSyncStates_SyncKey]
    ON [dbo].[AttendanceSyncStates]([SyncKey]);
END
GO

PRINT 'Migration V105 completed successfully.';
