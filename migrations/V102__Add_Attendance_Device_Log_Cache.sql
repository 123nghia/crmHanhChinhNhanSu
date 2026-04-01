PRINT 'Applying migration V102: add attendance device log cache...';

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AttendanceDeviceLogs]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[AttendanceDeviceLogs]
    (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [DeviceIp] [nvarchar](50) NOT NULL,
        [DevicePort] [int] NOT NULL,
        [DeviceUserId] [varchar](50) NOT NULL,
        [NormalizedUserId] [varchar](50) NULL,
        [RecordTime] [datetime2](0) NOT NULL,
        [InOutMode] [varchar](20) NULL,
        [Source] [nvarchar](50) NOT NULL,
        [CreateAt] [datetime] NOT NULL CONSTRAINT [DF_AttendanceDeviceLogs_CreateAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_AttendanceDeviceLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AttendanceDeviceLogs_Key' AND object_id = OBJECT_ID(N'[dbo].[AttendanceDeviceLogs]'))
BEGIN
    CREATE UNIQUE INDEX [UX_AttendanceDeviceLogs_Key]
    ON [dbo].[AttendanceDeviceLogs]([DeviceIp], [DevicePort], [DeviceUserId], [RecordTime]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceDeviceLogs_RecordTime' AND object_id = OBJECT_ID(N'[dbo].[AttendanceDeviceLogs]'))
BEGIN
    CREATE INDEX [IX_AttendanceDeviceLogs_RecordTime]
    ON [dbo].[AttendanceDeviceLogs]([RecordTime] DESC, [DeviceIp], [DevicePort]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceDeviceLogs_NormalizedUserId' AND object_id = OBJECT_ID(N'[dbo].[AttendanceDeviceLogs]'))
BEGIN
    CREATE INDEX [IX_AttendanceDeviceLogs_NormalizedUserId]
    ON [dbo].[AttendanceDeviceLogs]([NormalizedUserId], [RecordTime] DESC);
END
GO

PRINT 'Migration V102 completed successfully.';
