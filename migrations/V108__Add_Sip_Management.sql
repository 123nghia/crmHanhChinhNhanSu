-- =============================================
-- Migration: V108__Add_Sip_Management
-- Description: Add SIP server and SIP line management for employee assignments
-- =============================================

PRINT 'Applying migration V108: SIP management...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SipServers]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[SipServers]
    (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](200) NULL,
        [Host] [nvarchar](200) NULL,
        [Port] [int] NOT NULL CONSTRAINT [DF_SipServers_Port] DEFAULT(5060),
        [Domain] [nvarchar](200) NULL,
        [Transport] [varchar](20) NULL,
        [OutboundProxy] [nvarchar](200) NULL,
        [Note] [nvarchar](1000) NULL,
        [IsActive] [int] NOT NULL CONSTRAINT [DF_SipServers_IsActive] DEFAULT(1),
        [Deleted] [bit] NOT NULL CONSTRAINT [DF_SipServers_Deleted] DEFAULT(0),
        [CreatedBy] [int] NULL,
        [UpdatedBy] [int] NULL,
        [CreateAt] [datetime] NOT NULL CONSTRAINT [DF_SipServers_CreateAt] DEFAULT(GETDATE()),
        [UpdateAt] [datetime] NOT NULL CONSTRAINT [DF_SipServers_UpdateAt] DEFAULT(GETDATE()),
        CONSTRAINT [PK_SipServers] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SipLines]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[SipLines]
    (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [SipServerId] [int] NULL,
        [LineCode] [varchar](20) NOT NULL,
        [SipUserName] [nvarchar](150) NOT NULL,
        [SipPassword] [nvarchar](255) NOT NULL,
        [AuthUser] [nvarchar](150) NULL,
        [DisplayName] [nvarchar](200) NULL,
        [EmployeeId] [int] NULL,
        [AssignedAt] [datetime] NULL,
        [AssignedBy] [int] NULL,
        [RevokedAt] [datetime] NULL,
        [RevokedBy] [int] NULL,
        [Note] [nvarchar](1000) NULL,
        [IsActive] [int] NOT NULL CONSTRAINT [DF_SipLines_IsActive] DEFAULT(1),
        [Deleted] [bit] NOT NULL CONSTRAINT [DF_SipLines_Deleted] DEFAULT(0),
        [CreatedBy] [int] NULL,
        [UpdatedBy] [int] NULL,
        [CreateAt] [datetime] NOT NULL CONSTRAINT [DF_SipLines_CreateAt] DEFAULT(GETDATE()),
        [UpdateAt] [datetime] NOT NULL CONSTRAINT [DF_SipLines_UpdateAt] DEFAULT(GETDATE()),
        CONSTRAINT [PK_SipLines] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SipLines_LineCode' AND object_id = OBJECT_ID(N'[dbo].[SipLines]'))
BEGIN
    CREATE INDEX [IX_SipLines_LineCode] ON [dbo].[SipLines]([LineCode]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SipLines_EmployeeId' AND object_id = OBJECT_ID(N'[dbo].[SipLines]'))
BEGIN
    CREATE INDEX [IX_SipLines_EmployeeId] ON [dbo].[SipLines]([EmployeeId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SipServers WHERE ISNULL(Deleted, 0) = 0)
BEGIN
    INSERT INTO dbo.SipServers
    (
        [Name], [Host], [Port], [Domain], [Transport], [OutboundProxy], [Note],
        [IsActive], [Deleted], [CreatedBy], [UpdatedBy], [CreateAt], [UpdateAt]
    )
    VALUES
    (
        N'SIP Server mặc định', N'', 5060, N'', 'UDP', N'', N'Admin cập nhật thông tin server tại phần Cấu hình SIP.',
        1, 0, 0, 0, GETDATE(), GETDATE()
    );
END
GO

DECLARE @DefaultSipServerId INT;
SELECT TOP 1 @DefaultSipServerId = Id
FROM dbo.SipServers
WHERE ISNULL(Deleted, 0) = 0
ORDER BY CASE WHEN ISNULL(IsActive, 0) = 1 THEN 0 ELSE 1 END, Id;

INSERT INTO dbo.SipLines
(
    SipServerId, LineCode, SipUserName, SipPassword, AuthUser, DisplayName,
    EmployeeId, AssignedAt, AssignedBy, RevokedAt, RevokedBy, Note,
    IsActive, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt
)
SELECT
    @DefaultSipServerId,
    e.LineCode,
    e.LineCode,
    N'',
    NULL,
    e.FullName,
    e.Id,
    GETDATE(),
    0,
    NULL,
    NULL,
    N'Backfill từ Employees.LineCode',
    1,
    0,
    0,
    0,
    GETDATE(),
    GETDATE()
FROM dbo.Employees e
WHERE ISNULL(e.Deleted, 0) = 0
  AND NULLIF(LTRIM(RTRIM(e.LineCode)), '') IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.SipLines l
      WHERE l.LineCode = e.LineCode
        AND ISNULL(l.Deleted, 0) = 0
  );
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppPages WHERE Code = 'SipManagement')
BEGIN
    INSERT INTO dbo.AppPages (Code, Name, [Group], OrderIndex, IsActive)
    VALUES ('SipManagement', N'Cấu hình SIP', 'System', 102, 1);
END
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type = 'U')
BEGIN
    INSERT INTO dbo.RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '1', 'SipManagement', 1, 1, 1, 1, 1
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.RoleAllow
        WHERE RoleCode = '1'
          AND PageCode = 'SipManagement'
    );
END
GO

PRINT 'Migration V108 completed successfully.';
GO
