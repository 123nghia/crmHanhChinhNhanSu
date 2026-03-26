-- =============================================
-- Migration: V066__Add_Log_History
-- Description: Add login/logout history table and stored procedures
-- =============================================

PRINT 'Applying migration V066: Login/logout history...';

-- ============================================
-- 1. Create LogHistory table
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LogHistory]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[LogHistory](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NOT NULL,
        [UserName] [nvarchar](100) NULL,
        [FullName] [nvarchar](255) NULL,
        [RoleCode] [nvarchar](50) NULL,
        [LoginAt] [datetime] NOT NULL DEFAULT GETDATE(),
        [LogoutAt] [datetime] NULL,
        [Source] [nvarchar](50) NULL,
        [ClientIp] [nvarchar](50) NULL,
        [UserAgent] [nvarchar](255) NULL,
        [CreatedBy] [int] NULL,
        [CreateAt] [datetime] NULL DEFAULT GETDATE(),
        [UpdatedBy] [int] NULL,
        [UpdateAt] [datetime] NULL DEFAULT GETDATE(),
        [Deleted] [bit] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        CONSTRAINT [PK_LogHistory] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LogHistory_User_Login' AND object_id = OBJECT_ID(N'[dbo].[LogHistory]'))
BEGIN
    CREATE INDEX [IX_LogHistory_User_Login]
    ON [dbo].[LogHistory]([UserId], [RoleCode], [LoginAt] DESC);
END
GO

-- ============================================
-- 2. Stored Procedures
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_LogHistory_Insert]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_LogHistory_Insert];
GO

CREATE PROCEDURE [dbo].[sp_LogHistory_Insert]
(
    @UserId int,
    @UserName nvarchar(100) = NULL,
    @FullName nvarchar(255) = NULL,
    @RoleCode nvarchar(50) = NULL,
    @LoginAt datetime = NULL,
    @Source nvarchar(50) = NULL,
    @ClientIp nvarchar(50) = NULL,
    @UserAgent nvarchar(255) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @now datetime = ISNULL(@LoginAt, GETDATE());

    INSERT INTO LogHistory
    (
        UserId, UserName, FullName, RoleCode,
        LoginAt, LogoutAt,
        Source, ClientIp, UserAgent,
        CreateAt, UpdateAt, IsActive, Deleted
    )
    VALUES
    (
        @UserId, @UserName, @FullName, @RoleCode,
        @now, NULL,
        @Source, @ClientIp, @UserAgent,
        @now, @now, 1, 0
    );

    SELECT SCOPE_IDENTITY() AS Id;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_LogHistory_update]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_LogHistory_update];
GO

CREATE PROCEDURE [dbo].[sp_LogHistory_update]
(
    @LogId int = NULL,
    @UserId int = NULL,
    @RoleCode nvarchar(50) = NULL,
    @LogoutAt datetime = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @now datetime = ISNULL(@LogoutAt, GETDATE());

    IF (@LogId IS NOT NULL AND @LogId > 0)
    BEGIN
        UPDATE LogHistory
        SET LogoutAt = @now,
            UpdateAt = GETDATE()
        WHERE Id = @LogId;
    END
    ELSE
    BEGIN
        ;WITH LastSession AS
        (
            SELECT TOP 1 Id
            FROM LogHistory
            WHERE (@UserId IS NULL OR UserId = @UserId)
              AND (@RoleCode IS NULL OR RoleCode = @RoleCode)
              AND LogoutAt IS NULL
              AND ISNULL(Deleted, 0) = 0
            ORDER BY LoginAt DESC, Id DESC
        )
        UPDATE LogHistory
        SET LogoutAt = @now,
            UpdateAt = GETDATE()
        WHERE Id IN (SELECT Id FROM LastSession);
    END
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_LogHistory_getAll]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_LogHistory_getAll];
GO

CREATE PROCEDURE [dbo].[sp_LogHistory_getAll]
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
        LoginAt,
        LogoutAt,
        Source,
        ClientIp,
        UserAgent,
        CreateAt,
        UpdateAt
    FROM LogHistory
    WHERE ISNULL(Deleted, 0) = 0
      AND (@FromDate IS NULL OR LoginAt >= @FromDate)
      AND (@ToDate IS NULL OR LoginAt <= @ToDate)
      AND (@UserId IS NULL OR UserId = @UserId)
      AND (@RoleCode IS NULL OR RoleCode = @RoleCode)
      AND (@Token = '' OR UserName LIKE N'%' + @Token + '%' OR FullName LIKE N'%' + @Token + '%')
    ORDER BY LoginAt DESC, Id DESC
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END
GO

PRINT 'Migration V066 completed successfully.';
