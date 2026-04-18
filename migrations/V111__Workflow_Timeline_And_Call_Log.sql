PRINT 'Applying migration V111: workflow timeline and call log...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.WorkflowTimelineEvents') AND type = N'U')
BEGIN
    CREATE TABLE dbo.WorkflowTimelineEvents
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_WorkflowTimelineEvents PRIMARY KEY,
        EntityType nvarchar(50) NOT NULL,
        EntityId int NOT NULL,
        EventCode nvarchar(80) NOT NULL,
        EventTitle nvarchar(300) NULL,
        CurrentStatus int NULL,
        CurrentStatusText nvarchar(100) NULL,
        TriggeredBy int NULL,
        TriggeredByName nvarchar(200) NULL,
        AssignedTo int NULL,
        AssignedToName nvarchar(200) NULL,
        OccurredAt datetime NOT NULL CONSTRAINT DF_WorkflowTimelineEvents_OccurredAt DEFAULT (GETDATE()),
        DueAt datetime NULL,
        SlaHours int NULL,
        EmailSent bit NOT NULL CONSTRAINT DF_WorkflowTimelineEvents_EmailSent DEFAULT (0),
        EmailFailed bit NOT NULL CONSTRAINT DF_WorkflowTimelineEvents_EmailFailed DEFAULT (0),
        EmailLogId int NULL,
        NotificationCreated bit NOT NULL CONSTRAINT DF_WorkflowTimelineEvents_NotificationCreated DEFAULT (0),
        NotificationId int NULL,
        AttendanceSyncStatus nvarchar(30) NULL,
        ErrorMessage nvarchar(2000) NULL,
        MetadataJson nvarchar(max) NULL,
        Deleted bit NOT NULL CONSTRAINT DF_WorkflowTimelineEvents_Deleted DEFAULT (0),
        IsActive int NOT NULL CONSTRAINT DF_WorkflowTimelineEvents_IsActive DEFAULT (1),
        CreatedBy int NOT NULL CONSTRAINT DF_WorkflowTimelineEvents_CreatedBy DEFAULT (0),
        UpdatedBy int NOT NULL CONSTRAINT DF_WorkflowTimelineEvents_UpdatedBy DEFAULT (0),
        CreateAt datetime NOT NULL CONSTRAINT DF_WorkflowTimelineEvents_CreateAt DEFAULT (GETDATE()),
        UpdateAt datetime NOT NULL CONSTRAINT DF_WorkflowTimelineEvents_UpdateAt DEFAULT (GETDATE())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WorkflowTimelineEvents_Entity' AND object_id = OBJECT_ID(N'dbo.WorkflowTimelineEvents'))
BEGIN
    CREATE INDEX IX_WorkflowTimelineEvents_Entity
    ON dbo.WorkflowTimelineEvents(EntityType, EntityId, OccurredAt, Id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WorkflowTimelineEvents_AssignedTo' AND object_id = OBJECT_ID(N'dbo.WorkflowTimelineEvents'))
BEGIN
    CREATE INDEX IX_WorkflowTimelineEvents_AssignedTo
    ON dbo.WorkflowTimelineEvents(AssignedTo, DueAt, OccurredAt)
    INCLUDE(EntityType, EntityId, EventCode, CurrentStatus);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.CallLogs') AND type = N'U')
BEGIN
    CREATE TABLE dbo.CallLogs
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CallLogs PRIMARY KEY,
        EntityType nvarchar(50) NOT NULL,
        EntityId int NOT NULL,
        EmployeeId int NOT NULL,
        EmployeeName nvarchar(200) NULL,
        Extension nvarchar(50) NULL,
        LineCode nvarchar(50) NULL,
        PhoneNumber nvarchar(50) NOT NULL,
        Direction nvarchar(20) NOT NULL,
        ProviderCallId nvarchar(100) NULL,
        StartTime datetime NULL,
        EndTime datetime NULL,
        Duration int NULL,
        Status nvarchar(30) NOT NULL,
        RecordingUrl nvarchar(1000) NULL,
        Outcome nvarchar(100) NULL,
        Notes nvarchar(max) NULL,
        SourcePage nvarchar(100) NULL,
        ProviderRequestJson nvarchar(max) NULL,
        ProviderResponseJson nvarchar(max) NULL,
        ErrorMessage nvarchar(2000) NULL,
        NextFollowUpAt datetime NULL,
        Deleted bit NOT NULL CONSTRAINT DF_CallLogs_Deleted DEFAULT (0),
        IsActive int NOT NULL CONSTRAINT DF_CallLogs_IsActive DEFAULT (1),
        CreatedBy int NOT NULL CONSTRAINT DF_CallLogs_CreatedBy DEFAULT (0),
        UpdatedBy int NOT NULL CONSTRAINT DF_CallLogs_UpdatedBy DEFAULT (0),
        CreateAt datetime NOT NULL CONSTRAINT DF_CallLogs_CreateAt DEFAULT (GETDATE()),
        UpdateAt datetime NOT NULL CONSTRAINT DF_CallLogs_UpdateAt DEFAULT (GETDATE())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CallLogs_Entity' AND object_id = OBJECT_ID(N'dbo.CallLogs'))
BEGIN
    CREATE INDEX IX_CallLogs_Entity
    ON dbo.CallLogs(EntityType, EntityId, StartTime DESC, Id DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CallLogs_EmployeeTime' AND object_id = OBJECT_ID(N'dbo.CallLogs'))
BEGIN
    CREATE INDEX IX_CallLogs_EmployeeTime
    ON dbo.CallLogs(EmployeeId, StartTime DESC, Id DESC)
    INCLUDE(Status, Outcome, PhoneNumber, ProviderCallId);
END
GO

IF COL_LENGTH('dbo.AppNotifications', 'Category') IS NULL
BEGIN
    ALTER TABLE dbo.AppNotifications ADD Category nvarchar(30) NULL;
END
GO

IF COL_LENGTH('dbo.AppNotifications', 'RelatedEntityType') IS NULL
BEGIN
    ALTER TABLE dbo.AppNotifications ADD RelatedEntityType nvarchar(50) NULL;
END
GO

IF COL_LENGTH('dbo.AppNotifications', 'RelatedEntityId') IS NULL
BEGIN
    ALTER TABLE dbo.AppNotifications ADD RelatedEntityId int NULL;
END
GO

IF COL_LENGTH('dbo.AppNotifications', 'EventCode') IS NULL
BEGIN
    ALTER TABLE dbo.AppNotifications ADD EventCode nvarchar(80) NULL;
END
GO

IF COL_LENGTH('dbo.AppNotifications', 'DueAt') IS NULL
BEGIN
    ALTER TABLE dbo.AppNotifications ADD DueAt datetime NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppNotifications_ActionCenter' AND object_id = OBJECT_ID(N'dbo.AppNotifications'))
BEGIN
    CREATE INDEX IX_AppNotifications_ActionCenter
    ON dbo.AppNotifications(ReceiverId, IsRead, Category, DueAt, CreateAt DESC)
    INCLUDE(Type, RelatedEntityType, RelatedEntityId);
END
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.EmailTemplates') AND type = N'U')
BEGIN
    UPDATE dbo.EmailTemplates
    SET Body = ISNULL(Body, N'')
        + N'<p><strong>Xu ly nhanh:</strong> '
        + N'<a href="{{LeaveDetailUrl}}">Xem chi tiet</a> | '
        + N'<a href="{{LeaveApproveUrl}}">Duyet trong he thong</a> | '
        + N'<a href="{{LeaveRejectUrl}}">Tu choi trong he thong</a></p>',
        UpdateAt = GETDATE()
    WHERE Code IN ('LEAVE_CREATE', 'LEAVE_APPROVE', 'LEAVE_REJECT', 'LEAVE_PENDING_HCNS', 'LEAVE_PENDING_BGD')
      AND ISNULL(Deleted, 0) = 0
      AND ISNULL(Body, N'') NOT LIKE N'%{{LeaveApproveUrl}}%'
      AND ISNULL(Body, N'') NOT LIKE N'%{{LeaveRejectUrl}}%';
END
GO

PRINT 'Migration V111 completed.';
GO
