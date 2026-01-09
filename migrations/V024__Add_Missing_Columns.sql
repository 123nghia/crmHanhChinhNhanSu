-- Migration V024: Fix DocumentData table and Employee Update SP
-- Description: Adds IsActive column to DocumentData and updates sp_emp_update to include IsActive.

GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DocumentData]') AND name = 'IsActive')
BEGIN
    ALTER TABLE [dbo].[DocumentData] ADD [IsActive] [int] NULL DEFAULT 1;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_emp_update]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_emp_update];
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
    @FullName NVARCHAR(255) = NULL,
    @Email VARCHAR(50) = NULL,
    @CVLink VARCHAR(500) = '',
    @Noted NVARCHAR(500) = '',
    @DocumentStatus VARCHAR(10) = '',
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
    @Gender NVARCHAR(50) = NULL,
    @PlaceOfBirth NVARCHAR(255) = NULL,
    @Religion NVARCHAR(100) = NULL,
    @PersonalEmail NVARCHAR(255) = NULL,
    @BeneficiaryName NVARCHAR(255) = NULL,
    @EmergencyContact NVARCHAR(255) = NULL,
    @GroupId INT = -1
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
        Gender = @Gender,     
        PlaceOfBirth = @PlaceOfBirth,
        Religion = @Religion, 
        PersonalEmail = @PersonalEmail,
        BeneficiaryName = @BeneficiaryName,
        EmergencyContact = @EmergencyContact,
        IsActive = @IsActive
    WHERE id = @id;

    -- Sync Group information to GroupMember table
    IF (@GroupId > 0)
    BEGIN
        IF EXISTS (SELECT 1 FROM GroupMember WHERE MemberId = @id AND ISNULL(Deleted, 0) = 0)
        BEGIN
            UPDATE GroupMember 
            SET GroupId = @GroupId, UpdateAt = GETDATE(), UpdatedBy = @UpdatedBy
            WHERE MemberId = @id AND ISNULL(Deleted, 0) = 0
        END
        ELSE
        BEGIN
            INSERT INTO GroupMember(GroupId, MemberId, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt)
            VALUES (@GroupId, @id, 0, @UpdatedBy, @UpdatedBy, GETDATE(), GETDATE())
        END
    END

    IF (@SourceFrom > 0)      
    BEGIN
        UPDATE Employees      
        SET SourceFrom = @SourceFrom
        WHERE id = @id;       
    END
END
GO
