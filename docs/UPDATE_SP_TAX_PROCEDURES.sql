-- =============================================
-- Script cập nhật TaxItem Stored Procedures
-- Date: 2024
-- Description: Cập nhật sp_tax_insert, sp_tax_udpate với các trường mới
-- =============================================

USE [YourDatabaseName]  -- Thay đổi tên database của bạn
GO

PRINT '=============================================';
PRINT 'Bắt đầu cập nhật TaxItem stored procedures...';
PRINT '=============================================';
GO

-- =============================================
-- 1. Cập nhật sp_tax_insert
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_insert]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_tax_insert];
    PRINT 'Đã xóa sp_tax_insert cũ';
END
GO

CREATE PROCEDURE [dbo].[sp_tax_insert]
(
    @UserName VARCHAR(8) = NULL,
    @Number VARCHAR(20) = NULL,
    @PageTax INT = NULL,
    @CodeId NVARCHAR(50) = NULL,
    @BiaSo VARCHAR(4) = NULL,
    @RegBHYT NVARCHAR(150) = NULL,
    @CreatedBy INT = NULL,
    -- Các tham số mới
    @PITDate DATETIME = NULL,
    @EffectedFrom DATETIME = NULL
)
AS
BEGIN
    INSERT INTO [dbo].[TaxItem]
    (
        [UserName],
        [CodeId],
        [PageTax],
        [BiaSo],
        [Deleted],
        [IsActive],
        [CreatedBy],
        [UpdatedBy],
        [CreateAt],
        [UpdateAt],
        [RegBHYT],
        -- Các cột mới
        [PITDate],
        [EffectedFrom]
    )
    VALUES
    (
        @UserName,
        @CodeId,
        @PageTax,
        @BiaSo,
        0,
        0,
        @CreatedBy,
        @CreatedBy,
        GETDATE(),
        GETDATE(),
        @RegBHYT,
        -- Các giá trị mới
        @PITDate,
        @EffectedFrom
    );
END
GO

PRINT 'Đã tạo/cập nhật sp_tax_insert với các trường mới';
GO

-- =============================================
-- 2. Cập nhật sp_tax_udpate (hoặc sp_tax_update)
-- =============================================

-- Kiểm tra tên stored procedure thực tế
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_udpate]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_tax_udpate];
    PRINT 'Đã xóa sp_tax_udpate cũ';
END
GO

-- Tạo sp_tax_udpate (có thể là typo, nhưng giữ nguyên để tương thích)
CREATE PROCEDURE [dbo].[sp_tax_udpate]
(
    @Id INT,
    @UserName VARCHAR(8) = NULL,
    @CodeId NVARCHAR(50) = NULL,
    @Number VARCHAR(20) = NULL,
    @PageTax INT = NULL,
    @BiaSo VARCHAR(4) = NULL,
    @RegBHYT NVARCHAR(150) = NULL,
    @UpdatedBy INT = NULL,
    -- Các tham số mới
    @PITDate DATETIME = NULL,
    @EffectedFrom DATETIME = NULL
)
AS
BEGIN
    UPDATE [dbo].[TaxItem]
    SET 
        [UserName] = @UserName,
        [CodeId] = @CodeId,
        [Number] = @Number,
        [PageTax] = @PageTax,
        [BiaSo] = @BiaSo,
        [RegBHYT] = @RegBHYT,
        [UpdatedBy] = @UpdatedBy,
        [UpdateAt] = GETDATE(),
        -- Các cột mới
        [PITDate] = @PITDate,
        [EffectedFrom] = @EffectedFrom
    WHERE [Id] = @Id;
END
GO

PRINT 'Đã tạo/cập nhật sp_tax_udpate với các trường mới';
GO

-- Tạo thêm sp_tax_update (tên đúng chính tả) nếu cần
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_update]') AND type in (N'P', N'PC'))
BEGIN
    CREATE PROCEDURE [dbo].[sp_tax_update]
    (
        @Id INT,
        @UserName VARCHAR(8) = NULL,
        @CodeId NVARCHAR(50) = NULL,
        @Number VARCHAR(20) = NULL,
        @PageTax INT = NULL,
        @BiaSo VARCHAR(4) = NULL,
        @RegBHYT NVARCHAR(150) = NULL,
        @UpdatedBy INT = NULL,
        -- Các tham số mới
        @PITDate DATETIME = NULL,
        @EffectedFrom DATETIME = NULL
    )
    AS
    BEGIN
        UPDATE [dbo].[TaxItem]
        SET 
            [UserName] = @UserName,
            [CodeId] = @CodeId,
            [Number] = @Number,
            [PageTax] = @PageTax,
            [BiaSo] = @BiaSo,
            [RegBHYT] = @RegBHYT,
            [UpdatedBy] = @UpdatedBy,
            [UpdateAt] = GETDATE(),
            -- Các cột mới
            [PITDate] = @PITDate,
            [EffectedFrom] = @EffectedFrom
        WHERE [Id] = @Id;
    END
    GO
    
    PRINT 'Đã tạo sp_tax_update với các trường mới';
END
GO

PRINT '=============================================';
PRINT 'Hoàn thành cập nhật TaxItem stored procedures!';
PRINT '=============================================';
GO

