-- =============================================
-- Migration: V069__Add_Contract_And_Report
-- Description: Add Contract management tables, history, procedures, and reporting pages
-- =============================================

PRINT 'Applying migration V069: Contract and reporting...';

-- 1. Contracts table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Contracts]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Contracts](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [EmployeeId] [int] NOT NULL,
        [ContractTypeCode] [nvarchar](50) NOT NULL,
        [StartDate] [datetime] NULL,
        [EndDate] [datetime] NULL,
        [Status] [nvarchar](50) NULL,
        [FileUrl] [nvarchar](500) NULL,
        [Note] [nvarchar](500) NULL,
        [CreateAt] [datetime] NULL DEFAULT GETDATE(),
        [UpdateAt] [datetime] NULL DEFAULT GETDATE(),
        [CreatedBy] [int] NULL,
        [UpdatedBy] [int] NULL,
        [Deleted] [bit] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        CONSTRAINT [PK_Contracts] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Contracts_Employee_EndDate' AND object_id = OBJECT_ID(N'[dbo].[Contracts]'))
BEGIN
    CREATE INDEX [IX_Contracts_Employee_EndDate]
    ON [dbo].[Contracts]([EmployeeId], [EndDate] DESC);
END
GO

-- 2. ContractHistory table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ContractHistory]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[ContractHistory](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [ContractId] [int] NOT NULL,
        [EmployeeId] [int] NOT NULL,
        [Action] [nvarchar](50) NOT NULL,
        [ContractTypeCode] [nvarchar](50) NULL,
        [StartDate] [datetime] NULL,
        [EndDate] [datetime] NULL,
        [Status] [nvarchar](50) NULL,
        [FileUrl] [nvarchar](500) NULL,
        [Note] [nvarchar](500) NULL,
        [CreateAt] [datetime] NULL DEFAULT GETDATE(),
        [CreatedBy] [int] NULL,
        CONSTRAINT [PK_ContractHistory] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ContractHistory_ContractId' AND object_id = OBJECT_ID(N'[dbo].[ContractHistory]'))
BEGIN
    CREATE INDEX [IX_ContractHistory_ContractId]
    ON [dbo].[ContractHistory]([ContractId], [CreateAt] DESC);
END
GO

-- 3. Stored procedures
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Contract_getAll]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Contract_getAll];
GO

CREATE PROCEDURE [dbo].[sp_Contract_getAll]
(
    @Token nvarchar(255) = '',
    @Status nvarchar(50) = NULL,
    @ContractTypeCode nvarchar(50) = NULL,
    @EmployeeId int = NULL,
    @FromDate datetime = NULL,
    @ToDate datetime = NULL,
    @offset int = 0,
    @limit int = 50
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(1) OVER() AS TotalRecord,
        c.Id,
        c.EmployeeId,
        e.FullName,
        e.UserName,
        c.ContractTypeCode,
        md.Name AS ContractTypeName,
        c.StartDate,
        c.EndDate,
        c.Status,
        c.FileUrl,
        c.Note,
        c.CreateAt,
        c.UpdateAt
    FROM Contracts c
    INNER JOIN Employees e ON c.EmployeeId = e.Id
    LEFT JOIN MasterData md ON c.ContractTypeCode = md.Code AND md.TypeData = 1
    WHERE ISNULL(c.Deleted, 0) = 0
      AND (@EmployeeId IS NULL OR c.EmployeeId = @EmployeeId)
      AND (@ContractTypeCode IS NULL OR c.ContractTypeCode = @ContractTypeCode)
      AND (@Status IS NULL OR c.Status = @Status)
      AND (@FromDate IS NULL OR c.StartDate >= @FromDate)
      AND (@ToDate IS NULL OR c.StartDate <= @ToDate)
      AND (@Token = '' OR e.UserName LIKE N'%' + @Token + '%' OR e.FullName LIKE N'%' + @Token + '%')
    ORDER BY c.UpdateAt DESC, c.Id DESC
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Contract_getById]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Contract_getById];
GO

CREATE PROCEDURE [dbo].[sp_Contract_getById]
(
    @Id int
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 c.*, e.FullName, e.UserName, md.Name AS ContractTypeName
    FROM Contracts c
    INNER JOIN Employees e ON c.EmployeeId = e.Id
    LEFT JOIN MasterData md ON c.ContractTypeCode = md.Code AND md.TypeData = 1
    WHERE c.Id = @Id AND ISNULL(c.Deleted, 0) = 0;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Contract_insert]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Contract_insert];
GO

CREATE PROCEDURE [dbo].[sp_Contract_insert]
(
    @EmployeeId int,
    @ContractTypeCode nvarchar(50),
    @StartDate datetime = NULL,
    @EndDate datetime = NULL,
    @Status nvarchar(50) = NULL,
    @FileUrl nvarchar(500) = NULL,
    @Note nvarchar(500) = NULL,
    @CreatedBy int = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Contracts
    (
        EmployeeId, ContractTypeCode, StartDate, EndDate, Status, FileUrl, Note,
        CreateAt, UpdateAt, CreatedBy, UpdatedBy, Deleted, IsActive
    )
    VALUES
    (
        @EmployeeId, @ContractTypeCode, @StartDate, @EndDate, @Status, @FileUrl, @Note,
        GETDATE(), GETDATE(), @CreatedBy, @CreatedBy, 0, 1
    );

    SELECT CAST(SCOPE_IDENTITY() as int) AS Id;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Contract_update]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Contract_update];
GO

CREATE PROCEDURE [dbo].[sp_Contract_update]
(
    @Id int,
    @ContractTypeCode nvarchar(50),
    @StartDate datetime = NULL,
    @EndDate datetime = NULL,
    @Status nvarchar(50) = NULL,
    @Note nvarchar(500) = NULL,
    @UpdatedBy int = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Contracts
    SET ContractTypeCode = @ContractTypeCode,
        StartDate = @StartDate,
        EndDate = @EndDate,
        Status = @Status,
        Note = @Note,
        UpdatedBy = @UpdatedBy,
        UpdateAt = GETDATE()
    WHERE Id = @Id AND ISNULL(Deleted, 0) = 0;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Contract_delete]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Contract_delete];
GO

CREATE PROCEDURE [dbo].[sp_Contract_delete]
(
    @Id int,
    @UpdatedBy int = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Contracts
    SET Deleted = 1,
        UpdatedBy = @UpdatedBy,
        UpdateAt = GETDATE()
    WHERE Id = @Id;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ContractHistory_insert]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_ContractHistory_insert];
GO

CREATE PROCEDURE [dbo].[sp_ContractHistory_insert]
(
    @ContractId int,
    @EmployeeId int,
    @Action nvarchar(50),
    @ContractTypeCode nvarchar(50) = NULL,
    @StartDate datetime = NULL,
    @EndDate datetime = NULL,
    @Status nvarchar(50) = NULL,
    @FileUrl nvarchar(500) = NULL,
    @Note nvarchar(500) = NULL,
    @CreatedBy int = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ContractHistory
    (
        ContractId, EmployeeId, Action, ContractTypeCode,
        StartDate, EndDate, Status, FileUrl, Note,
        CreateAt, CreatedBy
    )
    VALUES
    (
        @ContractId, @EmployeeId, @Action, @ContractTypeCode,
        @StartDate, @EndDate, @Status, @FileUrl, @Note,
        GETDATE(), @CreatedBy
    );
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ContractHistory_getByContract]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_ContractHistory_getByContract];
GO

CREATE PROCEDURE [dbo].[sp_ContractHistory_getByContract]
(
    @ContractId int
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        h.Id,
        h.ContractId,
        h.EmployeeId,
        h.Action,
        h.ContractTypeCode,
        md.Name AS ContractTypeName,
        h.StartDate,
        h.EndDate,
        h.Status,
        h.FileUrl,
        h.Note,
        h.CreateAt,
        h.CreatedBy
    FROM ContractHistory h
    LEFT JOIN MasterData md ON h.ContractTypeCode = md.Code AND md.TypeData = 1
    WHERE h.ContractId = @ContractId
    ORDER BY h.CreateAt DESC, h.Id DESC;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Contract_getExpiring]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Contract_getExpiring];
GO

CREATE PROCEDURE [dbo].[sp_Contract_getExpiring]
(
    @Days int = 30
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @now datetime = GETDATE();
    DECLARE @to datetime = DATEADD(day, @Days, @now);

    SELECT c.*, e.FullName, e.UserName
    FROM Contracts c
    INNER JOIN Employees e ON c.EmployeeId = e.Id
    WHERE ISNULL(c.Deleted, 0) = 0
      AND c.EndDate IS NOT NULL
      AND c.EndDate >= @now
      AND c.EndDate <= @to;
END
GO

-- 4. AppPages + RoleAllow
IF NOT EXISTS (SELECT 1 FROM AppPages WHERE Code = 'Contract')
BEGIN
    INSERT INTO AppPages (Code, Name, [Group], OrderIndex, IsActive)
    VALUES ('Contract', N'Qu?n lý h?p d?ng', 'Human Resource', 12, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM AppPages WHERE Code = 'ReportAnalytics')
BEGIN
    INSERT INTO AppPages (Code, Name, [Group], OrderIndex, IsActive)
    VALUES ('ReportAnalytics', N'Báo cáo & phân tích', 'Report', 91, 1);
END
GO

-- Seed RoleAllow for all roles except TC (RoleCode = '2')
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type in (N'U'))
BEGIN
    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT DISTINCT RoleCode, 'Contract', 1, 1, 1, 1, 0
    FROM RoleAllow r
    WHERE r.RoleCode <> '2'
      AND NOT EXISTS (SELECT 1 FROM RoleAllow ra WHERE ra.RoleCode = r.RoleCode AND ra.PageCode = 'Contract');

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT DISTINCT RoleCode, 'ReportAnalytics', 1, 0, 0, 0, 0
    FROM RoleAllow r
    WHERE r.RoleCode <> '2'
      AND NOT EXISTS (SELECT 1 FROM RoleAllow ra WHERE ra.RoleCode = r.RoleCode AND ra.PageCode = 'ReportAnalytics');
END
GO

PRINT 'Migration V069 completed successfully.';
