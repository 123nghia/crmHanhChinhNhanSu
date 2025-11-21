-- =============================================
-- CHẠY SCRIPT NÀY TRONG SSMS/AZURE DATA STUDIO
-- =============================================
-- Đánh dấu migration V002 đã applied
-- (Vì database đã có schema đầy đủ rồi)
-- =============================================

USE humandev;
GO

-- Xóa record V002 nếu có (đã chạy lỗi)
DELETE FROM __MigrationHistory WHERE Version = '002';
GO

-- Đánh dấu V002 đã applied
INSERT INTO __MigrationHistory (Version, Description, AppliedOn, Success, ErrorMessage)
VALUES ('002', 'Full Database Schema (Marked - DB already has schema)', GETDATE(), 1, NULL);
GO

-- Kiểm tra
SELECT * FROM __MigrationHistory ORDER BY Version;
GO

PRINT '✓ Migration V002 đã được đánh dấu!';
PRINT 'Bây giờ chạy: dotnet run --project crmHuman';
GO

