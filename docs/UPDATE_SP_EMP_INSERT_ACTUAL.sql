-- =============================================
-- Script cập nhật sp_emp_insert theo cấu trúc thực tế
-- Date: 2024
-- Description: Cập nhật sp_emp_insert với logic tự động tạo UserName và GroupMember
-- =============================================

USE [YourDatabaseName]  -- Thay đổi tên database của bạn
GO

-- =============================================
-- Cập nhật sp_emp_insert
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_emp_insert]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_emp_insert];
    PRINT 'Đã xóa sp_emp_insert cũ';
END
GO

CREATE PROCEDURE [dbo].[sp_emp_insert]
(
    @EducationLevel VARCHAR(4) = NULL, 
    @UserName VARCHAR(8) = NULL,
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
    @Pass VARCHAR(100) = NULL,
    @FullName NVARCHAR(50) = NULL,
    @Email VARCHAR(30) = NULL, 
    @CVLink VARCHAR(500) = '',
    @Noted NVARCHAR(500) = '',
    @DocumentStatus VARCHAR(6) = '',
    @status INT = -1,
    @CreatedBy VARCHAR(5) = NULL,
    @UpdatedBy VARCHAR(5) = NULL,
    @CreateAt DATETIME = NULL, 
    @UpdateAt DATETIME = NULL, 
    @IsActive BIT = 1,
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
    DECLARE @maxId INT = 0;
    
    SELECT @maxId = MAX(id)
    FROM Employees;
    
    SET @maxId = @maxId + 1;
    
    DECLARE @tempUser VARCHAR(6);
    
    IF (@maxId < 10)
    BEGIN 
        SET @tempUser = CONCAT('VS000', @maxId);
    END 
    
    IF (@maxId < 100 AND @maxId >= 10)
    BEGIN 
        SET @tempUser = CONCAT('VS00', @maxId);
    END 
    
    IF (@maxId < 1000 AND @maxId >= 100)
    BEGIN 
        SET @tempUser = CONCAT('VS0', @maxId);
    END 
    
    IF (@maxId >= 1000)
    BEGIN 
        SET @tempUser = CONCAT('VS', @maxId);
    END 
    
    DECLARE @templead INT;
    SET @templead = 0;
    
    SELECT @templead = id 
    FROM Employees 
    WHERE RoleCode IN ('3', '6') AND id = @CreatedBy;
    
    INSERT INTO [dbo].[Employees]
    (
        [UserName],
        [FullName],
        [Phone],
        [Noted],
        [RoleCode],
        [Deleted],
        [IsActive],
        [CreatedBy],
        [UpdatedBy],
        [CreateAt],
        [UpdateAt],
        [Pass],
        [dob],
        [LineCode],
        [Onboard],
        [ColorCode],
        [TypeAccount],
        [DepartmentCode],
        [DocumentStatus],
        [PositionCode],
        [RelationCode],
        [NationalId],
        [NationalDate],
        [NationalPlace],
        [PermanentAddress],
        [TemporaryAddress],
        [ManagerId],
        [Email],
        [CVLink],
        [status],
        [EducationLevel],
        [DocumentCheck],
        -- Các cột mới
        [Gender],
        [PlaceOfBirth],
        [Religion],
        [PersonalEmail],
        [BeneficiaryName]
    )
    VALUES
    (
        @tempUser,
        @FullName,
        @Phone,
        @Noted,
        @RoleCode,
        0,
        @IsActive,
        @CreatedBy,
        @CreatedBy,
        GETDATE(),
        GETDATE(),
        @Pass,
        @Dob,
        '',
        @Onboard,
        '',
        '1',
        @DepartmentCode,
        @DocumentStatus,
        @PositionCode,
        '',
        @NationalId,
        @NationalDate,
        @NationalPlace,
        @PermanentAddress,
        @TemporaryAddress,
        @ManagerId,
        @Email,
        @CVLink,
        @status,
        @EducationLevel,
        @DocumentCheck,
        -- Các giá trị mới
        @Gender,
        @PlaceOfBirth,
        @Religion,
        @PersonalEmail,
        @BeneficiaryName
    );
    
    DECLARE @newId INT;
    SET @newId = @@IDENTITY;
    
    IF (@templead > 0)
    BEGIN 
        DECLARE @groupid INT;
        SET @groupid = -1;
        
        SELECT TOP 1 @groupid = id 
        FROM [Group] 
        WHERE ManagerId = @templead AND ISNULL(Deleted, 0) = 0;
        
        IF (@groupid > 0)
        BEGIN 
            INSERT INTO GroupMember(GroupId, MemberId, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt)
            VALUES (@groupid, @newId, 0, @CreatedBy, @CreatedBy, GETDATE(), GETDATE());
        END 
        
        RETURN;
    END 
    
    SELECT @newId;
END
GO

PRINT 'Đã tạo/cập nhật sp_emp_insert với các trường mới';
GO

