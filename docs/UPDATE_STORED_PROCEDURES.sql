-- =============================================
-- Script cập nhật Stored Procedures
-- Date: 2024
-- Description: Cập nhật sp_emp_insert, sp_emp_update, sp_tax_insert, sp_tax_udpate
--              để hỗ trợ các trường mới
-- =============================================

USE [YourDatabaseName]  -- Thay đổi tên database của bạn
GO

-- =============================================
-- 1. Cập nhật sp_emp_insert
-- =============================================

-- Kiểm tra xem stored procedure có tồn tại không
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_emp_insert]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_emp_insert];
    PRINT 'Đã xóa sp_emp_insert cũ';
END
GO

CREATE PROCEDURE [dbo].[sp_emp_insert]
    @FullName NVARCHAR(255) = NULL,
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
    
    -- Kiểm tra tên bảng thực tế (có thể là Employees hoặc Employee)
    DECLARE @TableName NVARCHAR(100);
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Employees')
        SET @TableName = 'Employees';
    ELSE IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Employee')
        SET @TableName = 'Employee';
    ELSE
    BEGIN
        RAISERROR('Không tìm thấy bảng Employees hoặc Employee', 16, 1);
        RETURN;
    END
    
    DECLARE @SQL NVARCHAR(MAX);
    
    SET @SQL = N'
    INSERT INTO [dbo].[' + @TableName + '] (
        [FullName], [NationalDate], [NationalId], [NationalPlace],
        [Dob], [Onboard], [Phone], [PositionCode], [RoleCode],
        [UserName], [Pass], [ManagerId], [DepartmentCode],
        [Email], [CVLink], [Noted], [PermanentAddress], [TemporaryAddress],
        [DocumentStatus], [Status], [CreatedBy], [UpdatedBy],
        [CreateAt], [UpdateAt], [IsActive], [EducationLevel],
        [DocumentCheck], [Maritalstatus], [StatusWork],
        [BankAccount], [BankName]';
    
    -- Thêm các cột mới nếu chúng tồn tại
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'Gender')
        SET @SQL = @SQL + N', [Gender]';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'PlaceOfBirth')
        SET @SQL = @SQL + N', [PlaceOfBirth]';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'Religion')
        SET @SQL = @SQL + N', [Religion]';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'PersonalEmail')
        SET @SQL = @SQL + N', [PersonalEmail]';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'BeneficiaryName')
        SET @SQL = @SQL + N', [BeneficiaryName]';
    
    SET @SQL = @SQL + N'
    )
    VALUES (
        @FullName, @NationalDate, @NationalId, @NationalPlace,
        @Dob, @Onboard, @Phone, @PositionCode, @RoleCode,
        @UserName, @Pass, @ManagerId, @DepartmentCode,
        @Email, @CVLink, @Noted, @PermanentAddress, @TemporaryAddress,
        @DocumentStatus, @Status, @CreatedBy, @UpdatedBy,
        @CreateAt, @UpdateAt, @IsActive, @EducationLevel,
        @DocumentCheck, @Maritalstatus, @StatusWork,
        @BankAccount, @BankName';
    
    -- Thêm các giá trị mới nếu cột tồn tại
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'Gender')
        SET @SQL = @SQL + N', @Gender';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'PlaceOfBirth')
        SET @SQL = @SQL + N', @PlaceOfBirth';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'Religion')
        SET @SQL = @SQL + N', @Religion';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'PersonalEmail')
        SET @SQL = @SQL + N', @PersonalEmail';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'BeneficiaryName')
        SET @SQL = @SQL + N', @BeneficiaryName';
    
    SET @SQL = @SQL + N'
    );
    
    SELECT SCOPE_IDENTITY() AS Id;';
    
    EXEC sp_executesql @SQL,
        N'@FullName NVARCHAR(255), @NationalDate DATETIME, @NationalId NVARCHAR(50), @NationalPlace NVARCHAR(255),
          @Dob DATETIME, @Onboard DATETIME, @Phone NVARCHAR(20), @PositionCode NVARCHAR(50), @RoleCode NVARCHAR(50),
          @UserName NVARCHAR(100), @Pass NVARCHAR(255), @ManagerId INT, @DepartmentCode NVARCHAR(50),
          @Email NVARCHAR(255), @CVLink NVARCHAR(500), @Noted NVARCHAR(MAX), @PermanentAddress NVARCHAR(MAX),
          @TemporaryAddress NVARCHAR(MAX), @DocumentStatus NVARCHAR(50), @Status INT, @CreatedBy INT, @UpdatedBy INT,
          @CreateAt DATETIME, @UpdateAt DATETIME, @IsActive INT, @EducationLevel NVARCHAR(50),
          @DocumentCheck NVARCHAR(MAX), @Maritalstatus NVARCHAR(50), @StatusWork NVARCHAR(50),
          @BankAccount NVARCHAR(50), @BankName NVARCHAR(255),
          @Gender NVARCHAR(50), @PlaceOfBirth NVARCHAR(255), @Religion NVARCHAR(100),
          @PersonalEmail NVARCHAR(255), @BeneficiaryName NVARCHAR(255)',
        @FullName, @NationalDate, @NationalId, @NationalPlace,
        @Dob, @Onboard, @Phone, @PositionCode, @RoleCode,
        @UserName, @Pass, @ManagerId, @DepartmentCode,
        @Email, @CVLink, @Noted, @PermanentAddress, @TemporaryAddress,
        @DocumentStatus, @Status, @CreatedBy, @UpdatedBy,
        @CreateAt, @UpdateAt, @IsActive, @EducationLevel,
        @DocumentCheck, @Maritalstatus, @StatusWork,
        @BankAccount, @BankName,
        @Gender, @PlaceOfBirth, @Religion, @PersonalEmail, @BeneficiaryName;
END
GO

PRINT 'Đã tạo/cập nhật sp_emp_insert';
GO

-- =============================================
-- 2. Cập nhật sp_emp_update
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_emp_update]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_emp_update];
    PRINT 'Đã xóa sp_emp_update cũ';
END
GO

CREATE PROCEDURE [dbo].[sp_emp_update]
    @Id INT,
    @FullName NVARCHAR(255) = NULL,
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
    
    -- Kiểm tra tên bảng thực tế
    DECLARE @TableName NVARCHAR(100);
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Employees')
        SET @TableName = 'Employees';
    ELSE IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Employee')
        SET @TableName = 'Employee';
    ELSE
    BEGIN
        RAISERROR('Không tìm thấy bảng Employees hoặc Employee', 16, 1);
        RETURN;
    END
    
    DECLARE @SQL NVARCHAR(MAX);
    
    SET @SQL = N'
    UPDATE [dbo].[' + @TableName + ']
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
        [BankName] = @BankName';
    
    -- Thêm các cột mới nếu chúng tồn tại
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'Gender')
        SET @SQL = @SQL + N', [Gender] = @Gender';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'PlaceOfBirth')
        SET @SQL = @SQL + N', [PlaceOfBirth] = @PlaceOfBirth';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'Religion')
        SET @SQL = @SQL + N', [Religion] = @Religion';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'PersonalEmail')
        SET @SQL = @SQL + N', [PersonalEmail] = @PersonalEmail';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'BeneficiaryName')
        SET @SQL = @SQL + N', [BeneficiaryName] = @BeneficiaryName';
    
    SET @SQL = @SQL + N'
    WHERE [Id] = @Id;
    
    SELECT @@ROWCOUNT AS RowsAffected;';
    
    EXEC sp_executesql @SQL,
        N'@Id INT, @FullName NVARCHAR(255), @NationalDate DATETIME, @NationalId NVARCHAR(50), @NationalPlace NVARCHAR(255),
          @Dob DATETIME, @Onboard DATETIME, @Phone NVARCHAR(20), @PositionCode NVARCHAR(50), @RoleCode NVARCHAR(50),
          @ManagerId INT, @DepartmentCode NVARCHAR(50), @Email NVARCHAR(255), @CVLink NVARCHAR(500), @Noted NVARCHAR(MAX),
          @PermanentAddress NVARCHAR(MAX), @TemporaryAddress NVARCHAR(MAX), @DocumentStatus NVARCHAR(50), @Status INT,
          @UpdatedBy INT, @UpdateAt DATETIME, @IsActive INT, @EducationLevel NVARCHAR(50), @DocumentCheck NVARCHAR(MAX),
          @Maritalstatus NVARCHAR(50), @StatusWork NVARCHAR(50), @BankAccount NVARCHAR(50), @BankName NVARCHAR(255),
          @Gender NVARCHAR(50), @PlaceOfBirth NVARCHAR(255), @Religion NVARCHAR(100),
          @PersonalEmail NVARCHAR(255), @BeneficiaryName NVARCHAR(255)',
        @Id, @FullName, @NationalDate, @NationalId, @NationalPlace,
        @Dob, @Onboard, @Phone, @PositionCode, @RoleCode,
        @ManagerId, @DepartmentCode, @Email, @CVLink, @Noted,
        @PermanentAddress, @TemporaryAddress, @DocumentStatus, @Status,
        @UpdatedBy, @UpdateAt, @IsActive, @EducationLevel, @DocumentCheck,
        @Maritalstatus, @StatusWork, @BankAccount, @BankName,
        @Gender, @PlaceOfBirth, @Religion, @PersonalEmail, @BeneficiaryName;
END
GO

PRINT 'Đã tạo/cập nhật sp_emp_update';
GO

-- =============================================
-- 3. Cập nhật sp_tax_insert
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_insert]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_tax_insert];
    PRINT 'Đã xóa sp_tax_insert cũ';
END
GO

CREATE PROCEDURE [dbo].[sp_tax_insert]
    @UserName NVARCHAR(100) = NULL,
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
    
    DECLARE @SQL NVARCHAR(MAX);
    
    SET @SQL = N'
    INSERT INTO [dbo].[TaxItem] (
        [UserName], [CodeId], [Number], [PageTax], [BiaSo], [RegBHYT], [CreatedBy]';
    
    -- Thêm các cột mới nếu chúng tồn tại
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TaxItem' AND COLUMN_NAME = 'PITDate')
        SET @SQL = @SQL + N', [PITDate]';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TaxItem' AND COLUMN_NAME = 'EffectedFrom')
        SET @SQL = @SQL + N', [EffectedFrom]';
    
    SET @SQL = @SQL + N'
    )
    VALUES (
        @UserName, @CodeId, @Number, @PageTax, @BiaSo, @RegBHYT, @CreatedBy';
    
    -- Thêm các giá trị mới nếu cột tồn tại
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TaxItem' AND COLUMN_NAME = 'PITDate')
        SET @SQL = @SQL + N', @PITDate';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TaxItem' AND COLUMN_NAME = 'EffectedFrom')
        SET @SQL = @SQL + N', @EffectedFrom';
    
    SET @SQL = @SQL + N'
    );
    
    SELECT SCOPE_IDENTITY() AS Id;';
    
    EXEC sp_executesql @SQL,
        N'@UserName NVARCHAR(100), @CodeId NVARCHAR(50), @Number NVARCHAR(50), @PageTax INT, @BiaSo INT,
          @RegBHYT NVARCHAR(50), @CreatedBy INT, @PITDate DATETIME, @EffectedFrom DATETIME',
        @UserName, @CodeId, @Number, @PageTax, @BiaSo, @RegBHYT, @CreatedBy, @PITDate, @EffectedFrom;
END
GO

PRINT 'Đã tạo/cập nhật sp_tax_insert';
GO

-- =============================================
-- 4. Cập nhật sp_tax_udpate (hoặc sp_tax_update)
-- =============================================

-- Kiểm tra tên stored procedure thực tế
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_udpate]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_tax_udpate];
    PRINT 'Đã xóa sp_tax_udpate cũ';
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_update]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_tax_update];
    PRINT 'Đã xóa sp_tax_update cũ';
END
GO

-- Tạo cả hai tên để đảm bảo tương thích
CREATE PROCEDURE [dbo].[sp_tax_udpate]
    @Id INT,
    @UserName NVARCHAR(100) = NULL,
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
    
    DECLARE @SQL NVARCHAR(MAX);
    
    SET @SQL = N'
    UPDATE [dbo].[TaxItem]
    SET 
        [UserName] = @UserName,
        [CodeId] = @CodeId,
        [Number] = @Number,
        [PageTax] = @PageTax,
        [BiaSo] = @BiaSo,
        [RegBHYT] = @RegBHYT,
        [UpdatedBy] = @UpdatedBy';
    
    -- Thêm các cột mới nếu chúng tồn tại
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TaxItem' AND COLUMN_NAME = 'PITDate')
        SET @SQL = @SQL + N', [PITDate] = @PITDate';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TaxItem' AND COLUMN_NAME = 'EffectedFrom')
        SET @SQL = @SQL + N', [EffectedFrom] = @EffectedFrom';
    
    SET @SQL = @SQL + N'
    WHERE [Id] = @Id;
    
    SELECT @@ROWCOUNT AS RowsAffected;';
    
    EXEC sp_executesql @SQL,
        N'@Id INT, @UserName NVARCHAR(100), @CodeId NVARCHAR(50), @Number NVARCHAR(50), @PageTax INT, @BiaSo INT,
          @RegBHYT NVARCHAR(50), @UpdatedBy INT, @PITDate DATETIME, @EffectedFrom DATETIME',
        @Id, @UserName, @CodeId, @Number, @PageTax, @BiaSo, @RegBHYT, @UpdatedBy, @PITDate, @EffectedFrom;
END
GO

PRINT 'Đã tạo/cập nhật sp_tax_udpate';
GO

PRINT '=============================================';
PRINT 'Đã cập nhật tất cả stored procedures!';
PRINT '=============================================';
GO

