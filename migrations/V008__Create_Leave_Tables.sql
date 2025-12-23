-- =============================================
-- Migration: V008__Create_Leave_Tables
-- Author: System
-- Date: 2025-12-23
-- Description: Create LeaveRequests table and seed MasterData and AppPages
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V008: Create Leave Tables...';

-- ============================================
-- 1. Seed MasterData for Leave Types (TypeData = 30)
-- ============================================

IF NOT EXISTS (SELECT * FROM MasterData WHERE TypeData = 30)
BEGIN
    PRINT '  → Seeding Leave Types in MasterData...';
    INSERT INTO [dbo].[MasterData] (Code, Name, TypeData, IsActive, Deleted, CreateAt) VALUES
    ('NP', N'Nghỉ phép năm', 30, 1, 0, GETDATE()),
    ('NB', N'Nghỉ bệnh', 30, 1, 0, GETDATE()),
    ('NVR', N'Nghỉ việc riêng', 30, 1, 0, GETDATE()),
    ('NTS', N'Nghỉ thai sản', 30, 1, 0, GETDATE()),
    ('NKL', N'Nghỉ không lương', 30, 1, 0, GETDATE());
    PRINT '  ✓ Leave Types seeded.';
END

-- ============================================
-- 2. Seed AppPages (if not already seeded in V007)
-- ============================================
IF NOT EXISTS (SELECT * FROM AppPages WHERE Code = 'LeaveApproval')
BEGIN
    PRINT '  → Seeding LeaveApproval AppPage...';
    INSERT INTO [dbo].[AppPages] (Code, Name, [Group], OrderIndex) VALUES
    ('LeaveApproval', N'Duyệt nghỉ phép', 'Human Resource', 11);
END

-- ============================================
-- 3. Create table LeaveRequests
-- ============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequests]') AND type in (N'U'))
BEGIN
    PRINT '  → Creating table LeaveRequests...';
    CREATE TABLE [dbo].[LeaveRequests](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [EmployeeId] [int] NOT NULL,
        [LeaveTypeCode] [varchar](8) NOT NULL,
        [FromDate] [datetime] NOT NULL,
        [ToDate] [datetime] NOT NULL,
        [NumDays] [decimal](18, 2) NULL,
        [Reason] [nvarchar](max) NULL,
        [Status] [int] DEFAULT 0, -- 0: Pending, 1: Approved, 2: Rejected
        [ApproverId] [int] NULL,
        [Comment] [nvarchar](max) NULL,
        [ApproveAt] [datetime] NULL,
        [Deleted] [bit] DEFAULT 0,
        [CreatedBy] [int] NULL,
        [CreateAt] [datetime] DEFAULT GETDATE(),
        [UpdatedBy] [int] NULL,
        [UpdateAt] [datetime] DEFAULT GETDATE(),
        PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT '  ✓ Table LeaveRequests created.';
END

-- ============================================
-- 4. Stored Procedures
-- ============================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Leave_GetAll]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Leave_GetAll];

PRINT '  → Creating sp_Leave_GetAll...';
EXEC('
CREATE PROCEDURE [dbo].[sp_Leave_GetAll]
    @EmployeeId int = NULL,
    @Status int = NULL,
    @FromDate datetime = NULL,
    @ToDate datetime = NULL,
    @Page int = 1,
    @Limit int = 20
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Offset int = (@Page - 1) * @Limit;

    SELECT 
        l.*,
        e.FullName as EmployeeName,
        mt.Name as LeaveTypeName,
        ap.FullName as ApproverName,
        COUNT(*) OVER() as TotalRecord
    FROM LeaveRequests l
    JOIN Employees e ON l.EmployeeId = e.Id
    JOIN MasterData mt ON l.LeaveTypeCode = mt.Code AND mt.TypeData = 30
    LEFT JOIN Employees ap ON l.ApproverId = ap.Id
    WHERE l.Deleted = 0
      AND (@EmployeeId IS NULL OR l.EmployeeId = @EmployeeId)
      AND (@Status IS NULL OR l.Status = @Status)
      AND (@FromDate IS NULL OR l.FromDate >= @FromDate)
      AND (@ToDate IS NULL OR l.ToDate <= @ToDate)
    ORDER BY l.CreateAt DESC
    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
END
');

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Leave_GetById]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Leave_GetById];

PRINT '  → Creating sp_Leave_GetById...';
EXEC('
CREATE PROCEDURE [dbo].[sp_Leave_GetById]
    @Id int
AS
BEGIN
    SELECT 
        l.*,
        e.FullName as EmployeeName,
        mt.Name as LeaveTypeName,
        ap.FullName as ApproverName
    FROM LeaveRequests l
    JOIN Employees e ON l.EmployeeId = e.Id
    JOIN MasterData mt ON l.LeaveTypeCode = mt.Code AND mt.TypeData = 30
    LEFT JOIN Employees ap ON l.ApproverId = ap.Id
    WHERE l.Id = @Id;
END
');

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Leave_Save]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Leave_Save];

PRINT '  → Creating sp_Leave_Save...';
EXEC('
CREATE PROCEDURE [dbo].[sp_Leave_Save]
    @Id int = 0,
    @EmployeeId int,
    @LeaveTypeCode varchar(8),
    @FromDate datetime,
    @ToDate datetime,
    @NumDays decimal(18,2),
    @Reason nvarchar(max),
    @UserId int
AS
BEGIN
    IF @Id > 0
    BEGIN
        UPDATE LeaveRequests SET
            LeaveTypeCode = @LeaveTypeCode,
            FromDate = @FromDate,
            ToDate = @ToDate,
            NumDays = @NumDays,
            Reason = @Reason,
            UpdatedBy = @UserId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;
        SELECT @Id;
    END
    ELSE
    BEGIN
        INSERT INTO LeaveRequests (EmployeeId, LeaveTypeCode, FromDate, ToDate, NumDays, Reason, Status, CreatedBy, CreateAt, UpdatedBy, UpdateAt, Deleted)
        VALUES (@EmployeeId, @LeaveTypeCode, @FromDate, @ToDate, @NumDays, @Reason, 0, @UserId, GETDATE(), @UserId, GETDATE(), 0);
        SELECT SCOPE_IDENTITY();
    END
END
');

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Leave_Approve]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Leave_Approve];

PRINT '  → Creating sp_Leave_Approve...';
EXEC('
CREATE PROCEDURE [dbo].[sp_Leave_Approve]
    @Id int,
    @Status int, -- 1: Approved, 2: Rejected
    @ApproverId int,
    @Comment nvarchar(max)
AS
BEGIN
    UPDATE LeaveRequests SET
        Status = @Status,
        ApproverId = @ApproverId,
        Comment = @Comment,
        ApproveAt = GETDATE(),
        UpdateAt = GETDATE(),
        UpdatedBy = @ApproverId
    WHERE Id = @Id;
END
');

-- Seed Admin permissions for the new LeaveApproval page
PRINT '  → Seeding Admin permissions for LeaveApproval...';
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type in (N'U'))
BEGIN
    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '1', 'LeaveApproval', 1, 1, 1, 1, 1
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '1' AND PageCode = 'LeaveApproval');
    
    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '1', 'LeaveRequest', 1, 1, 1, 1, 1
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '1' AND PageCode = 'LeaveRequest');
END

PRINT '✓ Migration V008 completed successfully';

COMMIT TRANSACTION;
