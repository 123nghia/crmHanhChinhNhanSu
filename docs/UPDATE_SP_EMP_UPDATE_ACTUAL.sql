-- =============================================
-- Script cập nhật sp_emp_update theo cấu trúc thực tế
-- Date: 2024
-- Description: Cập nhật sp_emp_update với các trường mới
-- =============================================

USE [YourDatabaseName]  -- Thay đổi tên database của bạn
GO

-- =============================================
-- Cập nhật sp_emp_update
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_emp_update]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_emp_update];
    PRINT 'Đã xóa sp_emp_update cũ';
END
GO

CREATE PROCEDURE [dbo].[sp_emp_update]
(
    @id INT,
    @NationalDate DATETIME = NULL, 
    @NationalPlace NVARCHAR(500) = '',
    @PermanentAddress NVARCHAR(500) = '',
    @TemporaryAddress NVARCHAR(500) = '',
    @Dob DATETIME = NULL,
    @Onboard DATETIME = NULL,
    @Phone VARCHAR(10) = NULL,
    @PositionCode VARCHAR(4) = '',
    @RoleCode VARCHAR(4) = '1',
    @ManagerId INT = -1,
    @DepartmentCode VARCHAR(4) = '',
    @NationalId VARCHAR(11) = '',
    @FullName NVARCHAR(50) = NULL,
    @Email VARCHAR(50) = NULL, 
    @CVLink VARCHAR(500) = '', 
    @Noted NVARCHAR(500) = '',
    @DocumentStatus INT = -1,
    @status INT = -1,
    @StatusWork INT = -1,
    @UpdatedBy VARCHAR(5) = NULL,
    @UpdateAt DATETIME = NULL, 
    @IsActive BIT = 1,
    @SourceFrom INT = 0,
    @BankName NVARCHAR(100) = NULL, 
    @BankAccount VARCHAR(30) = NULL,
    @EducationLevel VARCHAR(4) = NULL, 
    @Maritalstatus VARCHAR(4) = NULL,
    @DocumentCheck VARCHAR(200) = '',
    -- Các tham số mới
    @Gender NVARCHAR(50) = NULL,
    @PlaceOfBirth NVARCHAR(255) = NULL,
    @Religion NVARCHAR(100) = NULL,
    @PersonalEmail NVARCHAR(255) = NULL,
    @BeneficiaryName NVARCHAR(255) = NULL
)
AS
BEGIN
    UPDATE Employees 
    SET 
        FullName = @FullName,
        NationalDate = @NationalDate,
        NationalId = @NationalId,
        NationalPlace = @NationalPlace,
        dob = @Dob, 
        Noted = @Noted,
        RoleCode = @RoleCode,
        phone = @Phone,
        PositionCode = @PositionCode,
        DepartmentCode = @DepartmentCode,
        DocumentStatus = @DocumentStatus,
        [status] = @status,
        ManagerId = @ManagerId,
        CVLink = @CVLink,
        Email = @Email,
        Onboard = @Onboard,
        UpdateAt = @UpdateAt,
        UpdatedBy = @UpdatedBy,
        StatusWork = @StatusWork,
        TemporaryAddress = @TemporaryAddress, 
        PermanentAddress = @PermanentAddress,
        BankName = @BankName,
        BankAccount = @BankAccount,
        EducationLevel = @EducationLevel,
        Maritalstatus = @Maritalstatus,
        DocumentCheck = @DocumentCheck,
        -- Các cột mới
        Gender = @Gender,
        PlaceOfBirth = @PlaceOfBirth,
        Religion = @Religion,
        PersonalEmail = @PersonalEmail,
        BeneficiaryName = @BeneficiaryName
    WHERE id = @id;
    
    IF (@SourceFrom > 0)
    BEGIN 
        UPDATE Employees 
        SET SourceFrom = @SourceFrom
        WHERE id = @id;
    END 
END
GO

PRINT 'Đã tạo/cập nhật sp_emp_update với các trường mới';
GO

