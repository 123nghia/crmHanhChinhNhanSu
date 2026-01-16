-- =============================================
-- Migration: V047__Add_Candidate_ExpectedOnboardDate
-- Author: Antigravity
-- Date: 2026-01-16
-- Description: Add ExpectedOnboardDate to Candidate table and update stored procedures.
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V047: Add ExpectedOnboardDate to Candidate...';

-- 1. Add column to Candidate table
IF COL_LENGTH('dbo.Candidate', 'ExpectedOnboardDate') IS NULL
BEGIN
    PRINT '  + Adding ExpectedOnboardDate to Candidate...';
    ALTER TABLE [dbo].[Candidate]
    ADD [ExpectedOnboardDate] [datetime] NULL;
END
ELSE
BEGIN
    PRINT '  + ExpectedOnboardDate already exists, skipping.';
END

-- 2. Update sp_candidate_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_candidate_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  + Re-creating procedure sp_candidate_insert...';
    DROP PROCEDURE [dbo].[sp_candidate_insert];
END

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
    @Pass varchar(100) = null,
    @ExpectedOnboardDate datetime = null
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

declare @roleCode varchar(3);
declare @assingee1  int;
set @assingee1 =-1;

select @roleCode = RoleCode  from Employees where   id = @CreatedBy

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
            Pass,
            ExpectedOnboardDate
           )
      VALUES
           (
            @tempUser,@Name,@Email, @AvatarLink,@CVLink, @ShortDes, @Phone, @Noted, @Status,0,@IsActive,@CreatedBy,@CreatedBy,
            GETDATE(),getdate(),@Dob , @Source , @assingee1,
            @DepartmentId, @Position, @ManagerId ,@Referrer, @NationalId, @Address, @finalUserName, @Pass, @ExpectedOnboardDate
           )

end');

-- 3. Update sp_candidate_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_candidate_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  + Re-creating procedure sp_candidate_update...';
    DROP PROCEDURE [dbo].[sp_candidate_update];
END

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
    @EmployeeId int = null,
    @ExpectedOnboardDate datetime = null
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
         ExpectedOnboardDate = ISNULL(@ExpectedOnboardDate, ExpectedOnboardDate),
         UpdateAt = getdate()
    where id = @id
end');

-- 4. Update sp_candidate_getAll to return ExpectedOnboardDate
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_candidate_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  + Re-creating procedure sp_candidate_getAll...';
    DROP PROCEDURE [dbo].[sp_candidate_getAll];
END

EXEC('CREATE procedure [dbo].[sp_candidate_getAll]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 1000,
@CandidateStatus int = -1,
@DocumentStatus int = -1 ,
@ManagerId int = -1,

@From datetime = null,
@To datetime = null ,
@LoadAll int = 0,
@IsEmployee  bit = 0,
@loadCandidate int = 0
)
as
begin


if( @LoadAll = 1)
begin 

set @Limit =10000;
end 


declare @roleCode  varchar(3);
set @roleCode = '''';

select top 1 @roleCode = RoleCode from Employees where  id = @userId 

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord,
dbo.getUserName( d.CreatedBy) as AuthorName ,
dbo.getFullNameSorce( d.CreatedBy) as SourceName ,
dbo.getDisplayMasterdata(d.Position) as PostionName,
dbo.getDisplayMasterdata(d.Status) as StatusName,
dbo.getFullName( d.ManagerId)  as ManagerName,
dbo.getDisplayMasterdata(d.DepartmentId) as DepartmentName,
d.* from  Candidate d  '';


if(@Token is not null )
begin

 set @where += '' and (d.Code like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Name like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Email like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Phone like  N''''%'' + @Token +''%'''')'';
end;


if( @loadCandidate >0) 
begin 
set @where += '' and  isnull(d.isEmployee,0)  = 0 ''; 
end 

if( @IsEmployee = 1)
begin 
 set @where += '' and d.IsEmployee =1 '';
end 

if( @CandidateStatus > 0 )
begin 
	set @where += '' and  d.status  = @CandidateStatus ''; 
end 

if( @DocumentStatus > 0 )
begin 
	set @where += '' and  d.StatusHuman  = @DocumentStatus ''; 
end 


if( @ManagerId > 0 )
begin 
	set @where += '' and  d.ManagerId  = @ManagerId ''; 
end 

if(@From is not null )
begin 
set @where += '' 
and (  d.CreateAt  >= @fromDate )
 ''; 
end 


if(@to is not null )
begin 
set @where += '' 
and ( d.CreateAt  <= @toDate ) ''; 
end 

set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int,
@MemberId int, @GroupId int, 
@fromDate datetime,@toDate datetime , @Status int , @userId int ,
@CandidateStatus int ,
@ManagerId int ,
@DocumentStatus int 
'';



EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId,
@GroupId = @GroupId, @MemberId = @MemberId , @CandidateStatus = @CandidateStatus,
@ManagerId = @ManagerId,
@DocumentStatus = @DocumentStatus


end');

COMMIT TRANSACTION;
PRINT 'Migration V047 completed successfully.';
