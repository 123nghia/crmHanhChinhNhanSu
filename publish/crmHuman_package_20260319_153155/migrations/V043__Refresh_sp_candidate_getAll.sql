-- =============================================
-- Migration: V043__Refresh_sp_candidate_getAll
-- Author: System
-- Date: 2026-01-16
-- Description: Refresh sp_candidate_getAll to ensure d.* expands to include new UserName column.
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V043: Refresh sp_candidate_getAll...';

-- Drop if exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_candidate_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_candidate_getAll];
END

PRINT '  + Creating procedure sp_candidate_getAll...';
-- Recreate (Content from V002 standard definition)
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


end')

PRINT 'Migration V043 completed successfully';
COMMIT TRANSACTION;
