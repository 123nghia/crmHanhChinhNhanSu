-- =============================================
-- Migration: V003__Add_EmergencyContact_Column
-- Author: System
-- Date: 2024-11-21
-- Description: Thêm cột EmergencyContact vào bảng Employees
--              để lưu thông tin liên hệ khẩn cấp
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V003: Add EmergencyContact Column...';

-- Kiểm tra cột có tồn tại chưa
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('Employees') 
    AND name = 'EmergencyContact'
)
BEGIN
    PRINT '  → Adding EmergencyContact column to Employees table';
    ALTER TABLE Employees ADD EmergencyContact NVARCHAR(255) NULL;
    PRINT '  ✓ EmergencyContact column added successfully';
END
ELSE
BEGIN
    PRINT '  → EmergencyContact column already exists, skipping';
END

PRINT '✓ Migration V003 completed successfully';

COMMIT TRANSACTION;

