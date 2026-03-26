-- =============================================
-- Migration: V007__Create_Permission_Tables
-- Author: System
-- Date: 2025-12-23
-- Description: Create tables for Dynamic Permission System
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V007: Create Permission Tables...';

-- ============================================
-- 1. Create table AppPages
-- ============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AppPages]') AND type in (N'U'))
BEGIN
    PRINT '  → Creating table AppPages...';
    CREATE TABLE [dbo].[AppPages](
        [Code] [varchar](50) NOT NULL,
        [Name] [nvarchar](255) NULL,
        [Group] [nvarchar](100) NULL,
        [IsActive] [bit] DEFAULT 1,
        [OrderIndex] [int] DEFAULT 0,
        PRIMARY KEY CLUSTERED ([Code] ASC)
    );
    PRINT '  ✓ Table AppPages created.';
    
    -- Seed default pages
    PRINT '  → Seeding default AppPages...';
    INSERT INTO [dbo].[AppPages] (Code, Name, [Group], OrderIndex) VALUES
    ('Dashboard', N'Dashboard', 'Common', 1),
    ('Employee', N'Quản lý nhân viên', 'Human Resource', 2),
    ('Candidate', N'Quản lý ứng viên', 'Human Resource', 3),
    ('Job', N'Quản lý tin tuyển dụng', 'Human Resource', 4),
    ('Order', N'Quản lý đơn hàng', 'Sales', 5),
    ('Partner', N'Quản lý đối tác', 'Business', 6),
    ('Report', N'Báo cáo', 'Report', 90),
    ('System', N'Hệ thống', 'System', 99),
    ('Permission', N'Phân quyền', 'System', 100),
    ('LeaveRequest', N'Quản lý nghỉ phép', 'Human Resource', 10);
    
    PRINT '  ✓ Default AppPages seeded.';
END

-- ============================================
-- 2. Create table RoleAllow
-- ============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type in (N'U'))
BEGIN
    PRINT '  → Creating table RoleAllow...';
    CREATE TABLE [dbo].[RoleAllow](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [RoleCode] [varchar](20) NOT NULL,
        [PageCode] [varchar](50) NOT NULL,
        [IsView] [bit] DEFAULT 0,
        [IsAdd] [bit] DEFAULT 0,
        [IsEdit] [bit] DEFAULT 0,
        [IsDelete] [bit] DEFAULT 0,
        [IsApprove] [bit] DEFAULT 0,
        CONSTRAINT [PK_RoleAllow] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT '  ✓ Table RoleAllow created.';
    
    -- Seed Admin permissions (RoleCode = '1' is typically Admin)
    PRINT '  → Seeding Admin permissions...';
    -- Give Admin access to ALL current pages
    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '1', Code, 1, 1, 1, 1, 1 FROM AppPages;
    
    PRINT '  ✓ Admin permissions seeded.';
END

-- ============================================
-- 3. Stored Procedures for Permissions
-- ============================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Permission_GetByRole]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Permission_GetByRole];

PRINT '  → Creating sp_Permission_GetByRole...';
EXEC('
CREATE PROCEDURE [dbo].[sp_Permission_GetByRole]
    @RoleCode varchar(20)
AS
BEGIN
    SELECT 
        p.Code as PageCode,
        p.Name as PageName,
        p.[Group],
        ISNULL(r.IsView, 0) as IsView,
        ISNULL(r.IsAdd, 0) as IsAdd,
        ISNULL(r.IsEdit, 0) as IsEdit,
        ISNULL(r.IsDelete, 0) as IsDelete,
        ISNULL(r.IsApprove, 0) as IsApprove,
        ISNULL(r.Id, 0) as Id
    FROM AppPages p
    LEFT JOIN RoleAllow r ON p.Code = r.PageCode AND r.RoleCode = @RoleCode
    WHERE p.IsActive = 1
    ORDER BY p.[OrderIndex]
END
');

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Permission_Save]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Permission_Save];

PRINT '  → Creating sp_Permission_Save...';
EXEC('
CREATE PROCEDURE [dbo].[sp_Permission_Save]
    @RoleCode varchar(20),
    @PageCode varchar(50),
    @IsView bit,
    @IsAdd bit,
    @IsEdit bit,
    @IsDelete bit,
    @IsApprove bit
AS
BEGIN
    MERGE RoleAllow AS target
    USING (SELECT @RoleCode as RoleCode, @PageCode as PageCode) AS source
    ON (target.RoleCode = source.RoleCode AND target.PageCode = source.PageCode)
    WHEN MATCHED THEN
        UPDATE SET 
            IsView = @IsView, 
            IsAdd = @IsAdd, 
            IsEdit = @IsEdit, 
            IsDelete = @IsDelete,
            IsApprove = @IsApprove
    WHEN NOT MATCHED THEN
        INSERT (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
        VALUES (@RoleCode, @PageCode, @IsView, @IsAdd, @IsEdit, @IsDelete, @IsApprove);
END
');

PRINT '✓ Migration V007 completed successfully';

COMMIT TRANSACTION;
