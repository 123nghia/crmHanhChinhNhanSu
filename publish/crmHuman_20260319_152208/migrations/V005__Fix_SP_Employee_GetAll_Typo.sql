-- =============================================
-- Migration: V005__Fix_SP_Employee_GetAll_Typo
-- Author: System
-- Date: 2024-11-21
-- Description: Fix typo trong stored procedure sp_Employee_getAll
--              getDisplayMasterdata → getDisplayMasterData
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V005: Fix SP Employee GetAll Typo...';

-- Drop existing procedure
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('sp_Employee_getAll') AND type = 'P')
BEGIN
    PRINT '  → Dropping existing sp_Employee_getAll...';
    DROP PROCEDURE sp_Employee_getAll;
END

PRINT '  → Creating sp_Employee_getAll with fixed function names...';

EXEC('
CREATE procedure [dbo].[sp_Employee_getAll]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 10,
@From datetime = null,
@To datetime = null,
@IsDeleted  bit =  0
)
as
begin

declare @where  nvarchar(max) = '' where  1= 1 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);

declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = '' select count(d.id) over() as TotalRecord
,d.*,
 dbo.getDisplayMasterData(d.status) as StatusText,
  dbo.getDisplayMasterData(d.DocumentStatus) as DocumentStatusText,
  dbo.getDisplayMasterData(d.DepartmentCode) as DepartmentText,
    dbo.getDisplayMasterData(d.PositionCode) as PositionText,
dbo.getFullName( d.ManagerId)  as GroupName
	
  
 
from   Employees d  '';

if(@Token is not null )
begin

 set @where += '' and (d.UserName like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.FullName like  N''''%'' + @Token +''%'''''';

 set @where += '' or d.Phone like  N''''%'' + @Token +''%'''')'';
end;

if(  @IsDeleted  < 1 )
begin 

set @where += '' and isnull(d.Deleted,0) = 0  ''; 

end 

if(  @Status >-1 )
begin 

set @where += '' and d.IsActive =  @Status ''; 

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

if(  @userId > 0 )
begin 
	set @where += '' and d.id in ( select id  from  getAllUserByUserId(@userId)) 
	''; 
end 

set @where +='' order by d.UpdateAt desc''; 
set @mainClause = @mainClause +  @where

set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int '';

EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId

end
');

PRINT '  ✓ sp_Employee_getAll fixed successfully';

PRINT '✓ Migration V005 completed successfully';

COMMIT TRANSACTION;

