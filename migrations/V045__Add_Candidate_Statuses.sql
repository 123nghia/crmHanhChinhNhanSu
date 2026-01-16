-- =============================================
-- Migration: V045__Add_Candidate_Statuses
-- Author: System
-- Date: 2026-01-16
-- Description: Add statuses for Candidate Onboarding Process (Pass, Pending Employee, Official Employee)
-- =============================================

BEGIN TRANSACTION;

-- TypeData = 9 is Candidate Status (as inferred from code)

-- 1. Pass Interview (Đậu phỏng vấn) - Code 92
IF NOT EXISTS (SELECT 1 FROM MasterData WHERE TypeData = 9 AND Code = '92')
BEGIN
    INSERT INTO MasterData (Code, Name, TypeData, IsActive, CreatedBy, CreateAt, UpdateAt)
    VALUES ('92', N'Đậu phỏng vấn', 9, 1, 1, GETDATE(), GETDATE());
END
ELSE
BEGIN
    UPDATE MasterData SET Name = N'Đậu phỏng vấn' WHERE TypeData = 9 AND Code = '92';
END

-- 2. Pending Employee (Nhân viên chờ) - Code 93
-- User Description: "Nhân viên (dạng chờ)", "Vẫn truy cập với ứng viên"
IF NOT EXISTS (SELECT 1 FROM MasterData WHERE TypeData = 9 AND Code = '93')
BEGIN
    INSERT INTO MasterData (Code, Name, TypeData, IsActive, CreatedBy, CreateAt, UpdateAt)
    VALUES ('93', N'Nhân viên chờ', 9, 1, 1, GETDATE(), GETDATE());
END
ELSE
BEGIN
    UPDATE MasterData SET Name = N'Nhân viên chờ' WHERE TypeData = 9 AND Code = '93';
END

-- 3. Official Employee (Nhân viên chính thức) - Code 94
-- This corresponds to "Onboarded" usually.
IF NOT EXISTS (SELECT 1 FROM MasterData WHERE TypeData = 9 AND Code = '94')
BEGIN
    INSERT INTO MasterData (Code, Name, TypeData, IsActive, CreatedBy, CreateAt, UpdateAt)
    VALUES ('94', N'Nhân viên chính thức', 9, 1, 1, GETDATE(), GETDATE());
END
ELSE
BEGIN
    UPDATE MasterData SET Name = N'Nhân viên chính thức' WHERE TypeData = 9 AND Code = '94';
END

-- Ensure "Onboarded" (if exists) maps to 94 or is consistent
-- If previous code relied on name "Onboarded", we might keep it or update it.
-- Let's update any existing "Onboarded" to "Nhân viên chính thức" if it exists with a different code, 
-- or just assume 94 is the standard now.

COMMIT TRANSACTION;
