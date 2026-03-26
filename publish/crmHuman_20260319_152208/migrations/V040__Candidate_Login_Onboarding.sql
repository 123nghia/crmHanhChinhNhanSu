-- =============================================
-- Migration: V040__Candidate_Login_Onboarding
-- Author: System
-- Date: 2026-01-16
-- Description: Add candidate login fields + onboarding helpers
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V040: Candidate login + onboarding...';

-- ============================================
-- 1. Extend Candidate table
-- ============================================
IF COL_LENGTH('dbo.Candidate', 'UserName') IS NULL
BEGIN
    PRINT '  + Adding UserName to Candidate...';
    ALTER TABLE [dbo].[Candidate]
    ADD [UserName] [varchar](50) NULL;
END
ELSE
BEGIN
    PRINT '  + UserName already exists, skipping.';
END

IF COL_LENGTH('dbo.Candidate', 'Pass') IS NULL
BEGIN
    PRINT '  + Adding Pass to Candidate...';
    ALTER TABLE [dbo].[Candidate]
    ADD [Pass] [varchar](100) NULL;
END
ELSE
BEGIN
    PRINT '  + Pass already exists, skipping.';
END

IF COL_LENGTH('dbo.Candidate', 'EmployeeId') IS NULL
BEGIN
    PRINT '  + Adding EmployeeId to Candidate...';
    ALTER TABLE [dbo].[Candidate]
    ADD [EmployeeId] [int] NULL;
END
ELSE
BEGIN
    PRINT '  + EmployeeId already exists, skipping.';
END

-- ============================================
-- 2. Seed Candidate status "Onboarded" if missing
-- ============================================
IF NOT EXISTS (
    SELECT 1 FROM MasterData
    WHERE TypeData = 9 AND Name = 'Onboarded' AND ISNULL(Deleted, 0) = 0
)
BEGIN
    DECLARE @nextId INT = ISNULL((SELECT MAX(Id) FROM MasterData WHERE TypeData = 9), 0) + 1;
    DECLARE @code VARCHAR(8) = CONCAT('9', @nextId);

    INSERT INTO MasterData (Code, Name, TypeData, IsActive, Deleted, CreateAt, Extra, ApplyFor)
    VALUES (@code, 'Onboarded', 9, 1, 0, GETDATE(), 'green', 2);
END

-- ============================================
-- 3. Update sp_candidate_insert
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_candidate_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  + Dropping existing procedure sp_candidate_insert...';
    DROP PROCEDURE [dbo].[sp_candidate_insert];
END

PRINT '  + Creating procedure sp_candidate_insert...';
EXEC('CREATE procedure [dbo].[sp_candidate_insert]
(
    @Name nvarchar(50) null,
    @ShortDes varchar(100) null,
    @Email varchar(50) null,
    @Source int  = 0,
    @Phone varchar (10) null,
    @Dob datetime =null,
    @Status int  = 1,
    @AvatarLink varchar(300) = '''',
    @CVLink varchar(300) ='''',
    @CreatedBy varchar(5) null,
    @DepartmentId int = -1,
    @Position int =-1,
    @ManagerId int = -1,
    @Referrer nvarchar(200) ='''',
    @Noted nvarchar(500) = '''',
    @IsActive bit = null,
    @NationalId varchar(20) = null,
    @Address nvarchar(500) = null,
    @UserName varchar(50) = null,
    @Pass varchar(100) = null
)
as
begin

declare @maxId  int =0 ;
 select
@maxId = max(id)
from Candidate
set @maxId = @maxId +1;
declare @tempUser  varchar(6);
if(@maxId < 10)
begin
    set @tempUser = concat(''CA000'', @maxId)
end
else if(@maxId < 100)
begin
    set @tempUser = concat(''CA00'', @maxId)
end

else if(@maxId < 1000)
begin
    set @tempUser = concat(''CA0'', @maxId)
end
else
begin
    set @tempUser = concat(''CA'', @maxId)
end

declare @finalUserName varchar(50);
set @finalUserName = @UserName;
if(@finalUserName is null or LTRIM(RTRIM(@finalUserName)) = '''')
begin
    set @finalUserName = @tempUser;
end

declare @templead int;
set @templead = 0;
declare @roleCode varchar(3);
declare @assingee1  int;
set @assingee1 =-1;

select @templead = id , @roleCode = RoleCode  from Employees where   id = @CreatedBy
select @templead = id  from Employees where  RoleCode =3 and  id = @CreatedBy

if(@roleCode <>  ''4'')
begin
    set @assingee1 =   @CreatedBy
end

 INSERT INTO [dbo].[Candidate]
           ([Code]
           ,[Name]
           ,[Email]
           ,[AvatarLink]
           ,[CVLink]
           ,[ShortDes]
           ,[Phone]
           ,[Noted]
           ,[status]
           ,[Deleted]
           ,[IsActive]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt]
           ,[dob]
           , source,
            Assignee,
            DepartmentId,
            Position ,
            ManagerId,
            Referrer,
            NationalId,
            [Address],
            UserName,
            Pass
           )
     VALUES
           (
            @tempUser,@Name,@Email, @AvatarLink,@CVLink, @ShortDes, @Phone, @Noted, @Status,0,@IsActive,@CreatedBy,@CreatedBy,
            GETDATE(),getdate(),@Dob , @Source , @assingee1,
            @DepartmentId, @Position, @ManagerId ,@Referrer, @NationalId, @Address, @finalUserName, @Pass
           )

end');
PRINT '  + Procedure sp_candidate_insert created';

-- ============================================
-- 4. Update sp_candidate_update
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_candidate_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  + Dropping existing procedure sp_candidate_update...';
    DROP PROCEDURE [dbo].[sp_candidate_update];
END

PRINT '  + Creating procedure sp_candidate_update...';
EXEC('CREATE procedure [dbo].[sp_candidate_update]
(
    @id  int null,
    @Name nvarchar(50) null,
    @ShortDes varchar(500) null,
    @email varchar(50) null,
    @Phone varchar (10) null,
    @Dob datetime =null,
    @Status int  = 1,
    @AvatarLink varchar(300) = '''',
    @CVLink varchar(300) ='''',
    @UpdatedBy varchar(5) null,
    @Noted nvarchar(500) = '''',
    @IsActive bit = null,
    @Source int  = 0,
    @ManagerId int = -1,
    @IsEmployee bit = null,
    @DepartmentId int = null,
    @Position int =null,
    @Referrer nvarchar(200) = '''',
    @StatusHuman  int =null,
    @NationalId varchar(20) = null,
    @Address nvarchar(500) = null,
    @UserName varchar(50) = null,
    @Pass varchar(100) = null,
    @EmployeeId int = null
)
as
begin
    update Candidate
    set
         [Name] = @Name,
         AvatarLink = @AvatarLink,
         CVLink = @CVLink,
         Email =@email,
         Dob = @Dob,
         source= @Source,
         Phone = @Phone,
         ManagerId =@ManagerId,
         Referrer= @Referrer,
         ShortDes = @ShortDes,
         Noted = @Noted,
         IsActive = @IsActive,
         [Status] = @Status,
         DepartmentId = @DepartmentId,
         Position = @Position,
         StatusHuman = @StatusHuman,
         UpdatedBy = @UpdatedBy,
         IsEmployee = ISNULL(@IsEmployee, IsEmployee),
         NationalId = @NationalId,
         [Address] = @Address,
         UserName = case when @UserName is null or LTRIM(RTRIM(@UserName)) = '''' then UserName else @UserName end,
         Pass = case when @Pass is null or LTRIM(RTRIM(@Pass)) = '''' then Pass else @Pass end,
         EmployeeId = ISNULL(@EmployeeId, EmployeeId),
         UpdateAt = getdate()
    where id = @id
end');
PRINT '  + Procedure sp_candidate_update created';

-- ============================================
-- 5. Add sp_candidate_login
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_candidate_login]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  + Dropping existing procedure sp_candidate_login...';
    DROP PROCEDURE [dbo].[sp_candidate_login];
END

PRINT '  + Creating procedure sp_candidate_login...';
EXEC('CREATE procedure [dbo].[sp_candidate_login]
(
    @userName varchar(50),
    @password varchar(100)
)
as
begin
    SET NOCOUNT ON;
    select top (1) * from Candidate
    where UserName = @userName
      and Pass = @password
      and ISNULL(Deleted,0) = 0
      and ISNULL(IsActive,1) = 1;
end');
PRINT '  + Procedure sp_candidate_login created';

-- ============================================
-- 6. Add sp_candidate_changePassword
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_candidate_changePassword]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  + Dropping existing procedure sp_candidate_changePassword...';
    DROP PROCEDURE [dbo].[sp_candidate_changePassword];
END

PRINT '  + Creating procedure sp_candidate_changePassword...';
EXEC('CREATE procedure [dbo].[sp_candidate_changePassword]
(
    @password varchar(100),
    @id int
)
as
begin
    update Candidate
    set Pass = @password,
        UpdateAt = getdate()
    where Id = @id;
end');
PRINT '  + Procedure sp_candidate_changePassword created';

PRINT 'Migration V040 completed successfully';

COMMIT TRANSACTION;
