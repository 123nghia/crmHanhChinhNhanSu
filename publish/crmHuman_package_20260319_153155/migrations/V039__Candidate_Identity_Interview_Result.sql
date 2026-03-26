-- =============================================
-- Migration: V039__Candidate_Identity_Interview_Result
-- Author: System
-- Date: 2026-01-15
-- Description: Add candidate CCCD/address and interview result fields
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V039: Candidate identity + interview result...';

-- ============================================
-- 1. Extend Candidate table
-- ============================================
IF COL_LENGTH('dbo.Candidate', 'NationalId') IS NULL
BEGIN
    PRINT '  + Adding NationalId to Candidate...';
    ALTER TABLE [dbo].[Candidate]
    ADD [NationalId] [varchar](20) NULL;
END
ELSE
BEGIN
    PRINT '  + NationalId already exists, skipping.';
END

IF COL_LENGTH('dbo.Candidate', 'Address') IS NULL
BEGIN
    PRINT '  + Adding Address to Candidate...';
    ALTER TABLE [dbo].[Candidate]
    ADD [Address] [nvarchar](500) NULL;
END
ELSE
BEGIN
    PRINT '  + Address already exists, skipping.';
END

-- ============================================
-- 2. Extend ScheduleInterview table
-- ============================================
IF COL_LENGTH('dbo.ScheduleInterview', 'InterviewResult') IS NULL
BEGIN
    PRINT '  + Adding InterviewResult to ScheduleInterview...';
    ALTER TABLE [dbo].[ScheduleInterview]
    ADD [InterviewResult] [int] NULL;
END
ELSE
BEGIN
    PRINT '  + InterviewResult already exists, skipping.';
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
    @Address nvarchar(500) = null
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
            [Address]
           )
     VALUES
           (
            @tempUser,@Name,@Email, @AvatarLink,@CVLink, @ShortDes, @Phone, @Noted, @Status,0,@IsActive,@CreatedBy,@CreatedBy,
            GETDATE(),getdate(),@Dob , @Source , @assingee1,
            @DepartmentId, @Position, @ManagerId ,@Referrer, @NationalId, @Address
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
    @IsEmployee bit =0,
    @DepartmentId int = null,
    @Position int =null,
    @Referrer nvarchar(200) = '''',
    @StatusHuman  int =null,
    @NationalId varchar(20) = null,
    @Address nvarchar(500) = null
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
         IsEmployee = @IsEmployee,
         NationalId = @NationalId,
         [Address] = @Address,
         UpdateAt = getdate()
    where id = @id
end');
PRINT '  + Procedure sp_candidate_update created';

-- ============================================
-- 5. Update sp_ScheduleInterview_insert
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ScheduleInterview_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  + Dropping existing procedure sp_ScheduleInterview_insert...';
    DROP PROCEDURE [dbo].[sp_ScheduleInterview_insert];
END

PRINT '  + Creating procedure sp_ScheduleInterview_insert...';
EXEC('create procedure [dbo].[sp_ScheduleInterview_insert]
(
    @RelId int = null,
    @RelCode varchar(20) = null,
    @Type int = null,
    @ScheduleDate datetime = null,
    @AddressInfo nvarchar(500) = null,
    @Noted nvarchar(500) = null,
    @Status int = 0,
    @InterviewerId int = null,
    @InterviewMode int = null,
    @InterviewResult int = null,
    @CreatedBy int = null
)
as
begin
    INSERT INTO [dbo].[ScheduleInterview]
        ([RelId]
        ,[RelCode]
        ,[Type]
        ,[ScheduleDate]
        ,[AddressInfo]
        ,[Noted]
        ,[status]
        ,[InterviewerId]
        ,[InterviewMode]
        ,[InterviewResult]
        ,[Deleted]
        ,[CreatedBy]
        ,[UpdatedBy]
        ,[CreateAt]
        ,[UpdateAt])
    VALUES
        (@RelId,
         @RelCode,
         @Type,
         @ScheduleDate,
         @AddressInfo,
         @Noted,
         ISNULL(@Status, 0),
         @InterviewerId,
         @InterviewMode,
         @InterviewResult,
         0,
         @CreatedBy,
         @CreatedBy,
         getdate(),
         getdate());
end');
PRINT '  + Procedure sp_ScheduleInterview_insert created';

-- ============================================
-- 6. Update sp_ScheduleInterview_update
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ScheduleInterview_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  + Dropping existing procedure sp_ScheduleInterview_update...';
    DROP PROCEDURE [dbo].[sp_ScheduleInterview_update];
END

PRINT '  + Creating procedure sp_ScheduleInterview_update...';
EXEC('create procedure [dbo].[sp_ScheduleInterview_update]
(
    @Id int,
    @RelId int = null,
    @RelCode varchar(20) = null,
    @Type int = null,
    @ScheduleDate datetime = null,
    @AddressInfo nvarchar(500) = null,
    @Noted nvarchar(500) = null,
    @Status int = null,
    @InterviewerId int = null,
    @InterviewMode int = null,
    @InterviewResult int = null,
    @UpdatedBy int = null
)
as
begin
    update [dbo].[ScheduleInterview]
    set RelId = ISNULL(@RelId, RelId),
        RelCode = ISNULL(@RelCode, RelCode),
        Type = ISNULL(@Type, Type),
        ScheduleDate = ISNULL(@ScheduleDate, ScheduleDate),
        AddressInfo = ISNULL(@AddressInfo, AddressInfo),
        Noted = ISNULL(@Noted, Noted),
        status = ISNULL(@Status, status),
        InterviewerId = ISNULL(@InterviewerId, InterviewerId),
        InterviewMode = ISNULL(@InterviewMode, InterviewMode),
        InterviewResult = ISNULL(@InterviewResult, InterviewResult),
        UpdatedBy = ISNULL(@UpdatedBy, UpdatedBy),
        UpdateAt = getdate()
    where Id = @Id;
end');
PRINT '  + Procedure sp_ScheduleInterview_update created';

PRINT 'Migration V039 completed successfully';

COMMIT TRANSACTION;
