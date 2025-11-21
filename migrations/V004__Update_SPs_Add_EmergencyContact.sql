-- =============================================
-- Migration: V004__Update_SPs_Add_EmergencyContact
-- Author: System
-- Date: 2024-11-21
-- Description: Update stored procedures sp_emp_insert và sp_emp_update
--              để thêm parameter @EmergencyContact
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V004: Update SPs Add EmergencyContact...';

-- ============================================
-- 1. Update sp_emp_insert
-- ============================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('sp_emp_insert') AND type = 'P')
BEGIN
    PRINT '  → Dropping existing sp_emp_insert...';
    DROP PROCEDURE sp_emp_insert;
END

PRINT '  → Creating sp_emp_insert with EmergencyContact parameter...';

EXEC('
CREATE PROCEDURE [dbo].[sp_emp_insert]
(
    @EducationLevel VARCHAR(4) = NULL, 
    @UserName VARCHAR(8) = NULL,
    @NationalDate DATETIME = NULL, 
    @NationalPlace NVARCHAR(500) = '''',
    @PermanentAddress NVARCHAR(500) = '''',
    @TemporaryAddress NVARCHAR(500) = '''',
    @Dob DATETIME = NULL,
    @Onboard DATETIME = NULL,
    @Phone VARCHAR(10) = NULL,
    @PositionCode VARCHAR(4) = '''',
    @RoleCode VARCHAR(4) = ''1'',
    @ManagerId INT = -1,
    @DepartmentCode VARCHAR(4) = '''',
    @NationalId VARCHAR(11) = '''',
    @Pass VARCHAR(100) = NULL,
    @FullName NVARCHAR(50) = NULL,
    @Email VARCHAR(30) = NULL, 
    @CVLink VARCHAR(500) = '''',
    @Noted NVARCHAR(500) = '''',
    @DocumentStatus VARCHAR(6) = '''',
    @status INT = -1,
    @CreatedBy VARCHAR(5) = NULL,
    @UpdatedBy VARCHAR(5) = NULL,
    @CreateAt DATETIME = NULL, 
    @UpdateAt DATETIME = NULL, 
    @IsActive BIT = 1,
    @DocumentCheck VARCHAR(200) = '''',
    @Gender NVARCHAR(50) = NULL,
    @PlaceOfBirth NVARCHAR(255) = NULL,
    @Religion NVARCHAR(100) = NULL,
    @PersonalEmail NVARCHAR(255) = NULL,
    @BeneficiaryName NVARCHAR(255) = NULL,
    @EmergencyContact NVARCHAR(255) = NULL,
    @Maritalstatus VARCHAR(4) = NULL,
    @StatusWork INT = -1,
    @BankAccount VARCHAR(30) = NULL,
    @BankName NVARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @Now DATETIME = GETDATE();
        
        INSERT INTO [dbo].[Employees]
        (
            UserName, FullName, Phone, Noted, RoleCode, Deleted, IsActive,
            CreatedBy, UpdatedBy, CreateAt, UpdateAt, Pass, dob, LineCode,
            Onboard, ColorCode, TypeAccount, DepartmentCode, DocumentStatus,
            PositionCode, RelationCode, NationalId, NationalDate, NationalPlace,
            PermanentAddress, TemporaryAddress, ManagerId, Email, CVLink, status,
            EducationLevel, DocumentCheck, Gender, PlaceOfBirth, Religion,
            PersonalEmail, BeneficiaryName, EmergencyContact, Maritalstatus, StatusWork,
            BankAccount, BankName
        )
        VALUES
        (
            NULL, @FullName, @Phone, @Noted, @RoleCode, 0, @IsActive,
            @CreatedBy, @CreatedBy, @Now, @Now, @Pass, @Dob, '''',
            @Onboard, '''', ''1'', @DepartmentCode, @DocumentStatus,
            @PositionCode, '''', @NationalId, @NationalDate, @NationalPlace,
            @PermanentAddress, @TemporaryAddress, @ManagerId, @Email, @CVLink,
            @status, @EducationLevel, @DocumentCheck, @Gender, @PlaceOfBirth,
            @Religion, @PersonalEmail, @BeneficiaryName, @EmergencyContact, @Maritalstatus,
            @StatusWork, @BankAccount, @BankName
        );
        
        DECLARE @NewId INT = SCOPE_IDENTITY();
        DECLARE @NewUserName VARCHAR(8) = RIGHT(''000000'' + CAST(@NewId AS VARCHAR), 6);
        
        UPDATE [dbo].[Employees] SET UserName = @NewUserName WHERE Id = @NewId;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
');

PRINT '  ✓ sp_emp_insert updated successfully';

-- ============================================
-- 2. Update sp_emp_update
-- ============================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('sp_emp_update') AND type = 'P')
BEGIN
    PRINT '  → Dropping existing sp_emp_update...';
    DROP PROCEDURE sp_emp_update;
END

PRINT '  → Creating sp_emp_update with EmergencyContact parameter...';

EXEC('
CREATE PROCEDURE [dbo].[sp_emp_update]
(
    @id INT,
    @FullName NVARCHAR(50) = NULL,
    @NationalId VARCHAR(11) = NULL,
    @NationalDate DATETIME = NULL,
    @NationalPlace NVARCHAR(500) = NULL,
    @PermanentAddress NVARCHAR(500) = NULL,
    @TemporaryAddress NVARCHAR(500) = NULL,
    @Dob DATETIME = NULL,
    @Onboard DATETIME = NULL,
    @Phone VARCHAR(10) = NULL,
    @PositionCode VARCHAR(4) = NULL,
    @RoleCode VARCHAR(4) = NULL,
    @ManagerId INT = NULL,
    @DepartmentCode VARCHAR(4) = NULL,
    @Email VARCHAR(30) = NULL,
    @CVLink VARCHAR(500) = NULL,
    @Noted NVARCHAR(500) = NULL,
    @DocumentStatus VARCHAR(6) = NULL,
    @Status INT = NULL,
    @UpdatedBy VARCHAR(5) = NULL,
    @UpdateAt DATETIME = NULL,
    @IsActive BIT = NULL,
    @BankAccount VARCHAR(30) = NULL,
    @BankName NVARCHAR(100) = NULL,
    @EducationLevel VARCHAR(4) = NULL,
    @Maritalstatus VARCHAR(4) = NULL,
    @DocumentCheck VARCHAR(200) = NULL,
    @StatusWork INT = NULL,
    @Gender NVARCHAR(50) = NULL,
    @PlaceOfBirth NVARCHAR(255) = NULL,
    @Religion NVARCHAR(100) = NULL,
    @PersonalEmail NVARCHAR(255) = NULL,
    @BeneficiaryName NVARCHAR(255) = NULL,
    @EmergencyContact NVARCHAR(255) = NULL,
    @SourceFrom INT = -1
)
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE Employees
    SET FullName = ISNULL(@FullName, FullName),
        NationalId = ISNULL(@NationalId, NationalId),
        NationalDate = ISNULL(@NationalDate, NationalDate),
        NationalPlace = ISNULL(@NationalPlace, NationalPlace),
        PermanentAddress = ISNULL(@PermanentAddress, PermanentAddress),
        TemporaryAddress = ISNULL(@TemporaryAddress, TemporaryAddress),
        Dob = ISNULL(@Dob, Dob),
        Onboard = ISNULL(@Onboard, Onboard),
        Phone = ISNULL(@Phone, Phone),
        PositionCode = ISNULL(@PositionCode, PositionCode),
        RoleCode = ISNULL(@RoleCode, RoleCode),
        ManagerId = ISNULL(@ManagerId, ManagerId),
        DepartmentCode = ISNULL(@DepartmentCode, DepartmentCode),
        Email = ISNULL(@Email, Email),
        CVLink = ISNULL(@CVLink, CVLink),
        Noted = ISNULL(@Noted, Noted),
        DocumentStatus = ISNULL(@DocumentStatus, DocumentStatus),
        Status = ISNULL(@Status, Status),
        UpdatedBy = ISNULL(@UpdatedBy, UpdatedBy),
        UpdateAt = ISNULL(@UpdateAt, GETDATE()),
        IsActive = ISNULL(@IsActive, IsActive),
        BankAccount = ISNULL(@BankAccount, BankAccount),
        BankName = ISNULL(@BankName, BankName),
        EducationLevel = ISNULL(@EducationLevel, EducationLevel),
        Maritalstatus = ISNULL(@Maritalstatus, Maritalstatus),
        DocumentCheck = ISNULL(@DocumentCheck, DocumentCheck),
        StatusWork = ISNULL(@StatusWork, StatusWork),
        Gender = ISNULL(@Gender, Gender),
        PlaceOfBirth = ISNULL(@PlaceOfBirth, PlaceOfBirth),
        Religion = ISNULL(@Religion, Religion),
        PersonalEmail = ISNULL(@PersonalEmail, PersonalEmail),
        BeneficiaryName = ISNULL(@BeneficiaryName, BeneficiaryName),
        EmergencyContact = ISNULL(@EmergencyContact, EmergencyContact)
    WHERE id = @id;
    
    IF (@SourceFrom > 0)
    BEGIN
        UPDATE Employees SET SourceFrom = @SourceFrom WHERE id = @id;
    END
END
');

PRINT '  ✓ sp_emp_update updated successfully';

PRINT '✓ Migration V004 completed successfully';

COMMIT TRANSACTION;

