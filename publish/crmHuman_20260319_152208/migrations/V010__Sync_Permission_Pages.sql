-- =============================================
-- Migration: V010__Sync_Permission_Pages
-- Author: System
-- Date: 2025-12-23
-- Description: Sync Permission - Remove Order, Add CloudStorage
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V010: Sync Permission Pages...'

-- ============================================
-- 1. Remove "Order" (Quản lý đơn hàng) - không sử dụng
-- ============================================
PRINT '  → Removing Order from AppPages...'

-- First remove from RoleAllow
DELETE FROM RoleAllow WHERE PageCode = 'Order';

-- Then remove from AppPages
DELETE FROM AppPages WHERE Code = 'Order';

PRINT '  ✓ Order removed.'

-- ============================================
-- 2. Add "CloudStorage" (Kho tài liệu)
-- ============================================
PRINT '  → Adding CloudStorage to AppPages...'

IF NOT EXISTS (SELECT 1 FROM AppPages WHERE Code = 'CloudStorage')
BEGIN
    INSERT INTO AppPages (Code, Name, [Group], OrderIndex, IsActive)
    VALUES ('CloudStorage', N'Kho tài liệu', 'Common', 8, 1);
    
    -- Add permission for Admin role (RoleCode = '1')
    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    VALUES ('1', 'CloudStorage', 1, 1, 1, 1, 1);
    
    PRINT '  ✓ CloudStorage added with Admin permissions.'
END
ELSE
BEGIN
    PRINT '  → CloudStorage already exists, skipping.'
END

PRINT '✓ Migration V010 completed successfully'

COMMIT TRANSACTION;
