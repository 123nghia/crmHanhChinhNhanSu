-- =============================================
-- Migration: V053__Fix_Employee_UserName_Length
-- Author: System
-- Date: 2026-01-18
-- Description: Expand UserName columns and update SPs to support longer usernames.
-- =============================================

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'UserName')
BEGIN
    ALTER TABLE [dbo].[Employees] ALTER COLUMN [UserName] VARCHAR(100) NULL;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BHXHItem]') AND name = N'UserName')
BEGIN
    ALTER TABLE [dbo].[BHXHItem] ALTER COLUMN [UserName] VARCHAR(100) NULL;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TaxItem]') AND name = N'UserName')
BEGIN
    ALTER TABLE [dbo].[TaxItem] ALTER COLUMN [UserName] VARCHAR(100) NULL;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RelationItem]') AND name = N'UserName')
BEGIN
    ALTER TABLE [dbo].[RelationItem] ALTER COLUMN [UserName] VARCHAR(100) NULL;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_emp_insert]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_emp_insert];
GO

CREATE PROCEDURE [dbo].[sp_emp_insert]
(
    @EducationLevel VARCHAR(8) = NULL,
    @UserName VARCHAR(100) = NULL,
    @NationalDate DATETIME = NULL,
    @NationalPlace NVARCHAR(500) = '',
    @PermanentAddress NVARCHAR(500) = '',
    @TemporaryAddress NVARCHAR(500) = '',
    @Dob DATETIME = NULL,
    @Onboard DATETIME = NULL,
    @ResignationDate DATETIME = NULL,
    @Phone VARCHAR(10) = NULL,
    @PositionCode VARCHAR(4) = '',
    @RoleCode VARCHAR(4) = '1',
    @ManagerId INT = -1,
    @DepartmentCode VARCHAR(4) = '',
    @NationalId VARCHAR(11) = '',
    @Pass VARCHAR(100) = NULL,
    @FullName NVARCHAR(255) = NULL,
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
    @Ethnicity VARCHAR(8) = NULL,
    @Religion NVARCHAR(100) = NULL,
    @PersonalEmail NVARCHAR(255) = NULL,
    @BeneficiaryName NVARCHAR(255) = NULL,
    @Maritalstatus VARCHAR(4) = NULL,
    @StatusWork INT = -1,
    @BankAccount VARCHAR(30) = NULL,
    @BankName NVARCHAR(100) = NULL,
    @EmergencyContact NVARCHAR(255) = NULL,
    @FingerprintCode VARCHAR(50) = NULL,
    @GroupId INT = -1
)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @Now DATETIME = GETDATE();
        DECLARE @FinalUserName VARCHAR(100);
        DECLARE @tempUser VARCHAR(6);

        SET @FinalUserName = NULLIF(LTRIM(RTRIM(@UserName)), '');

        INSERT INTO [dbo].[Employees]
        (
            UserName, FullName, Phone, Noted, RoleCode, Deleted, IsActive,
            CreatedBy, UpdatedBy, CreateAt, UpdateAt, Pass, dob, LineCode,
            Onboard, ResignationDate, ColorCode, TypeAccount, DepartmentCode, DocumentStatus,
            PositionCode, RelationCode, NationalId, NationalDate, NationalPlace,
            PermanentAddress, TemporaryAddress, ManagerId, Email, CVLink, status,
            EducationLevel, DocumentCheck, Gender, PlaceOfBirth, Ethnicity, Religion,
            PersonalEmail, BeneficiaryName, Maritalstatus, StatusWork,
            BankAccount, BankName, EmergencyContact, FingerprintCode
        )
        VALUES
        (
            @FinalUserName, @FullName, @Phone, @Noted, @RoleCode, 0, @IsActive,
            @CreatedBy, @CreatedBy, @Now, @Now, @Pass, @Dob, '',
            @Onboard, @ResignationDate, '', '1', @DepartmentCode, @DocumentStatus,
            @PositionCode, '', @NationalId, @NationalDate, @NationalPlace,
            @PermanentAddress, @TemporaryAddress, @ManagerId, @Email, @CVLink,
            @status, @EducationLevel, @DocumentCheck, @Gender, @PlaceOfBirth, @Ethnicity,
            @Religion, @PersonalEmail, @BeneficiaryName, @Maritalstatus,
            @StatusWork, @BankAccount, @BankName, @EmergencyContact, @FingerprintCode
        );

        DECLARE @newId INT = SCOPE_IDENTITY();
        IF (@FinalUserName IS NULL)
        BEGIN
            SET @tempUser = CONCAT('VS', RIGHT('000' + CAST(@newId AS VARCHAR(4)), 4));
            UPDATE Employees SET UserName = @tempUser WHERE Id = @newId;
        END

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

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_emp_login]') AND type IN (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_emp_login];
END
GO

CREATE PROCEDURE [dbo].[sp_emp_login]
(
    @userName VARCHAR(100) = NULL,
    @password VARCHAR(300) = NULL
)
AS
BEGIN
    SELECT * FROM Employees d WHERE d.UserName = @userName AND ISNULL(d.Deleted,0) = 0;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_BHXHItem_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_BHXHItem_getAll];
END
GO

CREATE PROCEDURE [dbo].[sp_BHXHItem_getAll]
(
    @UserName VARCHAR(100) = ''
)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 * FROM [dbo].[BHXHItem] WHERE [UserName] = @UserName;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_tax_getAll];
END
GO

CREATE PROCEDURE [dbo].[sp_tax_getAll]
(
    @userid VARCHAR(100) = ''
)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 * FROM [dbo].[TaxItem] WHERE [UserName] = @userid ORDER BY [Id] DESC;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_RelationItem_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_RelationItem_getAll];
END
GO

CREATE PROCEDURE [dbo].[sp_RelationItem_getAll]
(
    @UserName VARCHAR(100) = ''
)
AS
BEGIN
    SELECT * FROM RelationItem WHERE UserName = @UserName;
END
GO
