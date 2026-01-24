-- =============================================
-- Migration: V067__Add_Audit_Log
-- Description: Add audit log table and stored procedures
-- =============================================

PRINT 'Applying migration V067: Audit log...';

-- ============================================
-- 1. Create AuditLog table
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLog]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[AuditLog](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NULL,
        [UserName] [nvarchar](100) NULL,
        [FullName] [nvarchar](255) NULL,
        [RoleCode] [nvarchar](50) NULL,
        [Action] [nvarchar](50) NOT NULL,
        [Path] [nvarchar](500) NOT NULL,
        [QueryString] [nvarchar](1000) NULL,
        [Payload] [nvarchar](max) NULL,
        [StatusCode] [int] NULL,
        [DurationMs] [int] NULL,
        [ClientIp] [nvarchar](50) NULL,
        [UserAgent] [nvarchar](255) NULL,
        [CreateAt] [datetime] NULL DEFAULT GETDATE(),
        [UpdateAt] [datetime] NULL DEFAULT GETDATE(),
        [CreatedBy] [int] NULL,
        [UpdatedBy] [int] NULL,
        [Deleted] [bit] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLog_User_CreateAt' AND object_id = OBJECT_ID(N'[dbo].[AuditLog]'))
BEGIN
    CREATE INDEX [IX_AuditLog_User_CreateAt]
    ON [dbo].[AuditLog]([UserId], [CreateAt] DESC);
END
GO

-- ============================================
-- 2. Stored Procedures
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_AuditLog_Insert]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_AuditLog_Insert];
GO

CREATE PROCEDURE [dbo].[sp_AuditLog_Insert]
(
    @UserId int = NULL,
    @UserName nvarchar(100) = NULL,
    @FullName nvarchar(255) = NULL,
    @RoleCode nvarchar(50) = NULL,
    @Action nvarchar(50),
    @Path nvarchar(500),
    @QueryString nvarchar(1000) = NULL,
    @Payload nvarchar(max) = NULL,
    @StatusCode int = NULL,
    @DurationMs int = NULL,
    @ClientIp nvarchar(50) = NULL,
    @UserAgent nvarchar(255) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @now datetime = GETDATE();

    INSERT INTO AuditLog
    (
        UserId, UserName, FullName, RoleCode,
        Action, Path, QueryString, Payload,
        StatusCode, DurationMs, ClientIp, UserAgent,
        CreateAt, UpdateAt, IsActive, Deleted
    )
    VALUES
    (
        @UserId, @UserName, @FullName, @RoleCode,
        @Action, @Path, @QueryString, @Payload,
        @StatusCode, @DurationMs, @ClientIp, @UserAgent,
        @now, @now, 1, 0
    );

    SELECT SCOPE_IDENTITY() AS Id;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_AuditLog_getAll]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_AuditLog_getAll];
GO

CREATE PROCEDURE [dbo].[sp_AuditLog_getAll]
(
    @FromDate datetime = NULL,
    @ToDate datetime = NULL,
    @UserId int = NULL,
    @RoleCode nvarchar(50) = NULL,
    @Token nvarchar(255) = '',
    @offset int = 0,
    @limit int = 50
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(1) OVER() AS TotalRecord,
        Id,
        UserId,
        UserName,
        FullName,
        RoleCode,
        Action,
        Path,
        QueryString,
        Payload,
        StatusCode,
        DurationMs,
        ClientIp,
        UserAgent,
        CreateAt,
        UpdateAt
    FROM AuditLog
    WHERE ISNULL(Deleted, 0) = 0
      AND (@FromDate IS NULL OR CreateAt >= @FromDate)
      AND (@ToDate IS NULL OR CreateAt <= @ToDate)
      AND (@UserId IS NULL OR UserId = @UserId)
      AND (@RoleCode IS NULL OR RoleCode = @RoleCode)
      AND (@Token = '' OR UserName LIKE N'%' + @Token + '%' OR FullName LIKE N'%' + @Token + '%')
    ORDER BY CreateAt DESC, Id DESC
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END
GO

PRINT 'Migration V067 completed successfully.';
