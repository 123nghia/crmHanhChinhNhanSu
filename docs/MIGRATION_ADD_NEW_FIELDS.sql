-- =============================================
-- Migration Script: Thêm các trường mới vào database
-- Date: 2024
-- Description: Thêm các trường mới cho Employee và TaxItem theo yêu cầu
-- =============================================

USE [YourDatabaseName]  -- Thay đổi tên database của bạn
GO

-- =============================================
-- 1. Thêm các cột mới vào bảng Employees
-- =============================================

-- Kiểm tra và thêm cột Gender (Giới tính)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = 'Gender')
BEGIN
    ALTER TABLE [dbo].[Employees]
    ADD [Gender] NVARCHAR(50) NULL;
    PRINT 'Đã thêm cột Gender vào bảng Employees';
END
ELSE
BEGIN
    PRINT 'Cột Gender đã tồn tại trong bảng Employees';
END
GO

-- Kiểm tra và thêm cột PlaceOfBirth (Nơi sinh)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = 'PlaceOfBirth')
BEGIN
    ALTER TABLE [dbo].[Employees]
    ADD [PlaceOfBirth] NVARCHAR(255) NULL;
    PRINT 'Đã thêm cột PlaceOfBirth vào bảng Employees';
END
ELSE
BEGIN
    PRINT 'Cột PlaceOfBirth đã tồn tại trong bảng Employees';
END
GO

-- Kiểm tra và thêm cột Religion (Tôn giáo)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = 'Religion')
BEGIN
    ALTER TABLE [dbo].[Employees]
    ADD [Religion] NVARCHAR(100) NULL;
    PRINT 'Đã thêm cột Religion vào bảng Employees';
END
ELSE
BEGIN
    PRINT 'Cột Religion đã tồn tại trong bảng Employees';
END
GO

-- Kiểm tra và thêm cột PersonalEmail (Email cá nhân)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = 'PersonalEmail')
BEGIN
    ALTER TABLE [dbo].[Employees]
    ADD [PersonalEmail] NVARCHAR(255) NULL;
    PRINT 'Đã thêm cột PersonalEmail vào bảng Employees';
END
ELSE
BEGIN
    PRINT 'Cột PersonalEmail đã tồn tại trong bảng Employees';
END
GO

-- Kiểm tra và thêm cột BeneficiaryName (Tên chủ tài khoản)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = 'BeneficiaryName')
BEGIN
    ALTER TABLE [dbo].[Employees]
    ADD [BeneficiaryName] NVARCHAR(255) NULL;
    PRINT 'Đã thêm cột BeneficiaryName vào bảng Employees';
END
ELSE
BEGIN
    PRINT 'Cột BeneficiaryName đã tồn tại trong bảng Employees';
END
GO

-- =============================================
-- 2. Thêm các cột mới vào bảng TaxItem
-- =============================================

-- Kiểm tra và thêm cột PITDate (Ngày cấp mã số thuế)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TaxItem]') AND name = 'PITDate')
BEGIN
    ALTER TABLE [dbo].[TaxItem]
    ADD [PITDate] DATETIME NULL;
    PRINT 'Đã thêm cột PITDate vào bảng TaxItem';
END
ELSE
BEGIN
    PRINT 'Cột PITDate đã tồn tại trong bảng TaxItem';
END
GO

-- Kiểm tra và thêm cột EffectedFrom (Hiệu lực từ)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TaxItem]') AND name = 'EffectedFrom')
BEGIN
    ALTER TABLE [dbo].[TaxItem]
    ADD [EffectedFrom] DATETIME NULL;
    PRINT 'Đã thêm cột EffectedFrom vào bảng TaxItem';
END
ELSE
BEGIN
    PRINT 'Cột EffectedFrom đã tồn tại trong bảng TaxItem';
END
GO

-- =============================================
-- 3. Cập nhật Stored Procedure: sp_emp_insert
-- =============================================
-- Lưu ý: Cần cập nhật stored procedure để nhận các tham số mới
-- Dưới đây là ví dụ, bạn cần điều chỉnh theo cấu trúc thực tế của stored procedure

/*
ALTER PROCEDURE [dbo].[sp_emp_insert]
    @FullName NVARCHAR(255),
    @NationalDate DATETIME = NULL,
    @NationalId NVARCHAR(50) = NULL,
    @NationalPlace NVARCHAR(255) = NULL,
    @Dob DATETIME = NULL,
    @Onboard DATETIME = NULL,
    @Phone NVARCHAR(20) = NULL,
    @PositionCode NVARCHAR(50) = NULL,
    @RoleCode NVARCHAR(50) = NULL,
    @UserName NVARCHAR(100) = NULL,
    @Pass NVARCHAR(255) = NULL,
    @ManagerId INT = NULL,
    @DepartmentCode NVARCHAR(50) = NULL,
    @Email NVARCHAR(255) = NULL,
    @CVLink NVARCHAR(500) = NULL,
    @Noted NVARCHAR(MAX) = NULL,
    @PermanentAddress NVARCHAR(MAX) = NULL,
    @TemporaryAddress NVARCHAR(MAX) = NULL,
    @DocumentStatus NVARCHAR(50) = NULL,
    @Status INT = 0,
    @CreatedBy INT = NULL,
    @UpdatedBy INT = NULL,
    @CreateAt DATETIME = NULL,
    @UpdateAt DATETIME = NULL,
    @IsActive INT = 1,
    @EducationLevel NVARCHAR(50) = NULL,
    @DocumentCheck NVARCHAR(MAX) = NULL,
    @Maritalstatus NVARCHAR(50) = NULL,
    @StatusWork NVARCHAR(50) = NULL,
    @BankAccount NVARCHAR(50) = NULL,
    @BankName NVARCHAR(255) = NULL,
    -- Các tham số mới
    @Gender NVARCHAR(50) = NULL,
    @PlaceOfBirth NVARCHAR(255) = NULL,
    @Religion NVARCHAR(100) = NULL,
    @PersonalEmail NVARCHAR(255) = NULL,
    @BeneficiaryName NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[Employees] (
        [FullName], [NationalDate], [NationalId], [NationalPlace],
        [Dob], [Onboard], [Phone], [PositionCode], [RoleCode],
        [UserName], [Pass], [ManagerId], [DepartmentCode],
        [Email], [CVLink], [Noted], [PermanentAddress], [TemporaryAddress],
        [DocumentStatus], [Status], [CreatedBy], [UpdatedBy],
        [CreateAt], [UpdateAt], [IsActive], [EducationLevel],
        [DocumentCheck], [Maritalstatus], [StatusWork],
        [BankAccount], [BankName],
        -- Các cột mới
        [Gender], [PlaceOfBirth], [Religion], [PersonalEmail], [BeneficiaryName]
    )
    VALUES (
        @FullName, @NationalDate, @NationalId, @NationalPlace,
        @Dob, @Onboard, @Phone, @PositionCode, @RoleCode,
        @UserName, @Pass, @ManagerId, @DepartmentCode,
        @Email, @CVLink, @Noted, @PermanentAddress, @TemporaryAddress,
        @DocumentStatus, @Status, @CreatedBy, @UpdatedBy,
        @CreateAt, @UpdateAt, @IsActive, @EducationLevel,
        @DocumentCheck, @Maritalstatus, @StatusWork,
        @BankAccount, @BankName,
        -- Các giá trị mới
        @Gender, @PlaceOfBirth, @Religion, @PersonalEmail, @BeneficiaryName
    );
    
    SELECT SCOPE_IDENTITY() AS Id;
END
GO
*/

-- =============================================
-- 4. Cập nhật Stored Procedure: sp_emp_update
-- =============================================
-- Lưu ý: Cần cập nhật stored procedure để nhận các tham số mới

/*
ALTER PROCEDURE [dbo].[sp_emp_update]
    @Id INT,
    @FullName NVARCHAR(255),
    @NationalDate DATETIME = NULL,
    @NationalId NVARCHAR(50) = NULL,
    @NationalPlace NVARCHAR(255) = NULL,
    @Dob DATETIME = NULL,
    @Onboard DATETIME = NULL,
    @Phone NVARCHAR(20) = NULL,
    @PositionCode NVARCHAR(50) = NULL,
    @RoleCode NVARCHAR(50) = NULL,
    @ManagerId INT = NULL,
    @DepartmentCode NVARCHAR(50) = NULL,
    @Email NVARCHAR(255) = NULL,
    @CVLink NVARCHAR(500) = NULL,
    @Noted NVARCHAR(MAX) = NULL,
    @PermanentAddress NVARCHAR(MAX) = NULL,
    @TemporaryAddress NVARCHAR(MAX) = NULL,
    @DocumentStatus NVARCHAR(50) = NULL,
    @Status INT = 0,
    @UpdatedBy INT = NULL,
    @UpdateAt DATETIME = NULL,
    @IsActive INT = 1,
    @EducationLevel NVARCHAR(50) = NULL,
    @DocumentCheck NVARCHAR(MAX) = NULL,
    @Maritalstatus NVARCHAR(50) = NULL,
    @StatusWork NVARCHAR(50) = NULL,
    @BankAccount NVARCHAR(50) = NULL,
    @BankName NVARCHAR(255) = NULL,
    -- Các tham số mới
    @Gender NVARCHAR(50) = NULL,
    @PlaceOfBirth NVARCHAR(255) = NULL,
    @Religion NVARCHAR(100) = NULL,
    @PersonalEmail NVARCHAR(255) = NULL,
    @BeneficiaryName NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [dbo].[Employees]
    SET 
        [FullName] = @FullName,
        [NationalDate] = @NationalDate,
        [NationalId] = @NationalId,
        [NationalPlace] = @NationalPlace,
        [Dob] = @Dob,
        [Onboard] = @Onboard,
        [Phone] = @Phone,
        [PositionCode] = @PositionCode,
        [RoleCode] = @RoleCode,
        [ManagerId] = @ManagerId,
        [DepartmentCode] = @DepartmentCode,
        [Email] = @Email,
        [CVLink] = @CVLink,
        [Noted] = @Noted,
        [PermanentAddress] = @PermanentAddress,
        [TemporaryAddress] = @TemporaryAddress,
        [DocumentStatus] = @DocumentStatus,
        [Status] = @Status,
        [UpdatedBy] = @UpdatedBy,
        [UpdateAt] = @UpdateAt,
        [IsActive] = @IsActive,
        [EducationLevel] = @EducationLevel,
        [DocumentCheck] = @DocumentCheck,
        [Maritalstatus] = @Maritalstatus,
        [StatusWork] = @StatusWork,
        [BankAccount] = @BankAccount,
        [BankName] = @BankName,
        -- Cập nhật các cột mới
        [Gender] = @Gender,
        [PlaceOfBirth] = @PlaceOfBirth,
        [Religion] = @Religion,
        [PersonalEmail] = @PersonalEmail,
        [BeneficiaryName] = @BeneficiaryName
    WHERE [Id] = @Id;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
*/

-- =============================================
-- 5. Cập nhật Stored Procedure: sp_tax_insert
-- =============================================

/*
ALTER PROCEDURE [dbo].[sp_tax_insert]
    @UserName NVARCHAR(100),
    @CodeId NVARCHAR(50) = NULL,
    @Number NVARCHAR(50) = NULL,
    @PageTax INT = NULL,
    @BiaSo INT = NULL,
    @RegBHYT NVARCHAR(50) = NULL,
    @CreatedBy INT = NULL,
    -- Các tham số mới
    @PITDate DATETIME = NULL,
    @EffectedFrom DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[TaxItem] (
        [UserName], [CodeId], [Number], [PageTax], [BiaSo], [RegBHYT], [CreatedBy],
        -- Các cột mới
        [PITDate], [EffectedFrom]
    )
    VALUES (
        @UserName, @CodeId, @Number, @PageTax, @BiaSo, @RegBHYT, @CreatedBy,
        -- Các giá trị mới
        @PITDate, @EffectedFrom
    );
    
    SELECT SCOPE_IDENTITY() AS Id;
END
GO
*/

-- =============================================
-- 6. Cập nhật Stored Procedure: sp_tax_udpate
-- =============================================

/*
ALTER PROCEDURE [dbo].[sp_tax_udpate]
    @Id INT,
    @UserName NVARCHAR(100),
    @CodeId NVARCHAR(50) = NULL,
    @Number NVARCHAR(50) = NULL,
    @PageTax INT = NULL,
    @BiaSo INT = NULL,
    @RegBHYT NVARCHAR(50) = NULL,
    @UpdatedBy INT = NULL,
    -- Các tham số mới
    @PITDate DATETIME = NULL,
    @EffectedFrom DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [dbo].[TaxItem]
    SET 
        [UserName] = @UserName,
        [CodeId] = @CodeId,
        [Number] = @Number,
        [PageTax] = @PageTax,
        [BiaSo] = @BiaSo,
        [RegBHYT] = @RegBHYT,
        [UpdatedBy] = @UpdatedBy,
        -- Cập nhật các cột mới
        [PITDate] = @PITDate,
        [EffectedFrom] = @EffectedFrom
    WHERE [Id] = @Id;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
*/

-- =============================================
-- 7. Kiểm tra kết quả
-- =============================================

-- Kiểm tra các cột đã được thêm vào bảng Employees
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Employees'
    AND COLUMN_NAME IN ('Gender', 'PlaceOfBirth', 'Religion', 'PersonalEmail', 'BeneficiaryName')
ORDER BY COLUMN_NAME;
GO

-- Kiểm tra các cột đã được thêm vào bảng TaxItem
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TaxItem'
    AND COLUMN_NAME IN ('PITDate', 'EffectedFrom')
ORDER BY COLUMN_NAME;
GO

PRINT '=============================================';
PRINT 'Migration script đã hoàn thành!';
PRINT 'Vui lòng kiểm tra và cập nhật các stored procedures theo cấu trúc thực tế của bạn.';
PRINT '=============================================';
GO

