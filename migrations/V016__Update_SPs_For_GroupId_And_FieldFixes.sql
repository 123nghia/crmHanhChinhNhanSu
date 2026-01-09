-- Migration V015: Update sp_emp_update and sp_emp_insert to support GroupId and fix DocumentStatus type
-- Description: Adds GroupId parameter, updates FullName length, fixes DocumentStatus type to VARCHAR, and adds GroupMember synchronization logic.

GO
/****** Object:  StoredProcedure [dbo].[sp_emp_update] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_emp_update]
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
    @FullName NVARCHAR(255) = NULL, -- Increased to 255
    @Email VARCHAR(50) = NULL,
    @CVLink VARCHAR(500) = '',
    @Noted NVARCHAR(500) = '',
    @DocumentStatus VARCHAR(10) = '', -- Changed to VARCHAR to match table
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
    -- New fields
    @Gender NVARCHAR(50) = NULL,
    @PlaceOfBirth NVARCHAR(255) = NULL,
    @Religion NVARCHAR(100) = NULL,
    @PersonalEmail NVARCHAR(255) = NULL,
    @BeneficiaryName NVARCHAR(255) = NULL,
    @EmergencyContact NVARCHAR(255) = NULL,
    @GroupId INT = -1 -- Added GroupId support
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
        EmergencyContact = @EmergencyContact
    WHERE id = @id;

    -- Sync Group information to GroupMember table
    IF (@GroupId > 0)
    BEGIN
        IF EXISTS (SELECT 1 FROM GroupMember WHERE MemberId = @id AND ISNULL(Deleted, 0) = 0)
        BEGIN
            UPDATE GroupMember 
            SET GroupId = @GroupId, UpdateAt = GETDATE(), UpdatedBy = @UpdatedBy
            WHERE MemberId = @id AND ISNULL(Deleted, 0) = 0
            
            -- Optional: Mark other group memberships as deleted if needed
            -- UPDATE GroupMember SET Deleted = 1 WHERE MemberId = @id AND GroupId <> @GroupId
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

/****** Object:  StoredProcedure [dbo].[sp_emp_insert] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_emp_insert]
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
    @FullName NVARCHAR(255) = NULL, -- Increased to 255
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
    @Gender NVARCHAR(50) = NULL,
    @PlaceOfBirth NVARCHAR(255) = NULL,
    @Religion NVARCHAR(100) = NULL,
    @PersonalEmail NVARCHAR(255) = NULL,
    @BeneficiaryName NVARCHAR(255) = NULL,
    @Maritalstatus VARCHAR(4) = NULL,
    @StatusWork INT = -1,     
    @BankAccount VARCHAR(30) = NULL,
    @BankName NVARCHAR(100) = NULL,
    @EmergencyContact NVARCHAR(255) = NULL,
    @GroupId INT = -1 -- Added GroupId support
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
            PersonalEmail, BeneficiaryName, Maritalstatus, StatusWork,
            BankAccount, BankName, EmergencyContact
        )
        VALUES
        (
            NULL, @FullName, @Phone, @Noted, @RoleCode, 0, @IsActive,
            @CreatedBy, @CreatedBy, @Now, @Now, @Pass, @Dob, '',
            @Onboard, '', '1', @DepartmentCode, @DocumentStatus,
            @PositionCode, '', @NationalId, @NationalDate, @NationalPlace,
            @PermanentAddress, @TemporaryAddress, @ManagerId, @Email, @CVLink,
            @status, @EducationLevel, @DocumentCheck, @Gender, @PlaceOfBirth,
            @Religion, @PersonalEmail, @BeneficiaryName, @Maritalstatus,
            @StatusWork, @BankAccount, @BankName, @EmergencyContact
        );

        DECLARE @newId INT = SCOPE_IDENTITY();
        -- Generate UserName VSXXXX
        DECLARE @tempUser VARCHAR(6);
        SET @tempUser = CONCAT('VS', RIGHT('000' + CAST(@newId AS VARCHAR(4)), 4));       

        UPDATE Employees SET UserName = @tempUser WHERE Id = @newId;

        -- Assign to selected Group or default Lead's group
        DECLARE @targetGroupId INT = @GroupId;
        IF (@targetGroupId <= 0)
        BEGIN
            SELECT TOP 1 @targetGroupId = Id FROM [Group] WHERE ManagerId = @CreatedBy AND ISNULL(Deleted,0) = 0;
        END

        IF @targetGroupId > 0
        BEGIN
            INSERT INTO GroupMember(GroupId, MemberId, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt)
            VALUES (@targetGroupId, @newId, 0, @CreatedBy, @CreatedBy, @Now, @Now);
        END

        COMMIT TRANSACTION;   
        SELECT @newId;        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
