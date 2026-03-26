-- =============================================
-- Migration: V002__Full_Database_Schema_Idempotent
-- Author: System (Auto-generated from sqlscript.sql)
-- Date: 2025-11-21 13:39:51
-- Description: Táº¡o database schema vá»›i IF NOT EXISTS checks
--              - Náº¿u object Ä‘Ã£ tá»“n táº¡i â†’ Bá» qua
--              - Náº¿u object chÆ°a cÃ³ â†’ Táº¡o má»›i
-- =============================================

BEGIN TRANSACTION;

PRINT '================================================';
PRINT 'Migration V002: Full Database Schema (Idempotent)';
PRINT '================================================';
PRINT '';
-- Function: getAllLineCodeViewByGroupId
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getAllLineCodeViewByGroupId]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getAllLineCodeViewByGroupId...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
create  FUNCTION [dbo].[getAllLineCodeViewByGroupId]
(

  @groupId  int =null

)
RETURNS   @temp table ( LineCode  varchar(10))
AS

begin
 
   

	
	
	if(@groupId < 1)
	begin 
	return;
	end


	insert   into  @temp 
	select LineCode from Employees where id in (
	select memberid from  GroupMember
	where groupid = @groupId
	and ISNULL(Deleted,0)=0)
	
	 return;

end')
    PRINT '  âœ“ Function getAllLineCodeViewByGroupId created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getAllLineCodeViewByGroupId already exists, skipping';
END
-- Function: getAllLineCodeViewByUserIdOriginal
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getAllLineCodeViewByUserIdOriginal]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getAllLineCodeViewByUserIdOriginal...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
create  FUNCTION [dbo].[getAllLineCodeViewByUserIdOriginal]
(
@userId  int =null

)
RETURNS   @temp table ( LineCode  varchar(10))
AS

begin
   
   declare @roleId int  = null;
   declare @vendorId int = null;
   



	

    select @roleId = e.RoleCode   from Employees e where Id = @userId

	declare @table table  ( id  int);

	if(@roleId =2 )
	begin 
		insert   into  @table 
		select @userId
	
	end


	if(@roleId = 3) 
	begin 
		declare @groupIdTemp int = null;
		
		select @groupIdTemp =  gr.Id  from [Group]  gr where gr.ManagerId =  @userId; 

		if(@groupIdTemp is null )
		begin 
			insert   into  @table 
		    select @userId
			
		end 
		else 
		begin 
				insert   into  @table 
				select memberid  from groupmember   where groupid =@groupIdTemp;
				
		end 
	end

	if(@roleId = 4) 
	begin 
				insert   into  @table 
				   select @userId
	end 
	
	


	if(@roleId = 5) 
	begin 
				insert   into  @table 
				select Id  from Employees   where  ISNULL(Deleted,0) =0
			
	end 

	if(@roleId =1 )
	begin 
		insert   into  @table 
	    select Id  from Employees   where  ISNULL(Deleted,0) =0
	
	end

	 insert into @temp  
	 select  em.LineCode from Employees   em where  em.Id in ( select  Id from @table)
	
  	 return;

end')
    PRINT '  âœ“ Function getAllLineCodeViewByUserIdOriginal created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getAllLineCodeViewByUserIdOriginal already exists, skipping';
END
-- Function: getAllMemberByGroupId
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getAllMemberByGroupId]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getAllMemberByGroupId...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
create  FUNCTION [dbo].[getAllMemberByGroupId]
(
	@groupId  int =null
)
RETURNS   @table table (id int)
AS

begin
insert   into  @table 
select gm.MemberId from [Group] gr  
inner join GroupMember gm
on gr.Id = gm.GroupId
and  gr.Id = @groupId
return;

end')
    PRINT '  âœ“ Function getAllMemberByGroupId created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getAllMemberByGroupId already exists, skipping';
END
-- Function: getAllUserByUserId
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getAllUserByUserId]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getAllUserByUserId...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE  FUNCTION [dbo].[getAllUserByUserId]
(
	@userId  int =null
)
RETURNS   @table table (id int)
AS

begin
   declare @roleId int  = null;

    select @roleId = e.RoleCode
	from Employees e where Id = @userId

	if(@roleId =1 or @roleId =4)
	begin 
		insert   into  @table 
		select Id  from Employees   
		return;
		
	end
   if( @roleId = 2 or @roleId = 7)
	begin 
		insert   into  @table 
		select @userId
		return;
		
	end 
	if( @roleId = 3 or @roleId = 6 )
	begin 
			declare @groupid int;
			set @groupid =0;
			select  @groupid = id from [Group] where ManagerId = @userId
			if(@groupid  < 1)
			begin 
				insert   into  @table 
				select @userId
				return;
			end 
			
			 else 

			 begin 
					insert   into  @table 
					select  e.MemberId from GroupMember e where e.GroupId = @groupid
					union  select  @userId
					return;
			 end 
	end 




	return ;

	
	


end')
    PRINT '  âœ“ Function getAllUserByUserId created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getAllUserByUserId already exists, skipping';
END
-- Function: getDisplayMasterData
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getDisplayMasterData]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getDisplayMasterData...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[getDisplayMasterData]
(
@id int null 

)
RETURNS nvarchar(100)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @name nvarchar(100)
	-- Add the T-SQL statements to compute the return value here
	select top 1  @name = [Name]   from MasterData 
where code = @id
	-- Return the result of the function
	RETURN @name

END')
    PRINT '  âœ“ Function getDisplayMasterData created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getDisplayMasterData already exists, skipping';
END
-- Function: getDisplayMasterDataById
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getDisplayMasterDataById]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getDisplayMasterDataById...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
create FUNCTION [dbo].[getDisplayMasterDataById]
(
@id int null 

)
RETURNS nvarchar(100)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @name nvarchar(100)
	-- Add the T-SQL statements to compute the return value here
	select top 1  @name = [Name]   from MasterData 
where id = @id
	-- Return the result of the function
	RETURN @name

END')
    PRINT '  âœ“ Function getDisplayMasterDataById created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getDisplayMasterDataById already exists, skipping';
END
-- Function: getExtraInfoDataById
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getExtraInfoDataById]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getExtraInfoDataById...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
create FUNCTION [dbo].[getExtraInfoDataById]
(
@id int null 

)
RETURNS nvarchar(100)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @name nvarchar(100)
	-- Add the T-SQL statements to compute the return value here
	select top 1  @name = Extra   from MasterData 
where id = @id
	-- Return the result of the function
	RETURN @name

END')
    PRINT '  âœ“ Function getExtraInfoDataById created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getExtraInfoDataById already exists, skipping';
END
-- Function: getFullInfo
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getFullInfo]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getFullInfo...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[getFullInfo]
(
@userId  int null 

)
RETURNS nvarchar(100)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @name nvarchar(100)
	-- Add the T-SQL statements to compute the return value here
	select top 1  @name = [Name] from Candidate 
where id = @userId
	-- Return the result of the function
	RETURN @name

END')
    PRINT '  âœ“ Function getFullInfo created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getFullInfo already exists, skipping';
END
-- Function: getFullName
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getFullName]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getFullName...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
create FUNCTION [dbo].[getFullName]
(
@userId  int null 

)
RETURNS nvarchar(100)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @name nvarchar(100)
	-- Add the T-SQL statements to compute the return value here
	select top 1  @name = FullName  from Employees 
where id = @userId
	-- Return the result of the function
	RETURN @name

END')
    PRINT '  âœ“ Function getFullName created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getFullName already exists, skipping';
END
-- Function: getFullNameJob
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getFullNameJob]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getFullNameJob...';
    EXEC('create FUNCTION [dbo].[getFullNameJob]
(
@id  int null

)
RETURNS nvarchar(100)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @name nvarchar(100)
	-- Add the T-SQL statements to compute the return value here
	select top 1  @name = [Name]  from JobItem 
	where id  = @id 
	-- Return the result of the function
	RETURN @name

END')
    PRINT '  âœ“ Function getFullNameJob created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getFullNameJob already exists, skipping';
END
-- Function: getFullNameSorce
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getFullNameSorce]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getFullNameSorce...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[getFullNameSorce]
(
@userId  int null 

)
RETURNS nvarchar(100)
AS
BEGIN
   
   if(@userId = 58)
   begin
		return ''Marketing''
	end
	else if ( @userId = 61)
	begin 

		return ''HCNS''
	end 

	DECLARE @text nvarchar(100)
	-- Add the T-SQL statements to compute the return value here
	select top 1  @text = FullName  from Employees 
		where id = @userId
	-- Return the result of the function
	RETURN @text

END')
    PRINT '  âœ“ Function getFullNameSorce created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getFullNameSorce already exists, skipping';
END
-- Function: getGroupNameOfUser
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getGroupNameOfUser]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getGroupNameOfUser...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
create FUNCTION [dbo].[getGroupNameOfUser]
(
@userId  int null 

)
RETURNS nvarchar(100)
AS
BEGIN
   declare  @groupId int ;
   set @groupId =0;
	select top 1 @groupId = GroupId from GroupMember  where MemberId = @userId
	declare @groupName nvarchar(50);
	set @groupName = ''''
	if(@groupId > 0) 
	begin 
		select @groupName = [Name] from [Group] where id = @groupId 

	end 

	if(@groupName = '''')
		set @groupName = '''';

	return @groupName

	
END')
    PRINT '  âœ“ Function getGroupNameOfUser created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getGroupNameOfUser already exists, skipping';
END
-- Function: getJobTitle
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getJobTitle]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getJobTitle...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
create FUNCTION [dbo].[getJobTitle]
(
@jobId int null 

)
RETURNS nvarchar(100)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @name nvarchar(100)
	-- Add the T-SQL statements to compute the return value here
	select top 1  @name = [Name]  from JobItem 
where id = @jobId
	-- Return the result of the function
	RETURN @name

END')
    PRINT '  âœ“ Function getJobTitle created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getJobTitle already exists, skipping';
END
-- Function: getLinhvucId
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getLinhvucId]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getLinhvucId...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
create FUNCTION [dbo].[getLinhvucId]
(
@jobId int null 

)
RETURNS nvarchar(100)
AS
BEGIN


	
	DECLARE @linvucId nvarchar(100)
	
	select top 1 @linvucId =  Field from JobItem 
where id = @jobId
	
	RETURN @linvucId

END')
    PRINT '  âœ“ Function getLinhvucId created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getLinhvucId already exists, skipping';
END
-- Function: getMasterdataApplyFor
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getMasterdataApplyFor]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getMasterdataApplyFor...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
Create FUNCTION [dbo].[getMasterdataApplyFor]
(
@id int null 

)
RETURNS nvarchar(100)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @name nvarchar(100)
	-- Add the T-SQL statements to compute the return value here
	select top 1  @name = ApplyFor   from MasterData 
where id = @id
	-- Return the result of the function
	RETURN @name

END')
    PRINT '  âœ“ Function getMasterdataApplyFor created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getMasterdataApplyFor already exists, skipping';
END
-- Function: getNameFromParrent
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getNameFromParrent]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getNameFromParrent...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
create FUNCTION [dbo].[getNameFromParrent]
(
@id int null 
)
RETURNS nvarchar(100)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @name nvarchar(100)
	-- Add the T-SQL statements to compute the return value here
	select top 1  @name = [Text]   from ParrentChild 
where id = @id
	-- Return the result of the function
	RETURN @name

END')
    PRINT '  âœ“ Function getNameFromParrent created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getNameFromParrent already exists, skipping';
END
-- Function: getNamePartner
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getNamePartner]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getNamePartner...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[getNamePartner]
(
@partnerId  int null 

)
RETURNS nvarchar(100)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @name nvarchar(100)
	-- Add the T-SQL statements to compute the return value here
	select top 1  @name =  [ShortName]   from [Partner ] 
where id = @partnerId
	-- Return the result of the function
	RETURN @name

END')
    PRINT '  âœ“ Function getNamePartner created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getNamePartner already exists, skipping';
END
-- Function: getOrderIdByCode
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getOrderIdByCode]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getOrderIdByCode...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
create FUNCTION [dbo].[getOrderIdByCode]
(
@orderCode varchar(20)  

)
RETURNS nvarchar(100)
AS
BEGIN
  declare @id int;
  select top 1 @id = id from [Order] where Code = @orderCode

  return @id 
END')
    PRINT '  âœ“ Function getOrderIdByCode created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getOrderIdByCode already exists, skipping';
END
-- Function: getUserName
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getUserName]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getUserName...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
create FUNCTION [dbo].[getUserName]
(
@userId  int null 

)
RETURNS nvarchar(100)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @text nvarchar(100)
	-- Add the T-SQL statements to compute the return value here
	select top 1  @text = UserName  from Employees 
where id = @userId
	-- Return the result of the function
	RETURN @text

END')
    PRINT '  âœ“ Function getUserName created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getUserName already exists, skipping';
END
-- Function: getUserNamev2
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getUserNamev2]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function getUserNamev2...';
    EXEC('-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
create FUNCTION [dbo].[getUserNamev2]
(
@userId  varchar(10) null

)
RETURNS nvarchar(100)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @name nvarchar(100)

	-- Add the T-SQL statements to compute the return value here
	select @name = UserName  from Employees where id  = @userId

	-- Return the result of the function
	RETURN @name

END')
    PRINT '  âœ“ Function getUserNamev2 created';
END
ELSE
BEGIN
    PRINT '  â†’ Function getUserNamev2 already exists, skipping';
END
-- Function: markettingGetAllViewId
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[markettingGetAllViewId]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    PRINT '  â†’ Creating function markettingGetAllViewId...';
    EXEC('CREATE  FUNCTION [dbo].[markettingGetAllViewId]
(
	@userId int =0
)
RETURNS   @table table (id int)
AS

begin

declare @roleId int = 0;
select @roleId = RoleCode from Employees where id = @userId 

if(@roleId = 6 or @roleId = 7)
begin 
			
		declare @manaagerId int =0;
		set @manaagerId =0;
		select top 1 @manaagerId = ManagerId from [Group] where ManagerId = @userId 
		if( @manaagerId is null or @manaagerId < 1)
		begin 
			set @manaagerId = 0;
		end 
		insert   into  @table 
		select Id  from Employees   where  id = @userId or CreatedBy = @manaagerId
		return;
end





if( @userId = 61)
begin 
		insert   into  @table 
		select Id  from Employees   where RoleCode = ''4''  and id = 61
		return;
end 

else 
begin 


	insert   into  @table 
	select Id  from Employees   where RoleCode = ''4'' 
	return;
end 
	
	return;


end')
    PRINT '  âœ“ Function markettingGetAllViewId created';
END
ELSE
BEGIN
    PRINT '  â†’ Function markettingGetAllViewId already exists, skipping';
END
-- Table: BHXHItem
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BHXHItem]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table BHXHItem...';
    EXEC('CREATE TABLE [dbo].[BHXHItem](
	[UserName] [varchar](8) NULL,
	[NumberCode] [varchar](20) NULL,
	[IsConfirmletter] [int] NULL,
	[Relid] [int] NULL,
	[ChungTuThue] [varchar](4) NULL,
	[Deleted] [bit] NULL,
	[IsActive] [bit] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DependentName] [nvarchar](100) NULL,
	[Dependent] [nvarchar](50) NULL,
	[EffectedFrom] [datetime] NULL,
	[PITDate] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table BHXHItem created';
END
ELSE
BEGIN
    PRINT '  â†’ Table BHXHItem already exists, skipping';
END
-- Table: Candidate
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Candidate]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table Candidate...';
    EXEC('CREATE TABLE [dbo].[Candidate](
	[Code] [varchar](8) NULL,
	[Name] [nvarchar](50) NULL,
	[AvatarLink] [varchar](500) NULL,
	[CVLink] [varchar](500) NULL,
	[ShortDes] [varchar](500) NULL,
	[Phone] [varchar](10) NULL,
	[Noted] [nvarchar](500) NULL,
	[status] [int] NULL,
	[Deleted] [bit] NULL,
	[IsActive] [bit] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[dob] [datetime] NULL,
	[Email] [varchar](50) NULL,
	[source] [int] NULL,
	[Assignee] [int] NULL,
	[ispush] [int] NULL,
	[StatusHuman] [int] NULL,
	[ManagerId] [int] NULL,
	[DepartmentId] [int] NULL,
	[Position] [int] NULL,
	[isEmployee] [bit] NULL,
	[Referrer] [nvarchar](200) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table Candidate created';
END
ELSE
BEGIN
    PRINT '  â†’ Table Candidate already exists, skipping';
END
-- Table: DocumentData
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentData]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table DocumentData...';
    EXEC('CREATE TABLE [dbo].[DocumentData](
	[RelId] [int] NULL,
	[RelCode] [varchar](10) NULL,
	[Code] [varchar](5) NULL,
	[DisplayText] [nvarchar](100) NULL,
	[ValueFile] [nvarchar](500) NULL,
	[status] [int] NULL,
	[Deleted] [bit] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[dataType] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table DocumentData created';
END
ELSE
BEGIN
    PRINT '  â†’ Table DocumentData already exists, skipping';
END
-- Table: Employees
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table Employees...';
    EXEC('CREATE TABLE [dbo].[Employees](
	[UserName] [varchar](8) NULL,
	[FullName] [nvarchar](50) NULL,
	[Phone] [varchar](10) NULL,
	[Noted] [nvarchar](500) NULL,
	[RoleCode] [varchar](4) NULL,
	[Deleted] [bit] NULL,
	[IsActive] [bit] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Pass] [varchar](300) NULL,
	[dob] [datetime] NULL,
	[LineCode] [varchar](4) NULL,
	[Onboard] [datetime] NULL,
	[ColorCode] [varchar](20) NULL,
	[TypeAccount] [varchar](2) NULL,
	[DepartmentCode] [varchar](4) NULL,
	[DocumentStatus] [varchar](6) NULL,
	[PositionCode] [varchar](4) NULL,
	[RelationCode] [varchar](2) NULL,
	[NationalId] [varchar](11) NULL,
	[NationalDate] [datetime] NULL,
	[NationalPlace] [nvarchar](500) NULL,
	[PermanentAddress] [nvarchar](500) NULL,
	[TemporaryAddress] [nvarchar](500) NULL,
	[ManagerId] [int] NULL,
	[Email] [varchar](70) NULL,
	[CVLink] [varchar](500) NULL,
	[status] [int] NULL,
	[sourceFrom] [int] NULL,
	[StatusWork] [int] NULL,
	[BankName] [nvarchar](100) NULL,
	[BankAccount] [varchar](30) NULL,
	[EducationLevel] [varchar](4) NULL,
	[Maritalstatus] [varchar](4) NULL,
	[DocumentCheck] [varchar](200) NULL,
	[Gender] [nvarchar](50) NULL,
	[PlaceOfBirth] [nvarchar](255) NULL,
	[Religion] [nvarchar](100) NULL,
	[PersonalEmail] [nvarchar](255) NULL,
	[BeneficiaryName] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table Employees created';
END
ELSE
BEGIN
    PRINT '  â†’ Table Employees already exists, skipping';
END
-- Table: Group
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Group]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table Group...';
    EXEC('CREATE TABLE [dbo].[Group](
	[Code] [varchar](6) NULL,
	[Name] [nvarchar](50) NULL,
	[ManagerId] [int] NULL,
	[Status] [int] NULL,
	[Deleted] [bit] NULL,
	[IsActive] [int] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table Group created';
END
ELSE
BEGIN
    PRINT '  â†’ Table Group already exists, skipping';
END
-- Table: GroupMember
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GroupMember]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table GroupMember...';
    EXEC('CREATE TABLE [dbo].[GroupMember](
	[GroupId] [int] NULL,
	[MemberId] [int] NULL,
	[Deleted] [bit] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table GroupMember created';
END
ELSE
BEGIN
    PRINT '  â†’ Table GroupMember already exists, skipping';
END
-- Table: hdldItem
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[hdldItem]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table hdldItem...';
    EXEC('CREATE TABLE [dbo].[hdldItem](
	[UserId] [varchar](8) NULL,
	[NoAgree] [varchar](20) NULL,
	[Start] [datetime] NULL,
	[End] [datetime] NULL,
	[CodeId] [varchar](5) NULL,
	[Deleted] [bit] NULL,
	[IsActive] [bit] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table hdldItem created';
END
ELSE
BEGIN
    PRINT '  â†’ Table hdldItem already exists, skipping';
END
-- Table: JobItem
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[JobItem]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table JobItem...';
    EXEC('CREATE TABLE [dbo].[JobItem](
	[Code] [varchar](8) NULL,
	[Name] [nvarchar](100) NULL,
	[Field] [int] NULL,
	[CareerId] [int] NULL,
	[Content] [ntext] NULL,
	[ShortDes] [nvarchar](200) NULL,
	[Noted] [nvarchar](300) NULL,
	[Status] [int] NULL,
	[Deleted] [bit] NULL,
	[IsActive] [bit] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[WarrantyDate] [int] NULL,
	[Inputfile] [varchar](300) NULL,
	[ProjectId] [int] NULL,
	[PartnerId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]')
    PRINT '  âœ“ Table JobItem created';
END
ELSE
BEGIN
    PRINT '  â†’ Table JobItem already exists, skipping';
END
-- Table: LogCall
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LogCall]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table LogCall...';
    EXEC('CREATE TABLE [dbo].[LogCall](
	[NoAgree] [varchar](20) NULL,
	[ProfileId] [int] NULL,
	[phone] [varchar](20) NULL,
	[LineCode] [varchar](10) NULL,
	[UserId] [int] NULL,
	[VendorId] [int] NULL,
	[TimeBuisiness] [datetime] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Deleted] [bit] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[sourceCall] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table LogCall created';
END
ELSE
BEGIN
    PRINT '  â†’ Table LogCall already exists, skipping';
END
-- Table: MasterData
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MasterData]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table MasterData...';
    EXEC('CREATE TABLE [dbo].[MasterData](
	[Code] [varchar](8) NULL,
	[Name] [nvarchar](50) NULL,
	[Noted] [nvarchar](500) NULL,
	[Deleted] [bit] NULL,
	[IsActive] [bit] NULL,
	[CreatedBy] [int] NULL,
	[TypeData] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Extra] [varchar](10) NULL,
	[ApplyFor] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table MasterData created';
END
ELSE
BEGIN
    PRINT '  â†’ Table MasterData already exists, skipping';
END
-- Table: OnboardMember
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OnboardMember]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table OnboardMember...';
    EXEC('CREATE TABLE [dbo].[OnboardMember](
	[JobId] [nvarchar](50) NULL,
	[Status] [varchar](500) NULL,
	[SystemStatus] [varchar](500) NULL,
	[CandidateId] [varchar](500) NULL,
	[PartnerId] [varchar](10) NULL,
	[ProjectId] [nvarchar](500) NULL,
	[OrderCode] [varchar](10) NULL,
	[Deleted] [bit] NULL,
	[Source] [bit] NULL,
	[ShortDes] [nvarchar](400) NULL,
	[OnboardDate] [datetime] NULL,
	[Assignee] [int] NULL,
	[Noted] [nvarchar](400) NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[dob] [datetime] NULL,
	[CVLink] [varchar](500) NULL,
	[Warrantydate] [datetime] NULL,
	[Dpd] [int] NULL,
	[statusFollow] [int] NULL,
	[result] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table OnboardMember created';
END
ELSE
BEGIN
    PRINT '  â†’ Table OnboardMember already exists, skipping';
END
-- Table: Order
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Order]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table Order...';
    EXEC('CREATE TABLE [dbo].[Order](
	[Code] [varchar](8) NULL,
	[Status] [int] NULL,
	[CandidateId] [int] NULL,
	[JobId] [int] NULL,
	[ShortDes] [nvarchar](500) NULL,
	[CVLink] [varchar](500) NULL,
	[Noted] [nvarchar](500) NULL,
	[Deleted] [bit] NULL,
	[IsActive] [bit] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[partnerId] [int] NULL,
	[ProjectId] [int] NULL,
	[source] [int] NULL,
	[DateGet] [datetime] NULL,
	[Enable] [bit] NULL,
	[Assignee] [int] NULL,
	[result] [int] NULL,
	[Isapply] [int] NULL,
	[isClose] [int] NULL,
	[isReturn] [int] NULL,
	[dateApply] [datetime] NULL,
	[dateOnboard] [datetime] NULL,
	[ispush] [int] NULL,
	[Regional] [varchar](5) NULL,
	[SchoolName] [nvarchar](500) NULL,
	[RankLevel] [int] NULL,
	[Gender] [int] NULL,
	[Introduction] [nvarchar](max) NULL,
	[Experience] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]')
    PRINT '  âœ“ Table Order created';
END
ELSE
BEGIN
    PRINT '  â†’ Table Order already exists, skipping';
END
-- Table: OrderImpactHIstory
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrderImpactHIstory]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table OrderImpactHIstory...';
    EXEC('CREATE TABLE [dbo].[OrderImpactHIstory](
	[OrderCode] [varchar](8) NULL,
	[ObjectInfo] [ntext] NULL,
	[OldStatus] [int] NULL,
	[NewStatus] [int] NULL,
	[Noted] [nvarchar](500) NULL,
	[Deleted] [bit] NULL,
	[IsActive] [bit] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TxtTimer] [nvarchar](100) NULL,
	[DateFrom] [datetime] NULL,
	[TxtPlace] [nvarchar](200) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]')
    PRINT '  âœ“ Table OrderImpactHIstory created';
END
ELSE
BEGIN
    PRINT '  â†’ Table OrderImpactHIstory already exists, skipping';
END
-- Table: ParrentChild
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ParrentChild]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table ParrentChild...';
    EXEC('CREATE TABLE [dbo].[ParrentChild](
	[Text] [nvarchar](300) NULL,
	[RelId] [int] NULL,
	[Type] [int] NULL,
	[CreateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table ParrentChild created';
END
ELSE
BEGIN
    PRINT '  â†’ Table ParrentChild already exists, skipping';
END
-- Table: Partner
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Partner]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table Partner...';
    EXEC('CREATE TABLE [dbo].[Partner](
	[Code] [varchar](8) NULL,
	[Name] [nvarchar](50) NULL,
	[Noted] [nvarchar](500) NULL,
	[Deleted] [bit] NULL,
	[IsActive] [bit] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TaxCode] [varchar](20) NULL,
	[ShortName] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table Partner created';
END
ELSE
BEGIN
    PRINT '  â†’ Table Partner already exists, skipping';
END
-- Table: regionals
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[regionals]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table regionals...';
    EXEC('CREATE TABLE [dbo].[regionals](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Code] [varchar](5) NULL,
	[Name] [nvarchar](50) NULL,
	[priorites] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table regionals created';
END
ELSE
BEGIN
    PRINT '  â†’ Table regionals already exists, skipping';
END
-- Table: RelationItem
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RelationItem]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table RelationItem...';
    EXEC('CREATE TABLE [dbo].[RelationItem](
	[UserName] [varchar](8) NULL,
	[Relationcode] [varchar](4) NULL,
	[Name] [nvarchar](50) NULL,
	[Phone] [varchar](10) NULL,
	[Noted] [nvarchar](500) NULL,
	[AddressInfo] [nvarchar](500) NULL,
	[Deleted] [bit] NULL,
	[IsActive] [bit] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table RelationItem created';
END
ELSE
BEGIN
    PRINT '  â†’ Table RelationItem already exists, skipping';
END
-- Table: ReportTalkTime
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReportTalkTime]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table ReportTalkTime...';
    EXEC('CREATE TABLE [dbo].[ReportTalkTime](
	[LineCode] [varchar](20) NULL,
	[NoAgree] [nvarchar](30) NULL,
	[PhoneLog] [varchar](300) NULL,
	[FileRecording] [varchar](200) NULL,
	[VendorId] [int] NULL,
	[Duration] [int] NULL,
	[CompanyId] [int] NULL,
	[CampangnId] [int] NULL,
	[CallDate] [datetime] NULL,
	[EventTime] [datetime] NULL,
	[Linkedid] [varchar](100) NULL,
	[Disposition] [varchar](20) NULL,
	[DurationReal] [decimal](18, 2) NULL,
	[DurationBill] [int] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Deleted] [bit] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[sourceCall] [int] NULL,
	[LineId] [int] NULL,
	[deleteFile] [bit] NULL,
	[isCal] [bit] NULL,
	[userid] [int] NULL,
	[endOfCall] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table ReportTalkTime created';
END
ELSE
BEGIN
    PRINT '  â†’ Table ReportTalkTime already exists, skipping';
END
-- Table: ReportTalkTimeGroupByDay
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReportTalkTimeGroupByDay]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table ReportTalkTimeGroupByDay...';
    EXEC('CREATE TABLE [dbo].[ReportTalkTimeGroupByDay](
	[LineCode] [varchar](20) NULL,
	[SumCall] [int] NULL,
	[SumNoAgree] [int] NULL,
	[BusinessTime] [datetime] NULL,
	[PerPercent] [decimal](18, 2) NULL,
	[SumAn] [int] NULL,
	[SumNoBussy] [int] NULL,
	[SumNOAswer] [int] NULL,
	[SumNOChanel] [int] NULL,
	[SumNOServe] [int] NULL,
	[TimeWaiting] [decimal](18, 2) NULL,
	[Timcall] [decimal](18, 2) NULL,
	[TimeTalking] [decimal](18, 2) NULL,
	[YearR] [int] NULL,
	[MonthR] [int] NULL,
	[DayR] [int] NULL,
	[SumNoFail] [int] NULL,
	[VendorId] [int] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Deleted] [bit] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SumNOCancel] [int] NULL,
	[LineId] [int] NULL,
	[userId] [int] NULL,
	[lastCall] [datetime] NULL,
	[lastCallcrm] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table ReportTalkTimeGroupByDay created';
END
ELSE
BEGIN
    PRINT '  â†’ Table ReportTalkTimeGroupByDay already exists, skipping';
END
-- Table: ScheduleInterview
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ScheduleInterview]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table ScheduleInterview...';
    EXEC('CREATE TABLE [dbo].[ScheduleInterview](
	[RelId] [int] NULL,
	[RelCode] [varchar](10) NULL,
	[Type] [int] NULL,
	[ScheduleDate] [datetime] NULL,
	[AddressInfo] [nvarchar](500) NULL,
	[Noted] [nvarchar](500) NULL,
	[status] [int] NULL,
	[Deleted] [bit] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table ScheduleInterview created';
END
ELSE
BEGIN
    PRINT '  â†’ Table ScheduleInterview already exists, skipping';
END
-- Table: TaxItem
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TaxItem]') AND type = 'U')
BEGIN
    PRINT '  â†’ Creating table TaxItem...';
    EXEC('CREATE TABLE [dbo].[TaxItem](
	[UserName] [varchar](8) NULL,
	[CodeId] [int] NULL,
	[BiaSo] [varchar](4) NULL,
	[Deleted] [bit] NULL,
	[IsActive] [bit] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedBy] [int] NULL,
	[CreateAt] [datetime] NULL,
	[UpdateAt] [datetime] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PageTax] [varchar](4) NULL,
	[RegBHYT] [nvarchar](150) NULL,
	[PITDate] [datetime] NULL,
	[EffectedFrom] [datetime] NULL,
	[Number] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]')
    PRINT '  âœ“ Table TaxItem created';
END
ELSE
BEGIN
    PRINT '  â†’ Table TaxItem already exists, skipping';
END
ALTER TABLE [dbo].[Candidate] ADD  DEFAULT ((0)) FOR [source]

ALTER TABLE [dbo].[Candidate] ADD  DEFAULT ((0)) FOR [ispush]

ALTER TABLE [dbo].[Candidate] ADD  DEFAULT ((0)) FOR [isEmployee]

ALTER TABLE [dbo].[DocumentData] ADD  DEFAULT ((0)) FOR [dataType]

ALTER TABLE [dbo].[Employees] ADD  DEFAULT ('') FOR [TypeAccount]

ALTER TABLE [dbo].[Employees] ADD  DEFAULT ('') FOR [DepartmentCode]

ALTER TABLE [dbo].[Employees] ADD  DEFAULT ('') FOR [DocumentStatus]

ALTER TABLE [dbo].[Employees] ADD  DEFAULT ('') FOR [PositionCode]

ALTER TABLE [dbo].[Employees] ADD  DEFAULT ('') FOR [RelationCode]

ALTER TABLE [dbo].[Employees] ADD  DEFAULT (NULL) FOR [NationalId]

ALTER TABLE [dbo].[Employees] ADD  DEFAULT (NULL) FOR [NationalDate]

ALTER TABLE [dbo].[Employees] ADD  DEFAULT (NULL) FOR [NationalPlace]

ALTER TABLE [dbo].[Employees] ADD  DEFAULT (NULL) FOR [PermanentAddress]

ALTER TABLE [dbo].[Employees] ADD  DEFAULT (NULL) FOR [TemporaryAddress]

ALTER TABLE [dbo].[Employees] ADD  DEFAULT ((0)) FOR [sourceFrom]

ALTER TABLE [dbo].[JobItem] ADD  DEFAULT ((0)) FOR [WarrantyDate]

ALTER TABLE [dbo].[LogCall] ADD  DEFAULT ((0)) FOR [sourceCall]

ALTER TABLE [dbo].[MasterData] ADD  DEFAULT ('red') FOR [Extra]

ALTER TABLE [dbo].[MasterData] ADD  DEFAULT ((2)) FOR [ApplyFor]

ALTER TABLE [dbo].[Order] ADD  DEFAULT ((0)) FOR [source]

ALTER TABLE [dbo].[Order] ADD  DEFAULT ((0)) FOR [Isapply]

ALTER TABLE [dbo].[Order] ADD  DEFAULT ((0)) FOR [isClose]

ALTER TABLE [dbo].[Order] ADD  DEFAULT ((0)) FOR [isReturn]

ALTER TABLE [dbo].[Order] ADD  DEFAULT (NULL) FOR [dateApply]

ALTER TABLE [dbo].[Order] ADD  DEFAULT (NULL) FOR [dateOnboard]

ALTER TABLE [dbo].[Order] ADD  DEFAULT ((0)) FOR [ispush]

ALTER TABLE [dbo].[ReportTalkTime] ADD  DEFAULT (NULL) FOR [sourceCall]

ALTER TABLE [dbo].[ReportTalkTime] ADD  DEFAULT ((0)) FOR [deleteFile]

ALTER TABLE [dbo].[ReportTalkTime] ADD  DEFAULT ((1)) FOR [isCal]

-- Stored Procedure: ChangeStatusApply
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChangeStatusApply]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure ChangeStatusApply...';
    DROP PROCEDURE [dbo].[ChangeStatusApply];
END

PRINT '  â†’ Creating procedure ChangeStatusApply...';
EXEC('CREATE procedure [dbo].[ChangeStatusApply]
(
	@UpdateBy int null,
	@OrderId int null

)
as
begin
	 
	 
	 update [Order] set 
		    Isapply = 1,
			dateApply = getdate(),
			UpdatedBy = @UpdateBy
	  where id = @OrderId


	  declare @ordercode varchar(10);
	  declare @statusOrder int =0;

	  select @ordercode = Code ,
	  @statusOrder = [Status]
	  
	  from [Order] where id =@OrderId
	  
	  
	  INSERT INTO [dbo].[OrderImpactHIstory]
           ([OrderCode]
           ,[ObjectInfo]
           ,[OldStatus]
           ,[NewStatus]
           ,[Noted]
           ,[Deleted]
           ,[IsActive]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt]
           ,[TxtTimer]
           ,[DateFrom]
           ,[TxtPlace])
     VALUES
           (@ordercode ,'''',0, @statusOrder, N''Chuyển sang danh sách ứng tuyển'', 0,
		   1,@UpdateBy,@UpdateBy, GETDATE(), GETDATE(),'''', null, ''''
		   )

end')
PRINT '  âœ“ Procedure ChangeStatusApply created';
-- Stored Procedure: pro_addUser
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[pro_addUser]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure pro_addUser...';
    DROP PROCEDURE [dbo].[pro_addUser];
END

PRINT '  â†’ Creating procedure pro_addUser...';
EXEC('create procedure [dbo].[pro_addUser]
(
	
		@UserName varchar(8) null,
		@Pass varchar(100) null,
		@TypeUser nvarchar(50) null,
		@UserId varchar (10) null,
		@FirstName varchar(5) null,
		@FullName varchar(5) null,
		@Email datetime = null, 
		@Phone datetime = null, 
		@CreatedBy  varchar(4) = 1
	
)
as
begin
			declare @idInsert int;
			set @idInsert =0;
			
			INSERT INTO [dbo].[UserInfo]
			(
			[Deleted]
			,[Status]
			,[CreateAt]
			,[CreatedBy]
			,[UpdateAt]
			,[UpdatedBy]
			,[phone]
			,[firstName]
			,[FullName]
			,[email])
			VALUES
			(
			0, 1, GETDATE(), @CreatedBy, GETDATE(), @CreatedBy,  @Phone, @FirstName, @FullName, 

				@Email
			)
			
		    set @idInsert=  IDENT_CURRENT(''UserInfo'');

			INSERT INTO [dbo].[AuthenUser]
			([userName]
			,[typeUser]
			,[pass]
			,[userId])
			VALUES
			(
			@UserName, @TypeUser, @Pass,@idInsert
			)
	
end')
PRINT '  âœ“ Procedure pro_addUser created';
-- Stored Procedure: sp_AddCandidateAndOrder
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_AddCandidateAndOrder]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_AddCandidateAndOrder...';
    DROP PROCEDURE [dbo].[sp_AddCandidateAndOrder];
END

PRINT '  â†’ Creating procedure sp_AddCandidateAndOrder...';
EXEC('CREATE procedure [dbo].[sp_AddCandidateAndOrder]
(
@JobId int NULL,
@ShortDes nvarchar(300) NULL,
@CVLink varchar(300) NULL, 
@NotedCan nvarchar(500),
@Email varchar(30) NULL ,
@Phone varchar(20) NULL,
@Name nvarchar(50) NULL,
@Dob datetime null,
@ShortDesOrder nvarchar(500) NULL, 
@IsActive INT null, 
@createBy INT null,
@StatusAplly int null,
@CandidateId int null

)
as
begin

declare @maxId  int =0 ;
set  @maxId = IDENT_CURRENT(''Candidate'');

set @maxId = @maxId +1;

declare @tempUser  varchar(6);

if(@maxId < 10)
begin 
	set @tempUser = concat(''CA000'', @maxId)
end 
if(@maxId < 100)
begin 
	set @tempUser = concat(''CA00'', @maxId)
end 

if(@maxId < 1000)
begin 
	set @tempUser = concat(''CA0'', @maxId)
end 

if(@maxId > 999)
begin 
	set @tempUser = concat(''CA'', @maxId)
end 
if(@maxId > 10000)
begin 
	set @tempUser = concat(''CA'', @maxId)
end 



if(@CandidateId < 1)
begin 

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
		    Assignee
		   )
        VALUES
           (
		    @tempUser, @Name,@Email,'''',@CVLink,@ShortDes,@Phone,@NotedCan,
			@StatusAplly,0,1,@createBy,@createBy,getdate(),getdate(),@Dob,0,@createBy
			)


end

if(@CandidateId > 1)
begin 
		update Candidate 
		set 
				[Name] = @Name,
				Email = @Email,
				ShortDes = @ShortDes,
				Phone = @Phone,
				Noted = @NotedCan,
				UpdatedBy = @createBy,
				UpdateAt = getdate(),
				dob =@Dob
		 where 
				id = @CandidateId

end 


   if( @JobId < 1)
		return ;
	
		

   
   declare @idCa  int;
   set @idCa =-1;

   if(@CandidateId < 1 )
	   begin 
		 select @idCa = SCOPE_IDENTITY() 
	   end 
   else 
	   begin 
			set @idCa= @CandidateId;
	   end 

declare @maxId1 int =0 ;

set  @maxId1 = IDENT_CURRENT(''[Order]'');


if(@maxId1 is null)
begin 
	set @maxId1 =0;
end
set @maxId1 = @maxId1 +1;
declare @tempUser1  varchar(6);
if(@maxId1 < 10)
begin 
	set @tempUser1 = concat(''MD000'', @maxId1)
end 
if(@maxId1 < 100)
begin 
	set @tempUser1 = concat(''MD00'', @maxId1)
end 

if(@maxId1 < 1000)
begin 
	set @tempUser1 = concat(''MD0'', @maxId1)
end 

if(@maxId1 > 999)
begin 
	set @tempUser1 = concat(''MD'', @maxId1)
end 
if(@maxId1 > 10000)
begin 
	set @tempUser1 = concat(''MD'', @maxId1)
end 
	

declare @partnerId1 int;
declare @ProjectId1 int;
select @partnerId1 = PartnerId, @ProjectId1 = ProjectId 
from JobItem where id = @JobId 



INSERT INTO [dbo].[Order]
           (
		    partnerId,
		    ProjectId,
		    [Code]
           ,[Status]
           ,[CandidateId]
           ,[JobId]
           ,[ShortDes]
           ,[CVLink]
           ,[Noted]
           ,[Deleted]
           ,[IsActive]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt],
		   source ,
		   DateGet ,
		   [Enable],
		   Assignee
		   )
VALUES
(
 @partnerId1,@ProjectId1,@tempUser1,@StatusAplly,
 @idCa, @JobId,@ShortDes,@CVLink,@ShortDesOrder,
 0,1,@createBy,@createBy,getdate(), getdate(),0,GETDATE(), 0,@createBy
		   
)


insert into OrderImpactHIstory( OrderCode, 
ObjectInfo, OldStatus,NewStatus, Noted, 
Deleted, IsActive, CreatedBy, CreateAt, TxtTimer,
DateFrom, TxtPlace) 
values ( @tempUser1,'''' , 0, @StatusAplly, N''Tạo mới đơn hàng'', 0, 1, @createBy,
GETDATE(), '''', null, '''')


end')
PRINT '  âœ“ Procedure sp_AddCandidateAndOrder created';
-- Stored Procedure: sp_AddCandidateAndOrderMarketting
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_AddCandidateAndOrderMarketting]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_AddCandidateAndOrderMarketting...';
    DROP PROCEDURE [dbo].[sp_AddCandidateAndOrderMarketting];
END

PRINT '  â†’ Creating procedure sp_AddCandidateAndOrderMarketting...';
EXEC('CREATE procedure [dbo].[sp_AddCandidateAndOrderMarketting]
(

@Name nvarchar(50) NULL,
@Email varchar(30) null,
@PhoneNumber varchar(20) NULL,
@Status int Null, 
@Dob datetime null,
 
@Source int null, 
@NotedCan nvarchar(500),
@CVLink varchar(300) NULL, 
@JobId int NULL,
@Document nvarchar(300) NULL,
@NotedOrder nvarchar(500) NULL, 
@userId INT null,
@Regional int = -1
)
as
begin

declare @role varchar = ''4'';
 
select @role =  RoleCode from Employees where id = @userId
declare @ispush int  =0;


if(@role = ''6''  or @role = ''7'')
begin 
	set @ispush =1;
end

declare @maxId  int =0 ;
set  @maxId = IDENT_CURRENT(''Candidate'');

set @maxId = @maxId +1;

declare @tempUser  varchar(6);

if(@maxId < 10)
begin 
	set @tempUser = concat(''CA000'', @maxId)
end 
if(@maxId < 100)
begin 
	set @tempUser = concat(''CA00'', @maxId)
end 

if(@maxId < 1000)
begin 
	set @tempUser = concat(''CA0'', @maxId)
end 

if(@maxId > 999)
begin 
	set @tempUser = concat(''CA'', @maxId)
end 
if(@maxId > 10000)
begin 
	set @tempUser = concat(''CA'', @maxId)
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
			ispush
		
		   )
     VALUES
           (
		    @tempUser, @Name,@Email,'''',@CVLink,@Document,@PhoneNumber,@NotedCan,
			@Status,0,1,@userId,@userId,getdate(),getdate(),@Dob,@Source,null , @ispush)

   if( @JobId < 1)
		return ;

   declare @idCa  int;
   set @idCa =-1;
   select @idCa = SCOPE_IDENTITY() 



   declare @maxId1 int =0 ;

set  @maxId1 = IDENT_CURRENT(''[Order]'');


if(@maxId1 is null)
begin 
	set @maxId1 =0;
end
set @maxId1 = @maxId1 +1;
declare @tempUser1  varchar(6);
if(@maxId1 < 10)
begin 
	set @tempUser1 = concat(''MD000'', @maxId)
end 
if(@maxId1 < 100)
begin 
	set @tempUser1 = concat(''MD00'', @maxId1)
end 

if(@maxId1 < 1000)
begin 
	set @tempUser1 = concat(''MD0'', @maxId1)
end 

if(@maxId1 > 999)
begin 
	set @tempUser1 = concat(''MD'', @maxId1)
end 
if(@maxId1 > 10000)
begin 
	set @tempUser1 = concat(''MD'', @maxId1)
end 
	

declare @partnerId1 int;
declare @ProjectId1 int;
select @partnerId1 = PartnerId, @ProjectId1 = ProjectId 
from JobItem where id = @JobId 
INSERT INTO [dbo].[Order]
           (
		    partnerId,
		    ProjectId,
		    [Code]
           ,[Status]
           ,[CandidateId]
           ,[JobId]
           ,[ShortDes]
           ,[CVLink]
           ,[Noted]
           ,[Deleted]
           ,[IsActive]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt],
		   source ,
		   DateGet ,
		   [Enable],
		   Assignee,
		   ispush,
		   Regional
		   )
VALUES
(
 @partnerId1,@ProjectId1,@tempUser1,46,
 @idCa, @JobId,@Document,@CVLink,@NotedOrder,
 0,1,@userId,@userId,getdate(), getdate(),@Source,null, 0, null , @ispush,@Regional
		   
)

insert into OrderImpactHIstory( OrderCode, 
ObjectInfo, OldStatus,NewStatus, Noted, 
Deleted, IsActive, CreatedBy, CreateAt, TxtTimer,
DateFrom, TxtPlace) 
values ( @tempUser1,'''' , 0, @Status, N''Tạo mới đơn hàng'', 0, 1, @userId,
GETDATE(), '''', null, '''')


end')
PRINT '  âœ“ Procedure sp_AddCandidateAndOrderMarketting created';
-- Stored Procedure: sp_AllCountCVGroupByDate
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_AllCountCVGroupByDate]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_AllCountCVGroupByDate...';
    DROP PROCEDURE [dbo].[sp_AllCountCVGroupByDate];
END

PRINT '  â†’ Creating procedure sp_AllCountCVGroupByDate...';
EXEC('CREATE procedure [dbo].[sp_AllCountCVGroupByDate]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 100,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1,
@Isapply int  =0,
@IsClose int = 0,
@IsReturn int =0 

)
as
begin

if(@GroupId =38)
begin 
 set @GroupId = 7
end 

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0 



'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);

declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  SELECT 
	CAST(d.CreateAt AS date) AS dateCreate, d.Assignee,
	dbo.getUserName(Assignee) as "userName" ,
	count(d.Id) as total
	FROM [order] d   '';


if(@Token is not null )
begin

 set @where += '' and (d.Code like  N''''%'' + @Token +''%'''''';
 set @where += '' or dbo.getFullInfo(d.CandidateId) like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Code like  N''''%'' + @Token +''%'''')'';
end;

if( @Status >-1 )
begin 
set @where += '' and d.Status =  @Status ''; 
end 

if( @Job >-1 )
begin 
set @where += '' and d.JobId =  @Job ''; 
end 

if(@From is not null )
begin 
set @where += '' 
and (  d.CreateAt  >= @fromDate )
 ''; 
end 

if(@to is not null )
begin 
set @where += '' and ( d.CreateAt  <= @toDate ) ''; 
end 

if( @userId > 0 )
begin 
	set @where += '' and d.Assignee in ( select id  from  getAllUserByUserId(@userId))''; 
end 

if(@GroupId >0 )
begin 
   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.Assignee  = @MemberId ''; 
end 
set @where +='' GROUP BY CAST(d.CreateAt AS date), d.Assignee ''; 
set @where +='' order by dateCreate asc,  Assignee asc ''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @IsReturn int,  @limit int, @IsClose int,  @fromDate datetime,@toDate datetime, @Isapply int , @Status int , @userId int, @Job int, @GroupId int, @MemberId int '';

EXECUTE sp_executesql @mainClause,@params,
@offset = @offset,
@Isapply = @Isapply,
@IsClose= @IsClose,
@IsReturn =  @IsReturn,
@limit = @limit,@GroupId = @GroupId, @MemberId = @MemberId,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId, @Job = @Job

end')
PRINT '  âœ“ Procedure sp_AllCountCVGroupByDate created';
-- Stored Procedure: sp_BHXHItem_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_BHXHItem_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_BHXHItem_getAll...';
    DROP PROCEDURE [dbo].[sp_BHXHItem_getAll];
END

PRINT '  â†’ Creating procedure sp_BHXHItem_getAll...';
EXEC('CREATE procedure [dbo].[sp_BHXHItem_getAll]
(
	@UserName varchar(8) = ''''
)
as
begin
 select top 1 * from BHXHItem where UserName  = @UserName
end')
PRINT '  âœ“ Procedure sp_BHXHItem_getAll created';
-- Stored Procedure: sp_BHXHItem_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_BHXHItem_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_BHXHItem_insert...';
    DROP PROCEDURE [dbo].[sp_BHXHItem_insert];
END

PRINT '  â†’ Creating procedure sp_BHXHItem_insert...';
EXEC('CREATE PROCEDURE [dbo].[sp_BHXHItem_insert]
    @UserName NVARCHAR(100),
    @NumberCode NVARCHAR(50) = NULL,
    @IsConfirmletter BIT = NULL,
    @Relid NVARCHAR(50) = NULL,
    @ChungTuThue NVARCHAR(255) = NULL,
    @CreatedBy INT = NULL,
    @Dependent NVARCHAR(50) = NULL,
    @DependentName NVARCHAR(255) = NULL,
    @PITDate DATETIME = NULL,
    @EffectedFrom DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[BHXHItem] (
        [UserName],[NumberCode],[IsConfirmletter],[Relid],[ChungTuThue],
        [CreatedBy],[Dependent],[DependentName],[PITDate],[EffectedFrom]
    ) VALUES (
        @UserName,@NumberCode,@IsConfirmletter,@Relid,@ChungTuThue,
        @CreatedBy,@Dependent,@DependentName,@PITDate,@EffectedFrom
    );
END')
PRINT '  âœ“ Procedure sp_BHXHItem_insert created';
-- Stored Procedure: sp_BHXHItem_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_BHXHItem_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_BHXHItem_update...';
    DROP PROCEDURE [dbo].[sp_BHXHItem_update];
END

PRINT '  â†’ Creating procedure sp_BHXHItem_update...';
EXEC('CREATE PROCEDURE [dbo].[sp_BHXHItem_update]
    @Id INT,
    @IsConfirmletter BIT = NULL,
    @NumberCode NVARCHAR(50) = NULL,
    @ChungTuThue NVARCHAR(255) = NULL,
    @DependentName NVARCHAR(255) = NULL,
    @Dependent NVARCHAR(50) = NULL,
    @EffectedFrom DATETIME = NULL,
    @PITDate DATETIME = NULL,
    @UpdatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[BHXHItem]
    SET [IsConfirmletter] = @IsConfirmletter,
        [NumberCode] = @NumberCode,
        [ChungTuThue] = @ChungTuThue,
        [DependentName] = @DependentName,
        [Dependent] = @Dependent,
        [EffectedFrom] = @EffectedFrom,
        [PITDate] = @PITDate,
        [UpdatedBy] = @UpdatedBy
    WHERE [Id] = @Id;
END')
PRINT '  âœ“ Procedure sp_BHXHItem_update created';
-- Stored Procedure: sp_candidate_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_candidate_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_candidate_getAll...';
    DROP PROCEDURE [dbo].[sp_candidate_getAll];
END

PRINT '  â†’ Creating procedure sp_candidate_getAll...';
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



--if( @LoadAll = 0)
--begin 
--if( @userId > 0 )
--begin 
--	set @where += '' and  d.Assignee in ( select id  from  getAllUserByUserId(@userId))
--	''; 
--end 

--end



--if(@GroupId >0 )
--begin 
--   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
--end 
--if(@MemberId >0 )
--begin 
--   set @where +=  '' and d.Assignee  = @MemberId ''; 
--end 
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
PRINT '  âœ“ Procedure sp_candidate_getAll created';
-- Stored Procedure: sp_candidate_getAllMarketting
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_candidate_getAllMarketting]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_candidate_getAllMarketting...';
    DROP PROCEDURE [dbo].[sp_candidate_getAllMarketting];
END

PRINT '  â†’ Creating procedure sp_candidate_getAllMarketting...';
EXEC('CREATE procedure [dbo].[sp_candidate_getAllMarketting]
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
@LoadAll  int =0 
)
as
begin


declare @roleCode  varchar(3);
set @roleCode = '''';

select top 1 @roleCode = RoleCode from Employees where  id = @userId 

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord, 
dbo.getFullNameSorce( d.CreatedBy) as SourceName ,
d.* from  Candidate d  '';


if(@Token is not null )
begin

 set @where += '' and (d.Code like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Name like  N''''%'' + @Token +''%'''''';

 set @where += '' or d.Phone like  N''''%'' + @Token +''%'''')'';
end;

if(@userId > 0)
begin 
set @where += '' and d.id not in ( '' +  cast (@userId as varchar(5)) + '' ) ''; 
end 




if(@roleCode = ''4'')
begin 
		set @where += '' and d.source > 0  ''; 
		set @where += '' and ( d.CreatedBy in ( select id  from  markettingGetAllViewId(@userId))) 
	''; 


end

else 
begin 
if( @userId > 0 )
begin 
	set @where += '' and ( d.CreatedBy in ( select id  from  getAllUserByUserId(@userId)) or 
	 d.Assignee in ( select id  from  getAllUserByUserId(@userId))
	) 
	''; 
end 
end 



set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int '';



EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId

end')
PRINT '  âœ“ Procedure sp_candidate_getAllMarketting created';
-- Stored Procedure: sp_candidate_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_candidate_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_candidate_insert...';
    DROP PROCEDURE [dbo].[sp_candidate_insert];
END

PRINT '  â†’ Creating procedure sp_candidate_insert...';
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
	@IsActive bit = null
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
			Referrer
		   )
     VALUES
           (
		    @tempUser,@Name,@Email, @AvatarLink,@CVLink, @ShortDes, @Phone, @Noted, @Status,0,@IsActive,@CreatedBy,@CreatedBy,

			GETDATE(),getdate(),@Dob , @Source , @assingee1, 
			@DepartmentId, @Position, @ManagerId ,@Referrer
		   
		   )

end')
PRINT '  âœ“ Procedure sp_candidate_insert created';
-- Stored Procedure: sp_candidate_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_candidate_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_candidate_update...';
    DROP PROCEDURE [dbo].[sp_candidate_update];
END

PRINT '  â†’ Creating procedure sp_candidate_update...';
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
	@StatusHuman  int =null
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
		 UpdateAt = getdate()
	where id = @id 

	

end')
PRINT '  âœ“ Procedure sp_candidate_update created';
-- Stored Procedure: sp_Check_Duplicate
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Check_Duplicate]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Check_Duplicate...';
    DROP PROCEDURE [dbo].[sp_Check_Duplicate];
END

PRINT '  â†’ Creating procedure sp_Check_Duplicate...';
EXEC('CREATE procedure [dbo].[sp_Check_Duplicate]
(

@email varchar(30) =  '''',
@phone varchar(20) =''''
)
as
begin

 select top (1) * from Candidate where Phone = @phone or Email = @email 
end')
PRINT '  âœ“ Procedure sp_Check_Duplicate created';
-- Stored Procedure: sp_digital_checkAllowSearch
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_digital_checkAllowSearch]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_digital_checkAllowSearch...';
    DROP PROCEDURE [dbo].[sp_digital_checkAllowSearch];
END

PRINT '  â†’ Creating procedure sp_digital_checkAllowSearch...';
EXEC('create procedure [dbo].[sp_digital_checkAllowSearch]
(
	@userid int 
)
as
begin

declare  @countEdu int;
declare  @countExp int;
declare  @countOther int;

select @countEdu = count(id) from  educationUser where CreatedBy =@userid

select @countExp = count(id) from  ExperienceUser  where CreatedBy =@userid

select @countOther = count(id) from OtherProfileUser where TypeData = 1 and CreatedBy = @userid

if ( @countEdu > 0 and @countExp >0  and @countOther >0)
begin 
  select 1 as  ''allow''
end 
else 
begin 
 select 0 as  ''allow''
end 


end')
PRINT '  âœ“ Procedure sp_digital_checkAllowSearch created';
-- Stored Procedure: sp_DocumentData_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_DocumentData_getAll...';
    DROP PROCEDURE [dbo].[sp_DocumentData_getAll];
END

PRINT '  â†’ Creating procedure sp_DocumentData_getAll...';
EXEC('CREATE procedure [dbo].[sp_DocumentData_getAll]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@RelId int = -1,
@RelCode varchar(10) = '''',
@DataType int  = 0,

@Page int =1,
@Status int = -1,
@Limit int = 1000,
@From datetime = null,
@To datetime = null
)
as
begin

set @Limit =1000;



declare @where  nvarchar(max) = '' where  1= 1 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);

declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord,
d.* from  DocumentData d  '';

if( @RelId > 0 )
begin 
	set @where += '' and   d.RelId = @RelId ''; 
end 

if(@DataType >0 )
begin 
	set @where += '' and   d.DataType = @DataType ''; 
end 
set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int,
@fromDate datetime,@toDate datetime ,  @DataType  int, 
@RelId int '';

EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @RelId = @RelId, @DataType = @DataType



end')
PRINT '  âœ“ Procedure sp_DocumentData_getAll created';
-- Stored Procedure: sp_DocumentData_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_DocumentData_insert...';
    DROP PROCEDURE [dbo].[sp_DocumentData_insert];
END

PRINT '  â†’ Creating procedure sp_DocumentData_insert...';
EXEC('CREATE procedure [dbo].[sp_DocumentData_insert]
(
	@RelId int null,
	@RelCode varchar(10) null,
	@DisplayText nvarchar(100) NULL,
	@ValueFile nvarchar(500) NULL,
	@Code  varchar(5)  null ,
	@CreatedBy int = null,
	@DataType  int =  0
	
)
as
begin

 INSERT INTO [dbo].[DocumentData]
           (
		   DataType,
				RelId
			   ,RelCode,
				Code,
				DisplayText,
				ValueFile,
				[status]
			   ,[Deleted]
			   ,[CreatedBy]
			   ,[UpdatedBy]
			   ,[CreateAt]
			   ,[UpdateAt]
		   )
     VALUES
           (
		   @DataType,
			   @RelId, 
			   @RelCode ,
			   @Code,
			   @DisplayText,
			   @ValueFile ,
			   0,
			   0,
			   @CreatedBy,
			   @CreatedBy, 
			   getdate(), 
			   getdate() 
		   )

end')
PRINT '  âœ“ Procedure sp_DocumentData_insert created';
-- Stored Procedure: sp_DocumentData_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_DocumentData_update...';
    DROP PROCEDURE [dbo].[sp_DocumentData_update];
END

PRINT '  â†’ Creating procedure sp_DocumentData_update...';
EXEC('CREATE procedure [dbo].[sp_DocumentData_update]
(
	@id int null,
	@ValueFile nvarchar(500) NULL,
	@CreatedBy int = null
)
as
begin
update  DocumentData 
set 
ValueFile = @ValueFile,
UpdateAt = getdate(),
UpdatedBy = @CreatedBy
where id = @id

end')
PRINT '  âœ“ Procedure sp_DocumentData_update created';
-- Stored Procedure: sp_emp_changePassword
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_emp_changePassword]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_emp_changePassword...';
    DROP PROCEDURE [dbo].[sp_emp_changePassword];
END

PRINT '  â†’ Creating procedure sp_emp_changePassword...';
EXEC('CREATE procedure [dbo].[sp_emp_changePassword]
(
		@password varchar(300) null,
		
		@id int = null
)
		
as
begin

		  update Employees set 
					  Pass = @password
		  where id = @id

 end')
PRINT '  âœ“ Procedure sp_emp_changePassword created';
-- Stored Procedure: sp_emp_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_emp_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_emp_insert...';
    DROP PROCEDURE [dbo].[sp_emp_insert];
END

PRINT '  â†’ Creating procedure sp_emp_insert...';
EXEC('CREATE PROCEDURE [dbo].[sp_emp_insert]
(
    @EducationLevel VARCHAR(4) = NULL, 
    @UserName VARCHAR(8) = NULL,        -- Sẽ không dùng, SP tự tạo
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
    @Maritalstatus VARCHAR(4) = NULL,
    @StatusWork INT = -1,
    @BankAccount VARCHAR(30) = NULL,
    @BankName NVARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    ---------------------------------------------------------------
    -- BẮT ĐẦU TRANSACTION
    ---------------------------------------------------------------
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Now DATETIME = GETDATE();

        -----------------------------------------------------------
        -- 1. INSERT bản ghi trước (UserName = NULL)
        -----------------------------------------------------------
        INSERT INTO [dbo].[Employees]
        (
            UserName, FullName, Phone, Noted, RoleCode, Deleted, IsActive,
            CreatedBy, UpdatedBy, CreateAt, UpdateAt, Pass, dob, LineCode,
            Onboard, ColorCode, TypeAccount, DepartmentCode, DocumentStatus,
            PositionCode, RelationCode, NationalId, NationalDate, NationalPlace,
            PermanentAddress, TemporaryAddress, ManagerId, Email, CVLink, status,
            EducationLevel, DocumentCheck, Gender, PlaceOfBirth, Religion,
            PersonalEmail, BeneficiaryName, Maritalstatus, StatusWork,
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
            @Religion, @PersonalEmail, @BeneficiaryName, @Maritalstatus,
            @StatusWork, @BankAccount, @BankName
        );

        -----------------------------------------------------------
        -- 2. Lấy Id mới tạo
        -----------------------------------------------------------
        DECLARE @newId INT = SCOPE_IDENTITY();

        -----------------------------------------------------------
        -- 3. Tạo UserName an toàn từ ID
        -- FORMAT: VS0001 / VS0010 / VS0100 / VS1000
        -----------------------------------------------------------
        DECLARE @tempUser VARCHAR(6);
        SET @tempUser = CONCAT(''VS'', RIGHT(''000'' + CAST(@newId AS VARCHAR(4)), 4));

        -----------------------------------------------------------
        -- 4. UPDATE lại UserName
        -----------------------------------------------------------
        UPDATE Employees
        SET UserName = @tempUser
        WHERE Id = @newId;

        -----------------------------------------------------------
        -- 5. Nếu người tạo là LEAD → tự add vào GroupMember
        -----------------------------------------------------------
        DECLARE @groupid INT = (
            SELECT TOP 1 Id
            FROM [Group]
            WHERE ManagerId = @CreatedBy AND ISNULL(Deleted,0) = 0
        );

        IF @groupid IS NOT NULL
        BEGIN
            INSERT INTO GroupMember(GroupId, MemberId, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt)
            VALUES (@groupid, @newId, 0, @CreatedBy, @CreatedBy, @Now, @Now);
        END

        -----------------------------------------------------------
        -- 6. Commit và trả ID
        -----------------------------------------------------------
        COMMIT TRANSACTION;
        SELECT @newId;

    END TRY
    BEGIN CATCH
        -----------------------------------------------------------
        -- 7. Lỗi → rollback
        -----------------------------------------------------------
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrLine INT = ERROR_LINE();

        RAISERROR (''Lỗi khi tạo nhân viên: %s (Line %d)'', 16, 1, @ErrMsg, @ErrLine);
    END CATCH
END')
PRINT '  âœ“ Procedure sp_emp_insert created';
-- Stored Procedure: sp_emp_login
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_emp_login]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_emp_login...';
    DROP PROCEDURE [dbo].[sp_emp_login];
END

PRINT '  â†’ Creating procedure sp_emp_login...';
EXEC('CREATE procedure [dbo].[sp_emp_login]
(
@userName varchar(8) null, 
@password varchar(300) null

)
as
begin

		select * from Employees d where d.UserName = @userName and 1=1 and ISNULL(d.Deleted,0) =0	

end')
PRINT '  âœ“ Procedure sp_emp_login created';
-- Stored Procedure: sp_emp_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_emp_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_emp_update...';
    DROP PROCEDURE [dbo].[sp_emp_update];
END

PRINT '  â†’ Creating procedure sp_emp_update...';
EXEC('CREATE PROCEDURE [dbo].[sp_emp_update]
(
    @id INT,
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
    @FullName NVARCHAR(50) = NULL,
    @Email VARCHAR(50) = NULL, 
    @CVLink VARCHAR(500) = '''', 
    @Noted NVARCHAR(500) = '''',
    @DocumentStatus INT = -1,
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
    @DocumentCheck VARCHAR(200) = '''',
    -- Các tham số mới
    @Gender NVARCHAR(50) = NULL,
    @PlaceOfBirth NVARCHAR(255) = NULL,
    @Religion NVARCHAR(100) = NULL,
    @PersonalEmail NVARCHAR(255) = NULL,
    @BeneficiaryName NVARCHAR(255) = NULL
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
        -- Các cột mới
        Gender = @Gender,
        PlaceOfBirth = @PlaceOfBirth,
        Religion = @Religion,
        PersonalEmail = @PersonalEmail,
        BeneficiaryName = @BeneficiaryName
    WHERE id = @id;
    
    IF (@SourceFrom > 0)
    BEGIN 
        UPDATE Employees 
        SET SourceFrom = @SourceFrom
        WHERE id = @id;
    END 
END')
PRINT '  âœ“ Procedure sp_emp_update created';
-- Stored Procedure: sp_Employee_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Employee_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Employee_getAll...';
    DROP PROCEDURE [dbo].[sp_Employee_getAll];
END

PRINT '  â†’ Creating procedure sp_Employee_getAll...';
EXEC('CREATE procedure [dbo].[sp_Employee_getAll]
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
  dbo.getDisplayMasterdata(d.DepartmentCode) as DepartmentText,
    dbo.getDisplayMasterdata(d.PositionCode) as PositionText,
dbo.getFullName( d.ManagerId)  as GroupName
	
  
 

from  Employees d  '';



if(@Token is not null )
begin

 set @where += '' and (d.UserName like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.FullName like  N''''%'' + @Token +''%'''''';

 set @where += '' or d.Phone like  N''''%'' + @Token +''%'''')'';
end;

--if(@userId > 0)
--begin 
--set @where += '' and d.id not in ( '' +  cast (@userId as varchar(5)) + '' ) ''; 
--end 


if( @IsDeleted  < 1 )
begin 

set @where += '' and isnull(d.Deleted,0) = 0  ''; 

end 




if( @Status >-1 )
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


if( @userId > 0 )
begin 
	set @where += '' and d.id in ( select id  from  getAllUserByUserId(@userId)) 
	''; 
end 




set @where +='' order by d.UpdateAt desc''; 
--set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where


set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int '';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId


end')
PRINT '  âœ“ Procedure sp_Employee_getAll created';
-- Stored Procedure: sp_GetAllCountCVApplyGroupByDate
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllCountCVApplyGroupByDate]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_GetAllCountCVApplyGroupByDate...';
    DROP PROCEDURE [dbo].[sp_GetAllCountCVApplyGroupByDate];
END

PRINT '  â†’ Creating procedure sp_GetAllCountCVApplyGroupByDate...';
EXEC('CREATE procedure [dbo].[sp_GetAllCountCVApplyGroupByDate]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 100,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1,
@Isapply int  =0,
@IsClose int = 0,
@IsReturn int =0 

)
as
begin

if(@GroupId =38)
begin 
 set @GroupId = 7
end 

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0 

and  isnull(d.Isapply,0) = 1  


'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);

declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  SELECT 
	CAST(d.CreateAt AS date) AS dateCreate, d.Assignee,
	dbo.getUserName(Assignee) as "userName" ,
	count(d.Id) as total
	FROM [order] d   '';


if(@Token is not null )
begin

 set @where += '' and (d.Code like  N''''%'' + @Token +''%'''''';
 set @where += '' or dbo.getFullInfo(d.CandidateId) like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Code like  N''''%'' + @Token +''%'''')'';
end;

if( @Status >-1 )
begin 
set @where += '' and d.Status =  @Status ''; 
end 

if( @Job >-1 )
begin 
set @where += '' and d.JobId =  @Job ''; 
end 

if(@From is not null )
begin 
set @where += '' 
and (  d.CreateAt  >= @fromDate )
 ''; 
end 

if(@to is not null )
begin 
set @where += '' and ( d.CreateAt  <= @toDate ) ''; 
end 

if( @userId > 0 )
begin 
	set @where += '' and d.Assignee in ( select id  from  getAllUserByUserId(@userId))''; 
end 

if(@GroupId >0 )
begin 
   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.Assignee  = @MemberId ''; 
end 
set @where +='' GROUP BY CAST(d.CreateAt AS date), d.Assignee ''; 
set @where +='' order by dateCreate asc,  Assignee asc ''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @IsReturn int,  @limit int, @IsClose int,  @fromDate datetime,@toDate datetime, @Isapply int , @Status int , @userId int, @Job int, @GroupId int, @MemberId int '';

EXECUTE sp_executesql @mainClause,@params,
@offset = @offset,
@Isapply = @Isapply,
@IsClose= @IsClose,
@IsReturn =  @IsReturn,
@limit = @limit,@GroupId = @GroupId, @MemberId = @MemberId,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId, @Job = @Job

end')
PRINT '  âœ“ Procedure sp_GetAllCountCVApplyGroupByDate created';
-- Stored Procedure: sp_GetAllCountCVOnboardGroupByDate
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllCountCVOnboardGroupByDate]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_GetAllCountCVOnboardGroupByDate...';
    DROP PROCEDURE [dbo].[sp_GetAllCountCVOnboardGroupByDate];
END

PRINT '  â†’ Creating procedure sp_GetAllCountCVOnboardGroupByDate...';
EXEC('CREATE procedure [dbo].[sp_GetAllCountCVOnboardGroupByDate]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 100,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1,
@Isapply int  =0,
@IsClose int = 0,
@IsReturn int =0 

)
as
begin

if(@GroupId =38)
begin 
 set @GroupId = 7
end 

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0 

and  isnull(d.Isapply,0)= 1 and  ( d.Status = 12  or   isnull(d.Result,0)= 1 )  

'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);

declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  SELECT 
	CAST(d.CreateAt AS date) AS dateCreate, d.Assignee,
	dbo.getUserName(Assignee) as "userName" ,
	count(d.Id) as total
	FROM [order] d   '';




if( @Status >-1 )
begin 
set @where += '' and d.Status =  @Status ''; 
end 

if( @Job >-1 )
begin 
set @where += '' and d.JobId =  @Job ''; 
end 

if(@From is not null )
begin 
set @where += '' 
and (  d.CreateAt  >= @fromDate )
 ''; 
end 

if(@to is not null )
begin 
set @where += '' and ( d.CreateAt  <= @toDate ) ''; 
end 

if( @userId > 0 )
begin 
	set @where += '' and d.Assignee in ( select id  from  getAllUserByUserId(@userId))''; 
end 

if(@GroupId >0 )
begin 
   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.Assignee  = @MemberId ''; 
end 
set @where +='' GROUP BY CAST(d.CreateAt AS date), d.Assignee ''; 
set @where +='' order by dateCreate asc,  Assignee asc ''; 
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @IsReturn int,  @limit int, @IsClose int,  @fromDate datetime,@toDate datetime, @Isapply int , @Status int , @userId int, @Job int, @GroupId int, @MemberId int '';



EXECUTE sp_executesql @mainClause,@params,
@offset = @offset,
@Isapply = @Isapply,
@IsClose= @IsClose,
@IsReturn =  @IsReturn,
@limit = @limit,@GroupId = @GroupId, @MemberId = @MemberId,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId, @Job = @Job


end')
PRINT '  âœ“ Procedure sp_GetAllCountCVOnboardGroupByDate created';
-- Stored Procedure: sp_GetAllCVInfo
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllCVInfo]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_GetAllCVInfo...';
    DROP PROCEDURE [dbo].[sp_GetAllCVInfo];
END

PRINT '  â†’ Creating procedure sp_GetAllCVInfo...';
EXEC('CREATE procedure [dbo].[sp_GetAllCVInfo]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 100,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;

set @mainClause = ''  select count(d.id) over() as TotalRecord,
dbo.getUserName( d.CreatedBy) as AuthorName ,
d.* from  Candidate d  '';






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


if( @userId > 0 )
begin 
	set @where += '' and d.CreatedBy in ( select id from markettingGetAllViewId(@userId)  ) ''; 
end 

set @where +='' order by d.UpdateAt desc''; 

set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int, @Job int '';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId,  @Job = @Job

end')
PRINT '  âœ“ Procedure sp_GetAllCVInfo created';
-- Stored Procedure: sp_GetAllCVNotMarketing
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllCVNotMarketing]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_GetAllCVNotMarketing...';
    DROP PROCEDURE [dbo].[sp_GetAllCVNotMarketing];
END

PRINT '  â†’ Creating procedure sp_GetAllCVNotMarketing...';
EXEC('CREATE  procedure [dbo].[sp_GetAllCVNotMarketing]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 100,
@From datetime = null,
@To datetime = null,
@Job int =-1,
@marketting int = 0
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);




declare @offset int = 0;

set @mainClause = ''  select count(d.id) over() as TotalRecord,
dbo.getUserName( d.CreatedBy) as AuthorName ,
d.* from  Candidate d   '';




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




if( @userId > 0 )
begin 
	set @where += '' and d.Assignee  in ( select id  from  getAllUserByUserId(@userId)) ''; 
end 

if(@GroupId >0 )
begin 
   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.Assignee  = @MemberId ''; 
end 

set @where +='' order by d.UpdateAt desc''; 
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,
@toDate datetime , @Status int , @userId int, @Job int,
@GroupId int, @MemberId int
'';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId , @Job = @Job,
@GroupId = @GroupId, @MemberId = @MemberId

end')
PRINT '  âœ“ Procedure sp_GetAllCVNotMarketing created';
-- Stored Procedure: sp_GetAllOnboardCV
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllOnboardCV]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_GetAllOnboardCV...';
    DROP PROCEDURE [dbo].[sp_GetAllOnboardCV];
END

PRINT '  â†’ Creating procedure sp_GetAllOnboardCV...';
EXEC('CREATE procedure [dbo].[sp_GetAllOnboardCV]
(
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0  '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;


set @mainClause = ''  select 
		count(d.id) over() as TotalRecord,
		dbo.getFullInfo(d.CandidateId) as CandidateFullName,
		d.OnboardDate as  OnboardDate,
		dbo.getJobTitle(d.JobId) as PositionText,
		dbo.getNamePartner(d.partnerId) as PartnerName ,
		dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
		dbo.getFullName(d.Assignee ) as       AssigeeName,

			dbo.getDisplayMasterDataById(d.Status) as StatusText,
		dbo.getDisplayMasterDataById(d.SystemStatus) as SystemStatusText,
		dbo.getUserName( d.CreatedBy) as AuthorName ,d.OrderCode, d.CreateAt, 
		d.UpdateAt,d.result, d.Id , dbo.getOrderIdByCode( d.OrderCode ) as  OrderId, 
		d.Warrantydate, d.Dpd, d.Assignee 
		
		from  [OnboardMember] d  '';

if( @Job >-1 )
begin 
set @where += '' and d.JobId =  @Job ''; 
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

if( @userId > 0 )
begin 
	set @where += '' and d.CreatedBy in ( select id from getAllUserByUserId(@userId)  ) ''; 
end 
if(@GroupId >0 )
begin 
   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.Assignee  = @MemberId ''; 
end 

set @where +='' order by d.CreateAt desc''; 

set @mainClause = @mainClause +  @where
set @params =N'' @fromDate datetime,@toDate datetime , @Status int , @userId int, @Job int,
@GroupId int, @MemberId int 
'';


EXECUTE sp_executesql @mainClause,@params, 
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId,  @Job = @Job,
@GroupId = @GroupId, @MemberId = @MemberId 

end')
PRINT '  âœ“ Procedure sp_GetAllOnboardCV created';
-- Stored Procedure: sp_GetAllOrderApply
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllOrderApply]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_GetAllOrderApply...';
    DROP PROCEDURE [dbo].[sp_GetAllOrderApply];
END

PRINT '  â†’ Creating procedure sp_GetAllOrderApply...';
EXEC('CREATE procedure [dbo].[sp_GetAllOrderApply]
(
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0  and   d.Isapply = 1 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;

set @mainClause = ''  select count(d.id) over() as TotalRecord, 
dbo.getJobTitle( d.JobId) as PositionText ,
dbo.getFullInfo(d.CandidateId) as UserNameText, 
dbo.getFullInfo(d.CandidateId) as CandidateFullName, 
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getExtraInfoDataById(d.Status) as ExtraColor,
dbo.getDisplayMasterDataById(d.Status) as StatusText,
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getNamePartner(d.partnerId) as PartnerName ,
dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
dbo.getUserName( d.CreatedBy) as AuthorName ,
d.* from  [Order] d  inner join Candidate e on d.CandidateId = e.Id  '';

if( @Status >-1 )
begin 
	set @where += '' and d.Status =  @Status ''; 
end 

if( @Job >-1 )
begin 
set @where += '' and d.JobId =  @Job ''; 
end 
if(@GroupId >0 )
begin 
   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.Assignee  = @MemberId ''; 
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

if( @userId > 0 )
begin 
	set @where += '' and d.CreatedBy in ( select id from getAllUserByUserId(@userId)  ) ''; 
end 

set @where +='' order by d.UpdateAt desc''; 

set @mainClause = @mainClause +  @where
set @params =N'' @fromDate datetime,@toDate datetime , @Status int , @userId int, @Job int ,
@GroupId int, @MemberId int 
'';


EXECUTE sp_executesql @mainClause,@params, 
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId,  @Job = @Job,
@GroupId = @GroupId, @MemberId = @MemberId

end')
PRINT '  âœ“ Procedure sp_GetAllOrderApply created';
-- Stored Procedure: sp_GetAllOrderDraft
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllOrderDraft]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_GetAllOrderDraft...';
    DROP PROCEDURE [dbo].[sp_GetAllOrderDraft];
END

PRINT '  â†’ Creating procedure sp_GetAllOrderDraft...';
EXEC('CREATE procedure [dbo].[sp_GetAllOrderDraft]
(
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0  and  isnull(d.Isapply,0) = 0 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;

set @mainClause = ''  select count(d.id) over() as TotalRecord, 

dbo.getJobTitle( d.JobId) as PositionText ,
dbo.getFullInfo(d.CandidateId) as UserNameText, 
dbo.getFullInfo(d.CandidateId) as CandidateFullName, 
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getExtraInfoDataById(d.Status) as ExtraColor,
dbo.getDisplayMasterDataById(d.Status) as StatusText,
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getNamePartner(d.partnerId) as PartnerName ,
dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
dbo.getUserName( d.CreatedBy) as AuthorName ,
d.* from  [Order] d  left join Candidate e on d.CandidateId = e.Id  '';

if( @Status >-1 )
begin 
	set @where += '' and d.Status =  @Status ''; 
end 

if( @Job >-1 )
begin 
set @where += '' and d.JobId =  @Job ''; 
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

if( @userId > 0 )
begin 
	set @where += '' and d.CreatedBy in ( select id from getAllUserByUserId(@userId)  ) ''; 
end 
if(@GroupId >0 )
begin 
   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.Assignee  = @MemberId ''; 
end 


set @where +='' order by d.UpdateAt desc''; 

set @mainClause = @mainClause +  @where
set @params =N'' @fromDate datetime,@toDate datetime , @Status int , @userId int, @Job int,
@GroupId int, @MemberId int  
'';


EXECUTE sp_executesql @mainClause,@params, 
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId,  @Job = @Job,
@GroupId = @GroupId, @MemberId = @MemberId 

end')
PRINT '  âœ“ Procedure sp_GetAllOrderDraft created';
-- Stored Procedure: sp_getALLOrderInfo
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_getALLOrderInfo]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_getALLOrderInfo...';
    DROP PROCEDURE [dbo].[sp_getALLOrderInfo];
END

PRINT '  â†’ Creating procedure sp_getALLOrderInfo...';
EXEC('CREATE procedure [dbo].[sp_getALLOrderInfo]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 100,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;

set @mainClause = ''  select count(d.id) over() as TotalRecord, 
dbo.getJobTitle( d.JobId) as PositionText ,
dbo.getFullInfo(d.CandidateId) as UserNameText, 
dbo.getFullInfo(d.CandidateId) as CandidateFullName, 
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getExtraInfoDataById(d.Status) as ExtraColor,
dbo.getDisplayMasterDataById(d.Status) as StatusText,
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getNamePartner(d.partnerId) as PartnerName ,
dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
dbo.getUserName( d.CreatedBy) as AuthorName ,
d.* from  [Order] d  inner join Candidate e on d.CandidateId = e.Id  '';


if(@Token is not null )
begin

 set @where += '' and (d.Code like  N''''%'' + @Token +''%'''''';
 set @where += '' or dbo.getFullInfo(d.CandidateId) like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Code like  N''''%'' + @Token +''%'''')'';
end;


if( @Status >-1 )
begin 

set @where += '' and d.Status =  @Status ''; 

end 

if( @Job >-1 )
begin 
set @where += '' and d.JobId =  @Job ''; 
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


if( @userId > 0 )
begin 
	set @where += '' and d.CreatedBy in ( select id from markettingGetAllViewId(@userId)  ) ''; 
end 

set @where +='' order by d.UpdateAt desc''; 

set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int, @Job int '';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId,  @Job = @Job

end')
PRINT '  âœ“ Procedure sp_getALLOrderInfo created';
-- Stored Procedure: sp_getALLOrderInfoNotMarketing
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_getALLOrderInfoNotMarketing]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_getALLOrderInfoNotMarketing...';
    DROP PROCEDURE [dbo].[sp_getALLOrderInfoNotMarketing];
END

PRINT '  â†’ Creating procedure sp_getALLOrderInfoNotMarketing...';
EXEC('CREATE  procedure [dbo].[sp_getALLOrderInfoNotMarketing]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 100,
@From datetime = null,
@To datetime = null,
@Job int =-1,
@marketting int = 0
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);




declare @offset int = 0;

set @mainClause = ''  select count(d.id) over() as TotalRecord, 
dbo.getJobTitle( d.JobId) as PositionText ,
dbo.getFullInfo(d.CandidateId) as UserNameText, 
dbo.getFullInfo(d.CandidateId) as CandidateFullName, 
dbo.getExtraInfoDataById(d.Status) as ExtraColor,
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getDisplayMasterDataById(d.Status) as StatusText,
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getNamePartner(d.partnerId) as PartnerName ,
dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
dbo.getUserName( d.CreatedBy) as AuthorName ,
d.* from  [Order] d  inner join Candidate e on d.CandidateId = e.Id 

'';


if(@Token is not null )
begin

 set @where += '' and (d.Code like  N''''%'' + @Token +''%'''''';
 set @where += '' or dbo.getFullInfo(d.CandidateId) like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Code like  N''''%'' + @Token +''%'''')'';
end;





if( @Status >-1 )
begin 

set @where += '' and d.Status =  @Status ''; 

end 

if( @Job >-1 )
begin 
set @where += '' and d.JobId =  @Job ''; 
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




if( @userId > 0 )
begin 
	set @where += '' and d.Assignee  in ( select id  from  getAllUserByUserId(@userId)) ''; 
end 

if(@GroupId >0 )
begin 
   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.Assignee  = @MemberId ''; 
end 

set @where +='' order by d.UpdateAt desc''; 
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int, @Job int,
@GroupId int, @MemberId int
'';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId , @Job = @Job,
@GroupId = @GroupId, @MemberId = @MemberId

end')
PRINT '  âœ“ Procedure sp_getALLOrderInfoNotMarketing created';
-- Stored Procedure: sp_GetALlOrderRemoveDup
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetALlOrderRemoveDup]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_GetALlOrderRemoveDup...';
    DROP PROCEDURE [dbo].[sp_GetALlOrderRemoveDup];
END

PRINT '  â†’ Creating procedure sp_GetALlOrderRemoveDup...';
EXEC('CREATE procedure [dbo].[sp_GetALlOrderRemoveDup]
(
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0   '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;

set @mainClause = ''  select count(d.id) over() as TotalRecord, 

dbo.getJobTitle( d.JobId) as PositionText ,
dbo.getFullInfo(d.CandidateId) as UserNameText, 
dbo.getFullInfo(d.CandidateId) as CandidateFullName, 
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getExtraInfoDataById(d.Status) as ExtraColor,
dbo.getDisplayMasterDataById(d.Status) as StatusText,
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getNamePartner(d.partnerId) as PartnerName ,
dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
dbo.getUserName( d.CreatedBy) as AuthorName ,
d.* from  [Order] d    '';

if( @Status >-1 )
begin 
	set @where += '' and d.Status =  @Status ''; 
end 

if( @Job >-1 )
begin 
set @where += '' and d.JobId =  @Job ''; 
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

if( @userId > 0 )
begin 
	set @where += '' and d.CreatedBy in ( select id from getAllUserByUserId(@userId)  ) ''; 
end 

if(@GroupId >0 )
begin 
   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.Assignee  = @MemberId ''; 
end 



set @where +='' order by d.UpdateAt desc''; 

set @mainClause = @mainClause +  @where
set @params =N'' @fromDate datetime,@toDate datetime , @Status int , @userId int, @Job int,
@GroupId int, @MemberId int   
'';


EXECUTE sp_executesql @mainClause,@params, 
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId,  @Job = @Job,
@GroupId = @GroupId, @MemberId = @MemberId  

end')
PRINT '  âœ“ Procedure sp_GetALlOrderRemoveDup created';
-- Stored Procedure: sp_GetAllRecordGroupByLineCode_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllRecordGroupByLineCode_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_GetAllRecordGroupByLineCode_getAll...';
    DROP PROCEDURE [dbo].[sp_GetAllRecordGroupByLineCode_getAll];
END

PRINT '  â†’ Creating procedure sp_GetAllRecordGroupByLineCode_getAll...';
EXEC('create procedure [dbo].[sp_GetAllRecordGroupByLineCode_getAll]
(
@PhoneLog varchar(20)  =null, 
@LineCode varchar(10) =null,
@Disposition  varchar(20) = null, 
@Token varchar(30) =  null,

@VendorId int =-1, 

@From datetime  =null, 

@To datetime  = null, 

@Limit int  = 10,

@Page  int = 1, 
@MemberId int  = -1,

@GroupId  int = -1, 
@OrderBy varchar (50) = null,
@UserId int = null 
	
)
as
begin


declare @roleId  int;
set @roleId =1;

select top 1  @roleId = f.RoleCode from Employees f where f.id = @UserId
declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0  '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = '' select count(d.id) over() as TotalRecord 
,d.*, dbo.getUserNamev2(d.userId) as userName 
from ReportTalkTimeGroupByDay d  '';
--if(@VendorId > 0)
--begin
--	set @where += '' and d.vendorid = '' +  cast (@VendorId as varchar(5)) + '' ''; 
--end



if(@roleId = 2 )
begin
	set @where += '' and d.LineCode = '' +  cast (@LineCode as varchar(10)) + '' ''; 
end




if(@UserId >0 )
begin
	set @where += '' and d.LineCode in  ( select linecode  from  dbo.getAllLineCodeViewByUserIdOriginal(@UserId)) ''; 
end 




	if(@GroupId >0 )
	begin
		set @where += '' and d.LineCode in  ( select linecode  from  dbo.getAllLineCodeViewByGroupId(@GroupId)) ''; 
	end

if(@MemberId >0 )
begin
	set @where += '' and d.LineCode in  ( select linecode  from  dbo.getAllLineCodeViewByUserIdOriginal(@MemberId)) ''; 
end 

if(@From is not null )
begin 
set @where += '' 
and d.BusinessTime  >= @fromDate
 ''; 
end 


if(@to is not null )
begin 
set @where += '' 
and  d.BusinessTime  <= @toDate
''; 
end 

set @where +='' order by d.linecode desc''; 
set @mainClause = @mainClause +  @where;




set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime, @UserId int, @GroupId int, @MemberId int  '';
EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit, @UserId  = @UserId, @GroupId= @GroupId,@MemberId= @MemberId,
@fromDate = @From,@toDate =@To



end')
PRINT '  âœ“ Procedure sp_GetAllRecordGroupByLineCode_getAll created';
-- Stored Procedure: sp_getAllTopImpact
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_getAllTopImpact]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_getAllTopImpact...';
    DROP PROCEDURE [dbo].[sp_getAllTopImpact];
END

PRINT '  â†’ Creating procedure sp_getAllTopImpact...';
EXEC('CREATE procedure [dbo].[sp_getAllTopImpact]
(

@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 10,
@From datetime = null,
@To datetime = null 
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord ,
dbo.getFullName (d.CreatedBy) as CreatedByName , 
dbo.getDisplayMasterDataById (d.NewStatus) as StatusName , 
d.* from OrderImpactHIstory   d   '';

if(@userId > 0)
begin 
set @where += '' and d.id not in ( '' +  cast (@userId as varchar(5)) + '' ) ''; 
end 

if( @Status >-1 )
begin 

set @where += '' and d.Status =  @Status ''; 

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


if( @userId > 0 )
begin 
	set @where += '' and d.CreatedBy in ( select id  from  getAllUserByUserId(@userId)) 
	''; 
end


if(@GroupId >0 )
begin 
   set @where +=  '' and d.CreatedBy in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.CreatedBy  = @MemberId ''; 
end 



set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int,
@GroupId int, @MemberId int'';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId,
@GroupId = @GroupId, @MemberId = @MemberId

end')
PRINT '  âœ“ Procedure sp_getAllTopImpact created';
-- Stored Procedure: sp_getAllTopImpactMarketting
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_getAllTopImpactMarketting]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_getAllTopImpactMarketting...';
    DROP PROCEDURE [dbo].[sp_getAllTopImpactMarketting];
END

PRINT '  â†’ Creating procedure sp_getAllTopImpactMarketting...';
EXEC('CREATE procedure [dbo].[sp_getAllTopImpactMarketting]
(

@userId int = null, 
@From datetime = null,
@To datetime = null,
@MemberId int = null,
@GroupId int =null,
@Limit int =10
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;
set @mainClause = '' select 
top 10  count(d.id) over() as TotalRecord ,
dbo.getFullName (d.CreatedBy) as CreatedByName , 
dbo.getDisplayMasterDataById (d.NewStatus) as StatusName , 
d.* 
from OrderImpactHIstory d inner join [Order] e on d.OrderCode = e.Code   '';
if( @userId > 0 )
begin 
	set @where += '' and e.CreatedBy in ( select id  from  markettingGetAllViewId(@userId)) 
	''; 
end 
set @where +='' order by d.CreateAt desc''; 
set @mainClause = @mainClause +  @where
set @params =N'' @fromDate datetime,@toDate datetime, @userId int '';


EXECUTE sp_executesql @mainClause,@params,
@fromDate = @From,@toDate =@To, @userId = @userId

end')
PRINT '  âœ“ Procedure sp_getAllTopImpactMarketting created';
-- Stored Procedure: sp_GetGetOverViewDashBoard_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetGetOverViewDashBoard_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_GetGetOverViewDashBoard_getAll...';
    DROP PROCEDURE [dbo].[sp_GetGetOverViewDashBoard_getAll];
END

PRINT '  â†’ Creating procedure sp_GetGetOverViewDashBoard_getAll...';
EXEC('create procedure [dbo].[sp_GetGetOverViewDashBoard_getAll]
(
@PhoneLog varchar(20)  =null, 
@LineCode varchar(10) =null,
@Disposition  varchar(20) = null, 
@Token varchar(30) =  null,

@VendorId int =null, 

@From datetime  =null, 

@To datetime  = null, 

@Limit int  = 10,

@Page  int = 1, 

@OrderBy varchar (50) = null,
@UserId int = null,
@MemberId int  = -1,
@GroupId  int = -1
	
)
as
begin



declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0  '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = '' select
sum (d.SumCall) as SumCall , sum(d.SumNoAgree) as SumNoAgree,  sum(d.TimeTalking) as TimeTalking, 

sum (d.TimeWaiting ) as TimeWaiting,
sum(d.Timcall) as Timcall ,   case when sum (d.SumCall)>0 then  100.0 * sum(d.SumAn)/ sum(d.SumCall) else 0 end  as [Perpercent]

from ReportTalkTimeGroupByDay d  '';



if(@LineCode > 0)
begin
	set @where += '' and d.LineCode = '' +  cast (@LineCode as varchar(10)) + '' ''; 
end

if(@UserId > 0 )
begin
	set @where += '' and d.LineCode in  ( select linecode  from  dbo.getAllLineCodeViewByUserIdOriginal(@UserId)) ''; 
end 


if(@GroupId >0 )
begin
	set @where += '' and d.LineCode in  ( select linecode  from  dbo.getAllLineCodeViewByGroupId(@GroupId)) ''; 
end

if(@MemberId >0 )
begin
	set @where += '' and d.LineCode in  ( select linecode  from  dbo.getAllLineCodeViewByUserIdOriginal(@MemberId)) ''; 
end 


if(@From is not null )
begin 
set @where += '' 
and d.BusinessTime  >= @fromDate
 ''; 
end 


if(@to is not null )
begin 
set @where += '' 
and  d.BusinessTime  <= @toDate
''; 
end

set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @UserId int, @GroupId int, @MemberId int '';
EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit, @UserId = @UserId,
@fromDate = @From,@toDate =@To, @GroupId= @GroupId,@MemberId= @MemberId




end')
PRINT '  âœ“ Procedure sp_GetGetOverViewDashBoard_getAll created';
-- Stored Procedure: sp_getParramDashboard
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_getParramDashboard]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_getParramDashboard...';
    DROP PROCEDURE [dbo].[sp_getParramDashboard];
END

PRINT '  â†’ Creating procedure sp_getParramDashboard...';
EXEC('CREATE procedure [dbo].[sp_getParramDashboard]
(

@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 10,
@From datetime = null,
@To datetime = null 
)
as
begin
  declare @countOrder int;
  set @countOrder = 0;
  declare @countCandidate int;
  set @countCandidate =0;

  select @countOrder = count(id)  from [Order] where 1=1
   select @countCandidate = count(id)  from Candidate where 1=1
  select @countOrder as CountOrderd, @countCandidate as CountCandidate 


end')
PRINT '  âœ“ Procedure sp_getParramDashboard created';
-- Stored Procedure: sp_getParramDashboardMarketting
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_getParramDashboardMarketting]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_getParramDashboardMarketting...';
    DROP PROCEDURE [dbo].[sp_getParramDashboardMarketting];
END

PRINT '  â†’ Creating procedure sp_getParramDashboardMarketting...';
EXEC('CREATE procedure [dbo].[sp_getParramDashboardMarketting]
(

@userId int = null, 
@Status int = -1,

@From datetime = null,
@To datetime = null
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;

set @mainClause = ''select count(d.id) over() as TotalRecord, 
dbo.getFullInfo(d.CandidateId) as UserNameText, 
d.Status,
dbo.getDisplayMasterDataById(d.Status) as StatusText 
 from  [Order] d 

'';


if( @Status >-1 )
begin 

set @where += '' and d.Status =  @Status ''; 
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

if( @userId > 0 )
begin 
	set @where += '' and d.CreatedBy in ( select id  from  markettingGetAllViewId(@userId))  ''; 
end 

set @where +='' order by d.CreateAt desc''; 

set @mainClause = @mainClause +  @where
set @params =N'' @fromDate datetime,@toDate datetime , @Status int , 
@userId int '';


EXECUTE sp_executesql @mainClause,@params,
@Status = @Status,
@userId = @userId,
@fromDate = @From,
@toDate =@To


end')
PRINT '  âœ“ Procedure sp_getParramDashboardMarketting created';
-- Stored Procedure: sp_Group_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Group_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Group_getAll...';
    DROP PROCEDURE [dbo].[sp_Group_getAll];
END

PRINT '  â†’ Creating procedure sp_Group_getAll...';
EXEC('CREATE procedure [dbo].[sp_Group_getAll]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 

@Page int =1,
@Status int = -1,
@Limit int = 10,
@From datetime = null,
@To datetime = null 
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord, d.*, dbo.getFullName(d.ManagerId) as ManagerName   from  [Group] d  '';


if(@Token is not null )
begin

 set @where += '' and (d.Name like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Code like  N''''%'' + @Token +''%'''')'';
end;







if( @Status >-1 )
begin 

set @where += '' and d.IsActive =  @Status ''; 

end 


--if(@From is not null )
--begin 
--set @where += '' 
--and (  d.CreateAt  >= @fromDate )
-- ''; 
--end 


--if(@to is not null )
--begin 
--set @where += '' 
--and ( d.CreateAt  <= @toDate ) ''; 
--end 




set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int '';

EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status ,@userId = @userId

end')
PRINT '  âœ“ Procedure sp_Group_getAll created';
-- Stored Procedure: sp_group_getAllLead
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_group_getAllLead]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_group_getAllLead...';
    DROP PROCEDURE [dbo].[sp_group_getAllLead];
END

PRINT '  â†’ Creating procedure sp_group_getAllLead...';
EXEC('CREATE procedure [dbo].[sp_group_getAllLead]
(
  @groupLeadId  int  = -1
)
as
begin

begin 
select id, UserName, FullName from Employees where RoleCode in ( ''3'',''6'')

--if( @groupLeadId  < 0)

--end 
--else 
--begin 

--select id, UserName, FullName from Employees where RoleCode in ( ''3'',''6'') 

--id not in 
--( 
--select ManagerId from [Group] where  ISNULL(Deleted ,0) = 0  


--)
--union 

--select id, UserName, FullName from Employees where RoleCode in ( ''3'',''6'') and 

--id = @groupLeadId


--end 

 end


  end
 

 select * from Employees')
PRINT '  âœ“ Procedure sp_group_getAllLead created';
-- Stored Procedure: sp_group_getAllMember
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_group_getAllMember]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_group_getAllMember...';
    DROP PROCEDURE [dbo].[sp_group_getAllMember];
END

PRINT '  â†’ Creating procedure sp_group_getAllMember...';
EXEC('CREATE procedure [dbo].[sp_group_getAllMember]
(
  @groupId  int  = -1
)
as
begin 
    if(@groupId >0 )
	begin 
		select Id, GroupId, MemberId, Deleted, CreatedBy,CreateAt, UpdatedBy, UpdateAt,
		dbo.getFullName(MemberId) as MemberName
		from GroupMember where GroupId =@groupId
		and   ISNULL(Deleted,0) =0
	end 
	
	else 
	begin 

	begin 
		select Id, GroupId, MemberId, Deleted, CreatedBy,CreateAt, UpdatedBy, UpdateAt,
		dbo.getFullName(MemberId) as MemberName
		from GroupMember where ISNULL(Deleted,0) =0
	end 
	end

	

 end')
PRINT '  âœ“ Procedure sp_group_getAllMember created';
-- Stored Procedure: sp_group_getAllMemberNotGroup
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_group_getAllMemberNotGroup]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_group_getAllMemberNotGroup...';
    DROP PROCEDURE [dbo].[sp_group_getAllMemberNotGroup];
END

PRINT '  â†’ Creating procedure sp_group_getAllMemberNotGroup...';
EXEC('CREATE procedure [dbo].[sp_group_getAllMemberNotGroup]

as
begin
		select * from Employees where  id not in ( select e.MemberId  from GroupMember e where  ISNULl(e.Deleted,0) =0 )
		and RoleCode = 2
 end')
PRINT '  âœ“ Procedure sp_group_getAllMemberNotGroup created';
-- Stored Procedure: sp_group_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_group_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_group_insert...';
    DROP PROCEDURE [dbo].[sp_group_insert];
END

PRINT '  â†’ Creating procedure sp_group_insert...';
EXEC('CREATE procedure [dbo].[sp_group_insert]
(
		
		@Name nvarchar(50) null,
		@ManagerId int null,
		@CreatedBy int null,
		
		@Status int null,
		@CreateAt datetime = null, 
		@IsActive bit = null
)
as
begin

declare @maxId  int =0 ;
 select
@maxId = max(id)
from [Group]
if(@maxId is null )
set @maxId =0

set @maxId = @maxId +1;
declare @tempUser  varchar(6);
if(@maxId < 10)
begin 
	set @tempUser = concat(''GR'', @maxId)
end 
else if(@maxId < 100)
begin 
	set @tempUser = concat(''GR0'', @maxId)
end 
else if(@maxId < 1000)
begin 
	set @tempUser = concat(''GR'', @maxId)
end 




INSERT INTO [dbo].[Group]
           ([Code]
           ,[Name]
           ,[ManagerId]
           ,[Status]
           ,[Deleted]
           ,[IsActive]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt])
     VALUES(  
			@tempUser ,
			@Name,
			@ManagerId,
			@Status,
			0,
			1,
		@CreatedBy,
		@CreatedBy,
		getdate(),
		getdate())
 end')
PRINT '  âœ“ Procedure sp_group_insert created';
-- Stored Procedure: sp_group_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_group_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_group_update...';
    DROP PROCEDURE [dbo].[sp_group_update];
END

PRINT '  â†’ Creating procedure sp_group_update...';
EXEC('CREATE procedure [dbo].[sp_group_update]
(
	    @id int null,
		@Name nvarchar(50) null,
		@ManagerId int null,
		@UpdatedBy int null,
		@Status int null,
	
		@IsActive bit = null
)
as
begin

update [Group] set  [name] = @Name, ManagerId= @ManagerId, Status = @Status,
IsActive = @IsActive ,
UpdateAt = GETDATE(),
UpdatedBy = @UpdatedBy
where id = @id


 end')
PRINT '  âœ“ Procedure sp_group_update created';
-- Stored Procedure: sp_groupEmp_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_groupEmp_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_groupEmp_insert...';
    DROP PROCEDURE [dbo].[sp_groupEmp_insert];
END

PRINT '  â†’ Creating procedure sp_groupEmp_insert...';
EXEC('CREATE procedure [dbo].[sp_groupEmp_insert]
(
		
	
		@MemberId int null,
		@GroupId int null,
		@CreatedBy int null,
		@CreateAt datetime = null
	
)
as
begin

INSERT INTO [dbo].[GroupMember]
           ([GroupId]
           ,[MemberId]
           ,[Deleted]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt])
     VALUES
           ( @GroupId,@MemberId, 0, @CreatedBy, @CreatedBy,getdate(),getdate())


end')
PRINT '  âœ“ Procedure sp_groupEmp_insert created';
-- Stored Procedure: sp_hdldItem_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_hdldItem_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_hdldItem_getAll...';
    DROP PROCEDURE [dbo].[sp_hdldItem_getAll];
END

PRINT '  â†’ Creating procedure sp_hdldItem_getAll...';
EXEC('CREATE procedure [dbo].[sp_hdldItem_getAll]
(
	@userid varchar(8) = ''''
)
as
begin
			select top 1 *  from hdldItem where userId  = @userid order by id desc
end')
PRINT '  âœ“ Procedure sp_hdldItem_getAll created';
-- Stored Procedure: sp_hdldItem_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_hdldItem_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_hdldItem_insert...';
    DROP PROCEDURE [dbo].[sp_hdldItem_insert];
END

PRINT '  â†’ Creating procedure sp_hdldItem_insert...';
EXEC('CREATE PROCEDURE [dbo].[sp_hdldItem_insert]
    @NoAgree NVARCHAR(255) = NULL,
    @Start DATETIME = NULL,
    @End DATETIME = NULL,
    @CodeId NVARCHAR(255) = NULL,
    @UserId NVARCHAR(50) = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[hdldItem] ([NoAgree],[Start],[End],[CodeId],[UserId],[CreatedBy])
    VALUES (@NoAgree,@Start,@End,@CodeId,@UserId,@CreatedBy);
END')
PRINT '  âœ“ Procedure sp_hdldItem_insert created';
-- Stored Procedure: sp_hdldItem_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_hdldItem_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_hdldItem_update...';
    DROP PROCEDURE [dbo].[sp_hdldItem_update];
END

PRINT '  â†’ Creating procedure sp_hdldItem_update...';
EXEC('CREATE PROCEDURE [dbo].[sp_hdldItem_update]
    @Id INT,
    @NoAgree NVARCHAR(255) = NULL,
    @Start DATETIME = NULL,
    @End DATETIME = NULL,
    @CodeId NVARCHAR(255) = NULL,
    @UserId NVARCHAR(50) = NULL,
    @UpdatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[hdldItem]
    SET [NoAgree] = @NoAgree,
        [Start] = @Start,
        [End] = @End,
        [CodeId] = @CodeId,
        [UserId] = @UserId,
        [UpdatedBy] = @UpdatedBy
    WHERE [Id] = @Id;
END')
PRINT '  âœ“ Procedure sp_hdldItem_update created';
-- Stored Procedure: sp_Impact_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Impact_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Impact_getAll...';
    DROP PROCEDURE [dbo].[sp_Impact_getAll];
END

PRINT '  â†’ Creating procedure sp_Impact_getAll...';
EXEC('CREATE procedure [dbo].[sp_Impact_getAll]
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
@marketting int = 0
)
as
begin

declare @where  nvarchar(max) = '' where  1=1 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord,  d.CreateAt, d.id, 

d.OrderCode, dbo.getFullName(d.CreatedBy) as authorName,dbo.getDisplayMasterDataById(d.NewStatus) as statusName, 
d.NewStatus,d.DateFrom, d.TxtTimer, d.TxtPlace
from OrderImpactHIstory  d inner join [Order] e on d.OrderCode = e.Code '';

if( @Status >-1 )
begin 

set @where += '' and d.NewStatus =  @Status ''; 

end 
if( @userId > 0 )
begin 
	set @where += '' and e.Assignee in ( select id  from  getAllUserByUserId(@userId)) 
	''; 
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

set @where +='' order by d.CreateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int '';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId


end')
PRINT '  âœ“ Procedure sp_Impact_getAll created';
-- Stored Procedure: sp_Impact_getAllMarketting
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Impact_getAllMarketting]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Impact_getAllMarketting...';
    DROP PROCEDURE [dbo].[sp_Impact_getAllMarketting];
END

PRINT '  â†’ Creating procedure sp_Impact_getAllMarketting...';
EXEC('CREATE procedure [dbo].[sp_Impact_getAllMarketting]
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
@marketting int = 0
)
as
begin

declare @where  nvarchar(max) = '' where  1=1 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord,  d.CreateAt, d.id, d.OrderCode, dbo.getFullName(d.CreatedBy) as authorName,dbo.getDisplayMasterDataById(d.NewStatus) as statusName, d.NewStatus,d.DateFrom, d.TxtTimer, d.TxtPlace
from OrderImpactHIstory  d inner join [Order] e on d.OrderCode = e.Code '';

if( @Status >-1 )
begin 

set @where += '' and d.NewStatus =  @Status ''; 

end 
if( @userId > 0 )
begin 
	set @where += '' and e.CreatedBy in ( select id  from  markettingGetAllViewId(@userId)) 
	''; 
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

set @where +='' order by d.CreateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int '';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId

end')
PRINT '  âœ“ Procedure sp_Impact_getAllMarketting created';
-- Stored Procedure: sp_jobItem_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_jobItem_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_jobItem_getAll...';
    DROP PROCEDURE [dbo].[sp_jobItem_getAll];
END

PRINT '  â†’ Creating procedure sp_jobItem_getAll...';
EXEC('CREATE procedure [dbo].[sp_jobItem_getAll]
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
@To datetime = null 
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord, d.*,
dbo.getDisplayMasterDataById(d.Field) as FieldText, 
dbo.getDisplayMasterData(d.CareerId) as CareerIdText
from  JobItem d  '';


if(@Token is not null )
begin

 set @where += '' and (d.Code like  N''''%'' + @Token +''%'''''';


 set @where += '' or d.Name like  N''''%'' + @Token +''%'''')'';
end;

if(@userId > 0)
begin 
set @where += '' and d.id not in ( '' +  cast (@userId as varchar(5)) + '' ) ''; 
end 

if( @Status >-1 )
begin 

set @where += '' and d.IsActive =  @Status ''; 

end 

--if(@From is not null )
--begin 
--set @where += '' 
--and (  d.CreateAt  >= @fromDate )
-- ''; 
--end 


--if(@to is not null )
--begin 
--set @where += '' 
--and ( d.CreateAt  <= @toDate ) ''; 
--end 


--if( @userId > 0 )
--begin 
--	set @where += '' and d.CreatedBy in ( select id  from  getAllUserByUserId(@userId)) 
--	''; 
--end 



set @where +='' order by d.UpdateAt desc''; 
--set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int '';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId

end')
PRINT '  âœ“ Procedure sp_jobItem_getAll created';
-- Stored Procedure: sp_jobItem_getAll2
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_jobItem_getAll2]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_jobItem_getAll2...';
    DROP PROCEDURE [dbo].[sp_jobItem_getAll2];
END

PRINT '  â†’ Creating procedure sp_jobItem_getAll2...';
EXEC('CREATE procedure [dbo].[sp_jobItem_getAll2]
(
@PartnerId int =-1,
@ProjectId int =-1,
@LinhvucId int =-1
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);

set @mainClause = ''  select d.Name, d.id, d.Code
from  JobItem d  '';

if(@PartnerId > 0)
begin 
set @where += ''and d.PartnerId = @PartnerId ''; 
end 

if( @ProjectId >0 )
begin 

set @where += ''and d.ProjectId = @ProjectId ''; 

end 

if( @LinhvucId >0 )
begin 

set @where += ''and d.Field = @LinhvucId ''; 

end 

set @where +='' order by d.UpdateAt desc''; 
set @mainClause = @mainClause +  @where
set @params =N'' @PartnerId int , @ProjectId int, @LinhvucId int '';


EXECUTE sp_executesql @mainClause,@params, @PartnerId = @PartnerId, @ProjectId = @ProjectId,
@LinhvucId = @LinhvucId
end')
PRINT '  âœ“ Procedure sp_jobItem_getAll2 created';
-- Stored Procedure: sp_jobItem_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_jobItem_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_jobItem_insert...';
    DROP PROCEDURE [dbo].[sp_jobItem_insert];
END

PRINT '  â†’ Creating procedure sp_jobItem_insert...';
EXEC('CREATE procedure [dbo].[sp_jobItem_insert]
(
		@Name nvarchar(100) null,
		@Field int  null,
		@CareerId int null,
		@Content ntext  NULL,
		@ShortDes  nvarchar(300) NULL,
		@Noted  nvarchar(300) NULL, 
		@Status int null, 	
		@CreatedBy varchar(5) null,
		@IsActive bit = null,
		@WarrantyDate int  =0,
		@Inputfile varchar(300) = null,
		@PartnerId int =null,
		@ProjectId int =null
		
)
as
begin

declare @maxId  int =0 ;
select
@maxId = max(id)
from JobItem
if(@maxId is null) 
set @maxId =0;

set @maxId = @maxId +1;
declare @tempUser  varchar(8);


if(@maxId < 10)
begin 
	set @tempUser = concat(''JOB0000'', @maxId)
end 
if(@maxId < 100)
begin 
	set @tempUser = concat(''JOB000'', @maxId)
end 

if(@maxId < 1000)
begin 
	set @tempUser = concat(''JOB00'', @maxId)
end 
if(@maxId > 10000)
begin 
	set @tempUser = concat(''JOB0'', @maxId)
end 
if(@maxId > 100000)
begin 
	set @tempUser = concat(''JOB'', @maxId)
end 

declare @templead int;
set @templead = 0;


INSERT INTO [dbo].[JobItem]
           (
				[Code]
			   ,[Name]
			   ,[Field]
			   ,[CareerId]
			   ,[Content]
			   ,[ShortDes]
			   ,[Noted]
			   ,[Status]
			   ,[Deleted]
			   ,[IsActive]
			   ,[CreatedBy]
			   ,[UpdatedBy]
			   ,[CreateAt]
			   ,[UpdateAt],
			   WarrantyDate,
			   Inputfile,
			   PartnerId,
			   ProjectId 

		   )

	
     VALUES
	 (
		@tempUser, @Name,@Field, @CareerId,@Content,@ShortDes,@Noted,@Status,0,@IsActive,@CreatedBy,@CreatedBy,getdate(), getdate(),@WarrantyDate,
		@Inputfile ,    @PartnerId,@ProjectId 
	 )
end')
PRINT '  âœ“ Procedure sp_jobItem_insert created';
-- Stored Procedure: sp_jobItem_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_jobItem_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_jobItem_update...';
    DROP PROCEDURE [dbo].[sp_jobItem_update];
END

PRINT '  â†’ Creating procedure sp_jobItem_update...';
EXEC('CREATE procedure [dbo].[sp_jobItem_update]
(
		@Name nvarchar(100) null,
		@Field int  null,
		@CareerId int null,
		@Content ntext  NULL,
		@ShortDes  nvarchar(300) NULL,
		@Noted  nvarchar(300) NULL, 
		@Status int null, 	
		@UpdatedBy varchar(5) null,
		@IsActive bit = null,
		@id int null,
		@WarrantyDate int  =0,
		@Inputfile varchar(300) = null,
			@PartnerId int =null,
		@ProjectId int =null
)
as
begin
	   update jobItem 
		set
				[name] = @Name,
				Field = @Field,
				CareerId = @CareerId,
				Content = @Content,
				ShortDes = @ShortDes,
				Noted = @Noted,
				IsActive = @IsActive,
				WarrantyDate  = @WarrantyDate,
				Inputfile = @Inputfile,
				PartnerId = @PartnerId,
				ProjectId = @ProjectId,
				[Status] = @Status
		where 
		        id =@id 
end')
PRINT '  âœ“ Procedure sp_jobItem_update created';
-- Stored Procedure: sp_masterData_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_masterData_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_masterData_getAll...';
    DROP PROCEDURE [dbo].[sp_masterData_getAll];
END

PRINT '  â†’ Creating procedure sp_masterData_getAll...';
EXEC('CREATE procedure [dbo].[sp_masterData_getAll]
(

@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 
@Type int =  -1,
@Page int =1,
@Status int = -1,
@Limit int = 10,
@From datetime = null,
@applyFor int = 2,
@To datetime = null 
)
as
begin

set @Limit = 200;
declare @where  nvarchar(max) = '' where  1= 1 and  isnull(Deleted, 0) = 0  '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord, d.* 
  from  [masterdata] d  '';


if(@Token is not null )
begin

 set @where += '' and (d.Name like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Code like  N''''%'' + @Token +''%'''')'';
end;







if( @Status >-1 )
begin 

set @where += '' and d.IsActive =  @Status ''; 

end 


if( @Type >-1 )
begin 

set @where += '' and d.TypeData =  @Type ''; 

end 


if( @applyFor = 0  )
begin 

set @where += '' and  ( d.ApplyFor  in (0,2)) ''; 

end 

if( @applyFor = 1  )
begin 

set @where += '' and  ( d.ApplyFor  in (1,2)) ''; 

end 


set @where +='' order by d.CreateAt desc ''; 
--set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int , @Type int  '';

EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit, @Type = @Type,
@fromDate = @From,@toDate =@To, @Status = @Status ,@userId = @userId

end')
PRINT '  âœ“ Procedure sp_masterData_getAll created';
-- Stored Procedure: sp_masterData_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_masterData_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_masterData_insert...';
    DROP PROCEDURE [dbo].[sp_masterData_insert];
END

PRINT '  â†’ Creating procedure sp_masterData_insert...';
EXEC('CREATE procedure [dbo].[sp_masterData_insert]
(
		@Name nvarchar(50) null,
		@Noted nvarchar(300) null, 
		@IsActive int  = 1 ,
		@TypeData int =1,
		@Extra varchar(10) =''red'',
		@ApplyFor int  = 2,
		@CreatedBy varchar(5) null
)
as
begin

declare @maxId  int =0 ;
 select
@maxId = max(id)
from masterdata 
where  TypeData = @TypeData
if(@maxId  is null )
set @maxId = 0;


set @maxId = @maxId +1;
declare @tempUser  varchar(6);


set @tempUser = concat(@TypeData, @maxId)

INSERT INTO [dbo].[MasterData]
           ([Code]
           ,[Name]
           ,[Noted]
           ,[Deleted]
           ,[IsActive]
           ,[CreatedBy]
           ,[TypeData]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt],
		   Extra,
		   ApplyFor
			
		   
		   )
     VALUES
           (

		   @tempUser, @Name, @Noted,0,@IsActive,@CreatedBy, @TypeData,@CreatedBy, getdate(),getdate(),@Extra, @ApplyFor
		   )

end')
PRINT '  âœ“ Procedure sp_masterData_insert created';
-- Stored Procedure: sp_masterData_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_masterData_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_masterData_update...';
    DROP PROCEDURE [dbo].[sp_masterData_update];
END

PRINT '  â†’ Creating procedure sp_masterData_update...';
EXEC('CREATE procedure [dbo].[sp_masterData_update]
(
		@Name nvarchar(50) null,
		@Noted nvarchar(300) null, 
		@IsActive int  = 1 ,
		@Extra varchar(10) =''red'',
		@id int =  -1,
		@ApplyFor int  = 2,
		@UpdatedBy varchar(5) null
)
as
begin
  update masterdata
  set 
  [Name] = @Name,
  noted = @Noted,
  ApplyFor= @ApplyFor,
  IsActive = @IsActive,
Extra = @Extra
  where id = @id
end')
PRINT '  âœ“ Procedure sp_masterData_update created';
-- Stored Procedure: sp_onboard_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_onboard_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_onboard_getAll...';
    DROP PROCEDURE [dbo].[sp_onboard_getAll];
END

PRINT '  â†’ Creating procedure sp_onboard_getAll...';
EXEC('CREATE procedure [dbo].[sp_onboard_getAll]
(
	@userId int = null, 
	@Limit int = 100,
	@MemberId int = null,
	@GroupId int =null,
	@Page int =1,
	@Status int = -1,
	@From datetime = null,
	@To datetime = null,
	@Job int =-1
)
as
begin
declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select 
count(d.id) over() as TotalRecord,
dbo.getFullInfo(d.CandidateId) as candidateName,
	d.OnboardDate as  OnboardDate,
	OrderCode as code,
	dbo.getJobTitle(d.JobId) as jobName,
	dbo.getNamePartner(d.partnerId) as PartnerName ,
	dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
	dbo.getFullName(d.Assignee ) as       AssigeeName,
	dbo.getOrderIdByCode(OrderCode) as OrderId,

	dbo.getDisplayMasterDataById(d.Status) as StatusText,
	dbo.getDisplayMasterDataById(d.SystemStatus) as SystemStatusText,
	dbo.getUserName( d.CreatedBy) as AuthorName ,d .*
	
	from  [OnboardMember] d  '';

if( @userId > 0 )
begin 
	set @where += '' and d.Assignee in ( select id  from  getAllUserByUserId(@userId))''; 
end 

if(@GroupId >0 )
begin 
   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.Assignee  = @MemberId ''; 
end 

if( @Status >-1 )
begin 

set @where += '' and d.result =  @Status ''; 

end 
if( @Job >-1 )
begin 

set @where += '' and d.JobId =  @Job ''; 

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
set @params =N'' @offset int,
@limit int, 
@Page int, 
@fromDate datetime,@toDate datetime ,
@Status int , 
@userId int,
@Job int, 
@GroupId int,
@MemberId int '';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,@GroupId = @GroupId, @MemberId = @MemberId,
@Page = @Page,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId, @Job = @Job


end')
PRINT '  âœ“ Procedure sp_onboard_getAll created';
-- Stored Procedure: sp_onboard_getAllMarketting
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_onboard_getAllMarketting]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_onboard_getAllMarketting...';
    DROP PROCEDURE [dbo].[sp_onboard_getAllMarketting];
END

PRINT '  â†’ Creating procedure sp_onboard_getAllMarketting...';
EXEC('CREATE procedure [dbo].[sp_onboard_getAllMarketting]
(
	@userId int = null, 
	@Limit int = 100,
	@MemberId int = null,
	@GroupId int =null,
	@Page int =1,
	@Status int = -1,
	@From datetime = null,
	@To datetime = null,
	@Job int =-1
)
as
begin
declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select 
count(d.id) over() as TotalRecord,
dbo.getFullInfo(d.CandidateId) as candidateName,
	d.OnboardDate as  OnboardDate,
	dbo.getJobTitle(d.JobId) as jobName,
		dbo.getNamePartner(d.partnerId) as PartnerName ,
	dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
	dbo.getFullName(d.Assignee ) as       AssigeeName,

	dbo.getDisplayMasterDataById(d.Status) as StatusText,
	dbo.getDisplayMasterDataById(d.SystemStatus) as SystemStatusText,
	dbo.getUserName( d.CreatedBy) as AuthorName ,d .*
	
	from  [OnboardMember] d  '';

if( @userId > 0 )
begin 
	set @where += '' and d.source > 0 ''; 
		set @where += '' and ( d.CreatedBy in ( select id  from  markettingGetAllViewId(@userId))) 
	''; 
end 



set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int,
@limit int, 
@Page int, 
@fromDate datetime,@toDate datetime ,
@Status int , 
@userId int,
@Job int, 
@GroupId int,
@MemberId int '';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,@GroupId = @GroupId, @MemberId = @MemberId,
@Page = @Page,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId, @Job = @Job



end')
PRINT '  âœ“ Procedure sp_onboard_getAllMarketting created';
-- Stored Procedure: sp_OnboardMember_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_OnboardMember_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_OnboardMember_insert...';
    DROP PROCEDURE [dbo].[sp_OnboardMember_insert];
END

PRINT '  â†’ Creating procedure sp_OnboardMember_insert...';
EXEC('CREATE procedure [dbo].[sp_OnboardMember_insert]
(
	@JobId int null,
	@Status varchar(100) null,
	@SystemStatus varchar(50) null,
	@CandidateId int  = 0,
	@PartnerId int =0, 
	@ProjectId int =  null,
	@OrderCode varchar(10) null, 
	@Source  int  =0,
	@CVLink varchar(500)  null,
	@ShortDes  varchar(500) null,
	@OnboardDate datetime null,
	@Assignee int  null, 
	@Noted nvarchar(500) null,
	@CreatedBy int null,
	@Dpd int  null, 
	@statusFollow int = @Status, 
	@result int = 1,
	@Warrantydate datetime null
	
)
as
begin
 
INSERT INTO  [dbo].[OnboardMember]
           (
		    [JobId]
           ,[Status]
           ,[SystemStatus]
           ,[CandidateId]
           ,[PartnerId]
           ,[ProjectId]
           ,[OrderCode]
           ,[Deleted]
           ,[Source]
           ,[ShortDes]
           ,[OnboardDate]
           ,[Assignee]
           ,[Noted]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt]
           ,[dob]
           ,[CVLink],
		   Dpd,
		   Warrantydate,
		   statusFollow,
		   result

		   )
     VALUES (@JobId ,@Status,@SystemStatus
           ,@CandidateId
           ,@PartnerId
           ,@ProjectId
           ,@OrderCode
           ,0
           ,@Source
           ,@ShortDes
           ,@OnboardDate
           ,@Assignee
           ,@Noted
         
           ,@CreatedBy,
		    @CreatedBy
           ,getdate()
           ,getdate()
           ,null
           ,@CVLink,
		   @Dpd,
		   @Warrantydate,
		   @Status,
		   0
		   )

end')
PRINT '  âœ“ Procedure sp_OnboardMember_insert created';
-- Stored Procedure: sp_OnboardMember_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_OnboardMember_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_OnboardMember_update...';
    DROP PROCEDURE [dbo].[sp_OnboardMember_update];
END

PRINT '  â†’ Creating procedure sp_OnboardMember_update...';
EXEC('CREATE procedure [dbo].[sp_OnboardMember_update]
(
	@id int,
	@result int = null,
	@OnboardDate datetime = null,
	@CreatedBy int = null

)
as
begin 

 if(@result = 1)
 begin 

		  declare @numberDay int;
			 declare @Warrantydate datetime;

			 set @numberDay =0;

			 select
			 @numberDay = e.Dpd ,
			 @Warrantydate = e.Warrantydate
			 from OnboardMember e where id = @id


			 set @Warrantydate =  DATEADD( dd,@numberDay,@OnboardDate)


		
			  update OnboardMember 
			  set     
			
					  result =@result,
					  OnboardDate = @OnboardDate,
					  Warrantydate = @Warrantydate,
					  UpdateAt = GETDATE()
			  where id = @id
 end 

 else 
 begin 
 
			  update OnboardMember 
			  set     
			
					  result =@result,
					 
					  UpdateAt = GETDATE()
			  where id = @id

 end 
	 
	

end')
PRINT '  âœ“ Procedure sp_OnboardMember_update created';
-- Stored Procedure: sp_order_Assingee
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_order_Assingee]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_order_Assingee...';
    DROP PROCEDURE [dbo].[sp_order_Assingee];
END

PRINT '  â†’ Creating procedure sp_order_Assingee...';
EXEC('CREATE procedure [dbo].[sp_order_Assingee]
(
	@CreatedBy int null,
	@Id int null,
	@Assignee int null
)
as
begin
	 declare @candidateId int;
	 set @candidateId =-1;

	 select  @candidateId = CandidateId  from [Order] where id = @Id
	   
      
	  update [Order] set 
			Assignee = @Assignee, 
			DateGet = getdate(),
			UpdatedBy = @CreatedBy,
			UpdateAt = getdate(),
			[Status] = 7,
			[Enable] = 1
	  where id = @Id

	  update Candidate  set Assignee = @Assignee where id = @candidateId

end')
PRINT '  âœ“ Procedure sp_order_Assingee created';
-- Stored Procedure: sp_order_changeResult
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_order_changeResult]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_order_changeResult...';
    DROP PROCEDURE [dbo].[sp_order_changeResult];
END

PRINT '  â†’ Creating procedure sp_order_changeResult...';
EXEC('CREATE procedure [dbo].[sp_order_changeResult]
(
	@CreatedBy int null,
	@Id int null,
	@result int null
)
as
begin
	 
	 
	 update [Order] set 
			result = @result,
			isClose = 1,
			UpdatedBy = @CreatedBy
	  where id = @Id

	  if( @result = 1)
	  begin 

		 update [Order] set 
			result = @result,
			isClose = 1,
			dateOnboard = getdate(),
			UpdatedBy = @CreatedBy
	  where id = @Id
	  end 


end')
PRINT '  âœ“ Procedure sp_order_changeResult created';
-- Stored Procedure: sp_Order_DashboardMarketting
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_DashboardMarketting]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_DashboardMarketting...';
    DROP PROCEDURE [dbo].[sp_Order_DashboardMarketting];
END

PRINT '  â†’ Creating procedure sp_Order_DashboardMarketting...';
EXEC('CREATE procedure [dbo].[sp_Order_DashboardMarketting]
(

@OrderBy varchar(30) = '''',
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 10,
@From datetime = null,
@To datetime = null,
@marketting int = 0
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord, 
dbo.getJobTitle( d.JobId) as PositionText ,
dbo.getFullInfo(d.CandidateId) as UserNameText, 
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getDisplayMasterDataById(d.Status) as StatusText,
dbo.getExtraInfoDataById(d.Status) as ExtraColor,
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getNamePartner(d.partnerId) as PartnerName ,
dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
dbo.getUserName( d.CreatedBy) as AuthorName ,
d.* from  [Order] d  '';




if( @Status >-1 )
begin 

set @where += '' and d.Status =  @Status ''; 

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




if( @userId > 0 )
begin 
	set @where += '' and d.Assignee in ( select id  from  markettingGetAllViewId(@userId)) ''; 


end 

set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int '';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId

end')
PRINT '  âœ“ Procedure sp_Order_DashboardMarketting created';
-- Stored Procedure: sp_Order_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_getAll...';
    DROP PROCEDURE [dbo].[sp_Order_getAll];
END

PRINT '  â†’ Creating procedure sp_Order_getAll...';
EXEC('CREATE procedure [dbo].[sp_Order_getAll]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 100,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1,
@Isapply int  =0,
@IsClose int = 0,
@IsReturn int =0,
@IsPush int =0,
@CTV bit = 0,

@DisplayAll bit  = 0

)
as
begin

if(@GroupId =38)
begin 
 set @GroupId = 7
end 

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0 
and isnull(d.Isapply,0) = @Isapply 
and  isnull(d.isClose,0) = @IsClose
and  isnull(d.IsReturn,0) = @IsReturn 
and isnull(d.ispush,0) = 0 '';

if( @DisplayAll = 1)
begin 
  set @where = '' where  isnull(d.Deleted,0) = 0  ''  
end 
else 
begin 
   set @where = '' where  isnull(d.Deleted,0) = 0 
and isnull(d.Isapply,0) = @Isapply 
and  isnull(d.isClose,0) = @IsClose
and  isnull(d.IsReturn,0) = @IsReturn 
and isnull(d.ispush,0) = 0  ''  
end



declare @mainClause nvarchar(max);
declare @params nvarchar(300);




declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord, 
dbo.getJobTitle( d.JobId) as PositionText ,
dbo.getFullInfo(d.CandidateId) as UserNameText, 
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getDisplayMasterDataById(d.Status) as StatusText,
dbo.getExtraInfoDataById(d.Status) as ExtraColor,


dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getNamePartner(d.partnerId) as PartnerName ,
dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
dbo.getUserName( d.CreatedBy) as AuthorName ,
d.* from  [Order] d  '';


if(@Token is not null )
begin

 set @where += '' and (d.Code like  N''''%'' + @Token +''%'''''';
 set @where += '' or dbo.getFullInfo(d.CandidateId) like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Code like  N''''%'' + @Token +''%'''')'';
end;





if( @Status >-1 )
begin 

set @where += '' and d.Status =  @Status ''; 

end 
if( @Job >-1 )
begin 

set @where += '' and d.JobId =  @Job ''; 

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





set @where += '' and  d.Assignee > 0  '';

if( @userId > 0 )
begin 
	set @where += '' and d.Assignee in ( select id  from  getAllUserByUserId(@userId))''; 
end 

if(@GroupId >0 )
begin 
   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.Assignee  = @MemberId ''; 
end 


set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @IsReturn int,  @limit int, @IsClose int,  @fromDate datetime,@toDate datetime, @Isapply int , @Status int , @userId int, @Job int, @GroupId int, @MemberId int '';

EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @Isapply = @Isapply,
@IsClose= @IsClose,
@IsReturn =  @IsReturn,
@limit = @limit,@GroupId = @GroupId, @MemberId = @MemberId,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId, @Job = @Job

end')
PRINT '  âœ“ Procedure sp_Order_getAll created';
-- Stored Procedure: sp_order_getAllHistory
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_order_getAllHistory]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_order_getAllHistory...';
    DROP PROCEDURE [dbo].[sp_order_getAllHistory];
END

PRINT '  â†’ Creating procedure sp_order_getAllHistory...';
EXEC('CREATE procedure [dbo].[sp_order_getAllHistory]
(
  @Phone varchar(10) null,
  @Email varchar(30) null
)
as
begin


declare @where  nvarchar(max) = '' where  1=1   '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
set @mainClause = '' 	select 
		dbo.getJobTitle( d.JobId) as PositionText ,
		dbo.getFullInfo(d.CandidateId) as UserNameText, 
		dbo.getFullName(d.Assignee ) as       AssigeeName,
		dbo.getDisplayMasterDataById(d.Status) as StatusText,
		dbo.getExtraInfoDataById(d.Status) as ExtraColor,
		dbo.getFullName(d.Assignee ) as       AssigeeName,
		dbo.getNamePartner(d.partnerId) as PartnerName ,
		dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
		dbo.getUserName( d.CreatedBy) as AuthorName, 
		d.*
		from  Candidate  ca
		inner join [order] d on ca.id = d.CandidateId  '';


if( @Phone is not null and
@Phone !='''' and 
@Email is not null and @Email !=''''  )
begin 

set @where += ''and  ( ca.phone =  @Phone   or ca.Email =  @Email ) ''; 

end
else if( @Phone is not null and
@Phone !='''')
begin

set @where += ''and  ca.phone =  @Phone ''; 
end 
else if( @Email is not null and
@Email !='''')
begin
set @where += ''and  ca.Email =  @Email ''; 
end 
else 
begin 
set @where += ''and  2= 1 ''; 
end 

set @where +='' order by d.Id desc''; 

set @mainClause = @mainClause +  @where

set @params =N'' @Phone varchar(10) , @Email varchar(30) '';


EXECUTE sp_executesql @mainClause,@params, @Phone = @Phone, @Email = @Email


end')
PRINT '  âœ“ Procedure sp_order_getAllHistory created';
-- Stored Procedure: sp_Order_getAllImpact
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_getAllImpact]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_getAllImpact...';
    DROP PROCEDURE [dbo].[sp_Order_getAllImpact];
END

PRINT '  â†’ Creating procedure sp_Order_getAllImpact...';
EXEC('CREATE procedure [dbo].[sp_Order_getAllImpact]
(
	@OrderCode varchar(8) = ''''
)
as
begin
declare @where  nvarchar(max) = '' where  1=1 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
set @mainClause = ''  select d.*, dbo.getFullName(d.CreatedBy) as CreatedByName,
dbo.getDisplayMasterDataById(NewStatus)  as StatusName 
from OrderImpactHIstory d  '';

if( @OrderCode != '''' )
begin 

set @where += '' and d.OrderCode =  @OrderCode ''; 


end 


set @where +='' order by d.id desc''; 
set @mainClause = @mainClause +  @where

set @params =N''@OrderCode varchar(8) '';


EXECUTE sp_executesql @mainClause,@params, @OrderCode = @OrderCode

end')
PRINT '  âœ“ Procedure sp_Order_getAllImpact created';
-- Stored Procedure: sp_Order_getAllLastest
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_getAllLastest]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_getAllLastest...';
    DROP PROCEDURE [dbo].[sp_Order_getAllLastest];
END

PRINT '  â†’ Creating procedure sp_Order_getAllLastest...';
EXEC('CREATE procedure [dbo].[sp_Order_getAllLastest]
(

@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 10,
@From datetime = null,
@To datetime = null,
@Job int =-1
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord, 
dbo.getJobTitle( d.JobId) as JobName ,
dbo.getFullInfo(d.CandidateId) as CandidateName, 

dbo.getDisplayMasterDataById(d.Status) as StatusText,
dbo.getExtraInfoDataById(d.Status) as ExtraColor,
d.* from  [Order] d  '';


if( @Status >-1 )
begin 

set @where += '' and d.Status =  @Status ''; 

end 

if( @Job >-1 )
begin 
set @where += '' and d.JobId =  @Job ''; 
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



if( @userId > 0 )
begin 
	set @where += '' and d.Assignee in ( select id  from  getAllUserByUserId(@userId)) 
	''; 
end 

if(@GroupId >0 )
begin 
   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.Assignee  = @MemberId ''; 
end 

set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int, @Job int,

@GroupId  int, @MemberId int 
'';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId, @Job = @Job,
@GroupId = @GroupId , @MemberId = @MemberId

end')
PRINT '  âœ“ Procedure sp_Order_getAllLastest created';
-- Stored Procedure: sp_Order_getAllLastestMarketting
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_getAllLastestMarketting]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_getAllLastestMarketting...';
    DROP PROCEDURE [dbo].[sp_Order_getAllLastestMarketting];
END

PRINT '  â†’ Creating procedure sp_Order_getAllLastestMarketting...';
EXEC('CREATE procedure [dbo].[sp_Order_getAllLastestMarketting]
(

@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 10,
@From datetime = null,
@To datetime = null,
@Job int =-1
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord, 
dbo.getJobTitle( d.JobId) as JobName ,
dbo.getFullInfo(d.CandidateId) as CandidateName, 
dbo.getDisplayMasterDataById(d.Status) as StatusText,
dbo.getExtraInfoDataById(d.Status) as ExtraColor,
d.* from  [Order] d  '';


if( @Status >-1 )
begin 

set @where += '' and d.Status =  @Status ''; 

end 


if( @Job >-1 )
begin 
set @where += '' and d.JobId =  @Job ''; 
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


if( @userId > 0 )
begin 
	set @where += '' and d.CreatedBy in ( select id  from  markettingGetAllViewId(@userId)) ''; 
end 



set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int, @Job int '';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId , @Job = @Job

end')
PRINT '  âœ“ Procedure sp_Order_getAllLastestMarketting created';
-- Stored Procedure: sp_Order_getAllReportStatus
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_getAllReportStatus]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_getAllReportStatus...';
    DROP PROCEDURE [dbo].[sp_Order_getAllReportStatus];
END

PRINT '  â†’ Creating procedure sp_Order_getAllReportStatus...';
EXEC('CREATE procedure [dbo].[sp_Order_getAllReportStatus]
(
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@From datetime = null,
@To datetime = null,
@marketting int = 0
)
as
begin

declare @where  nvarchar(max) = '' where  1=1 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;
set @mainClause = ''  
select  f.Name, f.id,
sum (case when e.Status is not null then 1 else 0 end)  as total
from MasterData f
left  join [order] e
on f.id = e.Status

'';
if( @userId > 0 )
begin 
	set @mainClause += '' and e.Assignee in ( select id  from  getAllUserByUserId(@userId)) 
	''; 
end 

if(@GroupId >0 )
begin 
   set @mainClause +=  '' and e.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @mainClause +=  '' and e.Assignee  = @MemberId ''; 
end 

if( @Status >-1 )
begin 

set @where += '' and f.Status =  @Status ''; 

end 


if(@From is not null )
begin 
set @where += '' 
and (  e.CreateAt  >= @fromDate )
 ''; 
end 


if(@to is not null )
begin 
set @where += '' 
and ( e.CreateAt  <= @toDate ) ''; 
end 



set @where +='' and f.TypeData = 4 
group by f.Id, f.Name
order by total desc ''; 

set @mainClause = @mainClause +  @where
set @params =N''   @fromDate datetime,@toDate datetime ,
@GroupId int, @MemberId int,  
@Status int , @userId int '';

EXECUTE sp_executesql @mainClause,@params,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId,
@GroupId = @GroupId, @MemberId = @MemberId

end')
PRINT '  âœ“ Procedure sp_Order_getAllReportStatus created';
-- Stored Procedure: sp_Order_getAllReportStatusDashboard
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_getAllReportStatusDashboard]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_getAllReportStatusDashboard...';
    DROP PROCEDURE [dbo].[sp_Order_getAllReportStatusDashboard];
END

PRINT '  â†’ Creating procedure sp_Order_getAllReportStatusDashboard...';
EXEC('CREATE procedure [dbo].[sp_Order_getAllReportStatusDashboard]
(
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Status int = -1,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1
)
as
begin

declare @where  nvarchar(max) = '' where  1=1 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;
set @mainClause = ''  
select  f.Name, f.id,
sum (case when e.Status is not null then 1 else 0 end)  as total
from MasterData f
left  join [order] e
on f.id = e.Status

'';
if( @userId >-1 )
begin 


set @mainClause += '' and e.CreatedBy in ( select id from markettingGetAllViewId(@userId)  ) ''; 

end 


if(@From is not null )
begin 
set @mainClause += '' 
and (  e.CreateAt  >= @fromDate )
 ''; 
end 


if(@to is not null )
begin 
set @mainClause += '' 
and ( e.CreateAt  <= @toDate ) ''; 
end 



if( @Job >-1 )
begin 
set @mainClause += '' and e.JobId =  @Job ''; 
end 

if( @Status >-1 )
begin 

set @mainClause += '' and e.Status =  @Status ''; 

end 


set @where +='' and f.TypeData = 4 and  f.id not in ( 0 ) 
group by f.Id, f.Name
order by total desc ''; 

set @mainClause = @mainClause +  @where
set @params =N''   @fromDate datetime,@toDate datetime , 
@Status int , @userId int , @Job int '';

EXECUTE sp_executesql @mainClause,@params,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId,
@Job = @Job

end')
PRINT '  âœ“ Procedure sp_Order_getAllReportStatusDashboard created';
-- Stored Procedure: sp_Order_getAllReportStatusDashboardNotMarketting
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_getAllReportStatusDashboardNotMarketting]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_getAllReportStatusDashboardNotMarketting...';
    DROP PROCEDURE [dbo].[sp_Order_getAllReportStatusDashboardNotMarketting];
END

PRINT '  â†’ Creating procedure sp_Order_getAllReportStatusDashboardNotMarketting...';
EXEC('CREATE procedure [dbo].[sp_Order_getAllReportStatusDashboardNotMarketting]
(
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Status int = -1,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1
)
as
begin

declare @where  nvarchar(max) = '' where  1=1 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;
set @mainClause = ''  
select  f.Name, f.id,
sum (case when e.Status is not null then 1 else 0 end)  as total
from MasterData f
left  join [order] e
on f.id = e.Status

'';
if( @userId >-1 )
begin 

set @mainClause += '' and ( e.Assignee  in ( select id  from  getAllUserByUserId(@userId)) ) ''; 

if(@GroupId >0 )
begin 
   set @mainClause +=  '' and e.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @mainClause +=  '' and e.Assignee  = @MemberId ''; 
end 

if(@From is not null )
begin 
set @mainClause += '' 
and (  e.CreateAt  >= @fromDate )
 ''; 
end 


if(@to is not null )
begin 
set @mainClause += '' 
and ( e.CreateAt  <= @toDate ) ''; 
end 


end 

if( @Status >-1 )
begin 

set @mainClause += '' and e.Status =  @Status ''; 

end 


if( @Job >-1 )
begin 
set @mainClause += '' and e.JobId =  @Job ''; 
end 






set @where +='' and f.TypeData = 4 
group by f.Id, f.Name
order by total desc ''; 

set @mainClause = @mainClause +  @where
set @params =N''   @fromDate datetime,@toDate datetime , 
@Status int , @userId int, @Job int, @GroupId int, @MemberId int '';

EXECUTE sp_executesql @mainClause,@params,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId,
 @GroupId = @GroupId, @MemberId = @MemberId,
@Job = @Job


end')
PRINT '  âœ“ Procedure sp_Order_getAllReportStatusDashboardNotMarketting created';
-- Stored Procedure: sp_Order_getAllReportStatusMarketting
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_getAllReportStatusMarketting]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_getAllReportStatusMarketting...';
    DROP PROCEDURE [dbo].[sp_Order_getAllReportStatusMarketting];
END

PRINT '  â†’ Creating procedure sp_Order_getAllReportStatusMarketting...';
EXEC('CREATE procedure [dbo].[sp_Order_getAllReportStatusMarketting]
(
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@From datetime = null,
@To datetime = null,
@marketting int = 0
)
as
begin

declare @where  nvarchar(max) = '' where  1=1 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;
set @mainClause = ''  
select  f.Name, f.id,
sum (case when e.Status is not null then 1 else 0 end)  as total
from MasterData f
left  join [order] e
on f.id = e.Status





'';

if( @userId >-1 )
begin 

set @mainClause += '' and e.CreatedBy in ( select id  from  markettingGetAllViewId(@userId))   ''; 

end 
	 

if( @Status >-1 )
begin 

set @where += '' and e.Status =  @Status ''; 

end 


if(@From is not null )
begin 
set @where += '' 
and (  f.CreateAt  >= @fromDate )
 ''; 
end 


if(@to is not null )
begin 
set @where += '' 
and ( f.CreateAt  <= @toDate ) ''; 
end 

set @where +='' and f.TypeData = 4 
group by f.Id, f.Name
order by total desc ''; 

set @mainClause = @mainClause +  @where
set @params =N''   @fromDate datetime,@toDate datetime , @Status int , @userId int '';

EXECUTE sp_executesql @mainClause,@params,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId

end')
PRINT '  âœ“ Procedure sp_Order_getAllReportStatusMarketting created';
-- Stored Procedure: sp_Order_getAllResult
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_getAllResult]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_getAllResult...';
    DROP PROCEDURE [dbo].[sp_Order_getAllResult];
END

PRINT '  â†’ Creating procedure sp_Order_getAllResult...';
EXEC('CREATE procedure [dbo].[sp_Order_getAllResult]
(
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Status int = -1,
@From datetime = null,
@To datetime = null,
@marketting int = 0
)
as
begin

declare @where  nvarchar(max) = '' where  1=1 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;
set @mainClause = '' select * from [Order] '';

if( @Status >-1 )
begin 

set @where += '' and f.Status =  @Status ''; 

end 

if( @userId >-1 )
begin 

set @where += '' and e.CreatedBy in  ( 58)  ''; 

end 

if(@From is not null )
begin 
set @where += '' 
and (  f.CreateAt  >= @fromDate )
 ''; 
end 


if(@to is not null )
begin 
set @where += '' 
and ( f.CreateAt  <= @toDate ) ''; 
end 



set @mainClause = @mainClause +  @where
set @params =N'' @fromDate datetime,@toDate datetime , 
@Status int , @userId int '';

EXECUTE sp_executesql @mainClause,@params,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId

end')
PRINT '  âœ“ Procedure sp_Order_getAllResult created';
-- Stored Procedure: sp_Order_getAllTracking
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_getAllTracking]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_getAllTracking...';
    DROP PROCEDURE [dbo].[sp_Order_getAllTracking];
END

PRINT '  â†’ Creating procedure sp_Order_getAllTracking...';
EXEC('CREATE procedure [dbo].[sp_Order_getAllTracking]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 100,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1,
@Isapply int  =1,
@IsReturn int  = 0,
@IsClose int = 0,
@DisplayAll int =1,
@IsPush int =0,
@CTV bit = 0
)
as
begin
declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0  '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;
set @Limit =1000;

declare @roleId int = 0;
select @roleId = RoleCode from Employees where id = @userId 




set @mainClause = ''  select count(d.id) over() as TotalRecord, 
dbo.getJobTitle( d.JobId) as PositionText ,
dbo.getFullInfo(d.CandidateId) as UserNameText, 
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getDisplayMasterDataById(d.Status) as StatusText,
dbo.getExtraInfoDataById(d.Status) as ExtraColor,
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getNamePartner(d.partnerId) as PartnerName ,
dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
dbo.getUserName( d.CreatedBy) as AuthorName ,
d.* from  [Order] d  '';


if( @Status >-1 )
begin 

set @where += '' and d.Status =  @Status ''; 

end 

if( @Job >-1 )
begin 

	set @where += '' and d.JobId =  @Job ''; 

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


if(@roleId = 6 or @roleId = 7)
begin 
	set @where += '' and    d.source > 0  and d.ispush = 0  ''; 
end
else 
begin 
set @where += '' and  d.Assignee > 0  and  d.source > 0   ''; 
end




set @where += '' and d.CreatedBy in ( select id  from  markettingGetAllViewId(@userId)) '';

set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int, 
@Job int ,  @Isapply int 
'';

EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId, @Job = @Job,
@Isapply= @Isapply

select @mainClause
end')
PRINT '  âœ“ Procedure sp_Order_getAllTracking created';
-- Stored Procedure: sp_Order_GetCountSource
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_GetCountSource]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_GetCountSource...';
    DROP PROCEDURE [dbo].[sp_Order_GetCountSource];
END

PRINT '  â†’ Creating procedure sp_Order_GetCountSource...';
EXEC('CREATE procedure [dbo].[sp_Order_GetCountSource]

as
begin
   select COUNT(id) as Total  from [order] d where d.Assignee is null  and ISNULL( d.ispush,0)=0 and ISNULL( d.Deleted,0)=0

   and d.CreatedBy in ( select id from Employees where RoleCode =4)
end')
PRINT '  âœ“ Procedure sp_Order_GetCountSource created';
-- Stored Procedure: sp_Order_GetCountSourceCTV
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_GetCountSourceCTV]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_GetCountSourceCTV...';
    DROP PROCEDURE [dbo].[sp_Order_GetCountSourceCTV];
END

PRINT '  â†’ Creating procedure sp_Order_GetCountSourceCTV...';
EXEC('CREATE procedure [dbo].[sp_Order_GetCountSourceCTV]

as
begin
   select COUNT(id) as Total  from [order] d where d.Assignee is null  
   and d.CreatedBy in ( select id  from Employees where RoleCode in ( 6 ,7) )
   and ISNULL( d.ispush,0)=0 and ISNULL( d.Deleted,0)=0 
end')
PRINT '  âœ“ Procedure sp_Order_GetCountSourceCTV created';
-- Stored Procedure: sp_Order_groupByStatus
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_groupByStatus]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_groupByStatus...';
    DROP PROCEDURE [dbo].[sp_Order_groupByStatus];
END

PRINT '  â†’ Creating procedure sp_Order_groupByStatus...';
EXEC('CREATE procedure [dbo].[sp_Order_groupByStatus]
(

@userId int = null, 
@MemberId int = null,
@GroupId int =null,

@From datetime = null,
@To datetime = null 
)
as
begin

declare @where  nvarchar(max) = '' where  d.Status  in  (select id from MasterData e
where e.TypeData =4)  '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);




set @mainClause = '' select  count (id) as Total ,d.Status as statusiD, 
dbo.getDisplayMasterDataById(d.Status) as StatusName
from [order] d
  '';

if(@userId > 0)
begin 
set @where += '' and d.id not in ( '' +  cast (@userId as varchar(5)) + '' ) ''; 
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


if( @userId > 0 )
begin 
	set @where += '' and d.CreatedBy in ( select id  from  getAllUserByUserId(@userId)) ''; 
end 



set @where +='' 

group by d.Status ''; 

set @mainClause = @mainClause +  @where
set @params =N'' @fromDate datetime,@toDate datetime , @userId int '';


EXECUTE sp_executesql @mainClause,@params, 
@fromDate = @From,@toDate =@To, @userId = @userId

end')
PRINT '  âœ“ Procedure sp_Order_groupByStatus created';
-- Stored Procedure: sp_Order_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_insert...';
    DROP PROCEDURE [dbo].[sp_Order_insert];
END

PRINT '  â†’ Creating procedure sp_Order_insert...';
EXEC('CREATE procedure [dbo].[sp_Order_insert]
(
	@Status [int] NULL,
	@CandidateId [int] NULL,
	@partnerId [int] NULL,
	@JobId [int] NULL,
	@ProjectId [int] null,
	@ShortDes [nvarchar](500) NULL,
	@CVLink [varchar](500) NULL,
	@Noted [nvarchar](500) NULL,
	@CreatedBy [int] NULL,
	@Source int  = 0,
	@DateGet datetime null,
	@Enable bit null, 
	@Assignee int null
)
as
begin

declare @maxId  int =0 ;
 select
@maxId = max(id)
from [Order]

if(@maxId is null)
begin 
	set @maxId =0;
end
set @maxId = @maxId +1;
declare @tempUser  varchar(6);
if(@maxId < 10)
begin 
	set @tempUser = concat(''MD000'', @maxId)
end 
else if(@maxId < 100)
begin 
	set @tempUser = concat(''MD00'', @maxId)
end 

else if(@maxId < 1000)
begin 
	set @tempUser = concat(''MD0'', @maxId)
end 
else 
begin 
	set @tempUser = concat(''MD'', @maxId)
end 

declare @templead int;
set @templead = 0;

select @templead = id from Employees where  RoleCode =3 and  id = @CreatedBy

declare @statusInput int;
set @statusInput = 7;


if(@Source = 0)
begin 
	set  @DateGet = getdate();
	set @Assignee = @CreatedBy;
	set @Enable =  1;

end 

else 
begin 
	set @statusInput = 46;
end 

 INSERT INTO [dbo].[Order]
           (
		   partnerId,
		   ProjectId,
		    [Code]
           ,[Status]
           ,[CandidateId]
           ,[JobId]
           ,[ShortDes]
           ,[CVLink]
           ,[Noted]
           ,[Deleted]
           ,[IsActive]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt],
		   source ,
		   DateGet ,
		   [Enable],
		   Assignee
		   )
VALUES
(
 @partnerId,@ProjectId, @tempUser,@statusInput,@CandidateId,@JobId,@ShortDes, @CVLink,@Noted,0,1,@CreatedBy,@CreatedBy,GETDATE(),getdate(),
 @Source,
 @DateGet, @Enable, @Assignee
		   
)

insert into OrderImpactHIstory( OrderCode,NewStatus, Noted,Deleted, IsActive,CreatedBy, 
CreateAt) values ( @tempUser, @statusInput, N''Khởi tạo đơn hàng'',0,1,@CreatedBy,getdate())
end')
PRINT '  âœ“ Procedure sp_Order_insert created';
-- Stored Procedure: sp_Order_MarkettingGetAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_MarkettingGetAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_MarkettingGetAll...';
    DROP PROCEDURE [dbo].[sp_Order_MarkettingGetAll];
END

PRINT '  â†’ Creating procedure sp_Order_MarkettingGetAll...';
EXEC('CREATE procedure [dbo].[sp_Order_MarkettingGetAll]
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
@marketting int = 0,
@Isapply int  =0,
@Job int =-1,
@DisplayAll int = 0,
@IsClose int = 0,
@IsReturn int =0,
@IsPush int =0,
@CTV bit = 0
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0  '';

if(@IsPush =0)
begin 
		set @where +='' and  isnull(d.ispush,0) = 0 '';
end 
else 

begin 
	set @where +='' and  isnull(d.ispush,0) = 1 '';
end 

declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord, 
dbo.getJobTitle( d.JobId) as PositionText ,
dbo.getFullInfo(d.CandidateId) as UserNameText, 
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getDisplayMasterDataById(d.Status) as StatusText,
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getExtraInfoDataById(d.Status) as ExtraColor,
dbo.getNamePartner(d.partnerId) as PartnerName ,
dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
dbo.getUserName( d.CreatedBy) as AuthorName ,
dbo.getFullNameSorce ( d.CreatedBy) as SourceName ,
d.* from  [Order] d  '';


if(@Token is not null )
begin

 set @where += '' and (d.Code like  N''''%'' + @Token +''%'''''';
 set @where += '' or dbo.getFullInfo(d.CandidateId) like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Code like  N''''%'' + @Token +''%'''')'';
end;





if( @Status >-1 )
begin 

set @where += '' and d.Status =  @Status ''; 

end 
if( @Job >-1 )
begin 

set @where += '' and d.JobId =  @Job ''; 

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

if( @marketting = 1 )
begin 
	set @where += '' and  isnull(d.Assignee,0)  < 1  and d.source > 0   '';
	set @where += '' and d.CreatedBy in ( select id  from  markettingGetAllViewId(@userId)) '';

end

if( @CTV = 1)
begin 
	set @where += '' and  isnull(d.Assignee,0)  < 1  and d.source > 0   '';
	set @where += '' and d.CreatedBy in ( select id  from  Employees e where RoleCode in (6,7)) and d.isPush =0  '';
end 





set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where


set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int,
@Job int
'';


EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId , @Job= @Job




end')
PRINT '  âœ“ Procedure sp_Order_MarkettingGetAll created';
-- Stored Procedure: sp_order_PushCaseCTV
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_order_PushCaseCTV]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_order_PushCaseCTV...';
    DROP PROCEDURE [dbo].[sp_order_PushCaseCTV];
END

PRINT '  â†’ Creating procedure sp_order_PushCaseCTV...';
EXEC('CREATE procedure [dbo].[sp_order_PushCaseCTV]
(
	@CreatedBy int null,
	@Id int null,
	@result int null
)
as
begin
	 
	 
			 update [Order] set 
			ispush=0,
			UpdatedBy = @CreatedBy
			where id = @Id
end')
PRINT '  âœ“ Procedure sp_order_PushCaseCTV created';
-- Stored Procedure: sp_order_returnOrder
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_order_returnOrder]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_order_returnOrder...';
    DROP PROCEDURE [dbo].[sp_order_returnOrder];
END

PRINT '  â†’ Creating procedure sp_order_returnOrder...';
EXEC('CREATE procedure [dbo].[sp_order_returnOrder]
(
	@CreatedBy int null,
	@Id int null,
	@result int null
)
as
begin
	 
	 
			 update [Order] set 
			[Status] =30,
			isClose = 0,
			result =0,
			isReturn = 1,
			Isapply = 0,
			UpdatedBy = @CreatedBy
			where id = @Id
end')
PRINT '  âœ“ Procedure sp_order_returnOrder created';
-- Stored Procedure: sp_Order_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Order_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Order_update...';
    DROP PROCEDURE [dbo].[sp_Order_update];
END

PRINT '  â†’ Creating procedure sp_Order_update...';
EXEC('CREATE procedure [dbo].[sp_Order_update]
(
	@partnerId int null, 
	@ProjectId int null,
	@Status [int] NULL,

	@IsActive [int] NUll,
	@CandidateId [int] NULL,
	@JobId [int] NULL,
	@ShortDes [nvarchar](500) NULL,
	@CVLink [varchar](500) NULL,
	@Noted [nvarchar](500) NULL,
	@UpdatedBy [int] NULL,
	@id int NULL,
		@Source int  = 0
)
as
begin

update [order]

set   [Status] = @Status, 
CandidateId = @CandidateId,

JobId =@JobId,
partnerId= @partnerId,
CVLink = @CVLink,
ShortDes = @ShortDes,
Noted  = @Noted,  
UpdatedBy = @UpdatedBy, 
ProjectId  =@ProjectId,
UpdateAt = getdate() 

where id = @id
end')
PRINT '  âœ“ Procedure sp_Order_update created';
-- Stored Procedure: sp_OrderImpactHIstory_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_OrderImpactHIstory_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_OrderImpactHIstory_insert...';
    DROP PROCEDURE [dbo].[sp_OrderImpactHIstory_insert];
END

PRINT '  â†’ Creating procedure sp_OrderImpactHIstory_insert...';
EXEC('CREATE procedure [dbo].[sp_OrderImpactHIstory_insert]
(
	@OrderCode varchar(8) null, 
	@NewStatus [int] NULL,
	@Noted nvarchar(500) NULL,
	@TxtTimer nvarchar(100) NULL,
	@DateFrom datetime NULL,
	@TxtPlace nvarchar(200) NULL ,
	@CreatedBy int null
)
as
begin

 INSERT INTO [dbo].[OrderImpactHIstory]
           ([OrderCode]
           ,[ObjectInfo]
           ,[OldStatus]
           ,[NewStatus]
           ,[Noted]
           ,[Deleted]
           ,[IsActive]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt]
           ,[TxtTimer]
           ,[DateFrom]
           ,[TxtPlace])
     VALUES
           (
		   
		   @OrderCode, '''', '''',@NewStatus, @Noted,0,1,@CreatedBy,@CreatedBy,getdate(),getdate(), @TxtTimer,@DateFrom,
		   @TxtPlace
		   )

declare @idOrder int ;

select top 1 @idOrder = Id from [Order] where Code = @OrderCode

if(@idOrder > 0 and  @NewStatus > 0) 
begin 
		update  [Order]

		set 
			[Status] = @NewStatus,
			UpdateAt = getdate(),
			UpdatedBy =  @CreatedBy
		where id = @idOrder

end 



end')
PRINT '  âœ“ Procedure sp_OrderImpactHIstory_insert created';
-- Stored Procedure: sp_OrderImportSourceMarketting
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_OrderImportSourceMarketting]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_OrderImportSourceMarketting...';
    DROP PROCEDURE [dbo].[sp_OrderImportSourceMarketting];
END

PRINT '  â†’ Creating procedure sp_OrderImportSourceMarketting...';
EXEC('CREATE procedure [dbo].[sp_OrderImportSourceMarketting]
(
	@NameCandidate nvarchar( 50),
	@LinkDocument varchar(500) NULL,
	@DateAdd datetime  NULL,
	@PhoneNumber varchar(10) NULL,
	@EmailCan varchar(50),
	@SourceMarketting int NULL,
	@NotedIn [nvarchar](500) NULL,
	@JobId int NULL,
	@Assignee int NULL,
	@createBy int null
)
as
begin

declare @maxId  int =0 ;
set  @maxId = IDENT_CURRENT(''Candidate'');

set @maxId = @maxId +1;


declare @tempUser  varchar(6);

if(@maxId < 10)
begin 
	set @tempUser = concat(''CA000'', @maxId)
end 
if(@maxId < 100)
begin 
	set @tempUser = concat(''CA00'', @maxId)
end 

if(@maxId < 1000)
begin 
	set @tempUser = concat(''CA0'', @maxId)
end 
if(@maxId > 999)
begin 
	set @tempUser = concat(''CA'', @maxId)
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
		    Assignee
		   )
     VALUES
           (
		    @tempUser, @NameCandidate,@EmailCan,'''','''',@LinkDocument,@PhoneNumber,@NotedIn,
			0,0,1,@createBy,@createBy,getdate(),getdate(),null,@SourceMarketting,-1)

   declare @idCa  int;
   set @idCa =-1;
   select @idCa = SCOPE_IDENTITY() 



   declare @maxId1 int =0 ;

set  @maxId1 = IDENT_CURRENT(''[Order]'');


if(@maxId1 is null)
begin 
	set @maxId1 =0;
end
set @maxId1 = @maxId +1;
declare @tempUser1  varchar(6);
if(@maxId1 < 10)
begin 
	set @tempUser1 = concat(''OD000'', @maxId)
end 
if(@maxId1 < 100)
begin 
	set @tempUser1 = concat(''OD00'', @maxId)
end 

if(@maxId1 < 1000)
begin 
	set @tempUser1 = concat(''OD0'', @maxId)
end 
if(@maxId1 > 999)
begin 
	set @tempUser1 = concat(''OD'', @maxId)
end 
	

declare @partnerId1 int;
declare @ProjectId1 int;
select @partnerId1 = PartnerId, @ProjectId1 = ProjectId from JobItem where id = @JobId 



INSERT INTO [dbo].[Order]
           (
		    partnerId,
		    ProjectId,
		    [Code]
           ,[Status]
           ,[CandidateId]
           ,[JobId]
           ,[ShortDes]
           ,[CVLink]
           ,[Noted]
           ,[Deleted]
           ,[IsActive]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt],
		   source ,
		   DateGet ,
		   [Enable],
		   Assignee
		   )
VALUES
(
 @partnerId1,@ProjectId1,@tempUser1,46, @idCa, @JobId,@LinkDocument,'''',@NotedIn,
 0,1,@createBy,@createBy,getdate(), getdate(),@SourceMarketting,null, 0,-1
		   
)


end')
PRINT '  âœ“ Procedure sp_OrderImportSourceMarketting created';
-- Stored Procedure: sp_orderReport_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_orderReport_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_orderReport_getAll...';
    DROP PROCEDURE [dbo].[sp_orderReport_getAll];
END

PRINT '  â†’ Creating procedure sp_orderReport_getAll...';
EXEC('CREATE procedure [dbo].[sp_orderReport_getAll]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 
@MemberId int = null,
@GroupId int =null,
@Page int =1,
@Status int = -1,
@Limit int = 100,
@From datetime = null,
@To datetime = null,
@marketting int = 0,
@Job int =-1,
@Isapply int  =0,
@IsClose int = 0,
@IsReturn int =0 

)
as
begin

if(@GroupId =38)
begin 
 set @GroupId = 7
end 

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0 
and isnull(d.Isapply,0) = @Isapply 
and  isnull(d.isClose,0) = @IsClose
and  isnull(d.IsReturn,0) = @IsReturn 


'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);




declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord, 
dbo.getJobTitle( d.JobId) as PositionText ,
dbo.getFullInfo(d.CandidateId) as UserNameText, 
dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getDisplayMasterDataById(d.Status) as StatusText,
dbo.getExtraInfoDataById(d.Status) as ExtraColor,


dbo.getFullName(d.Assignee ) as       AssigeeName,
dbo.getNamePartner(d.partnerId) as PartnerName ,
dbo.getNameFromParrent(d.ProjectId) as ProjectName ,
dbo.getUserName( d.CreatedBy) as AuthorName ,
d.* from  [Order] d  '';


if(@Token is not null )
begin

 set @where += '' and (d.Code like  N''''%'' + @Token +''%'''''';
 set @where += '' or dbo.getFullInfo(d.CandidateId) like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Code like  N''''%'' + @Token +''%'''')'';
end;





if( @Status >-1 )
begin 

set @where += '' and d.Status =  @Status ''; 

end 
if( @Job >-1 )
begin 

set @where += '' and d.JobId =  @Job ''; 

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





set @where += '' and  d.Assignee > 0  '';

if( @userId > 0 )
begin 
	set @where += '' and d.Assignee in ( select id  from  getAllUserByUserId(@userId))''; 
end 

if(@GroupId >0 )
begin 
   set @where +=  '' and d.Assignee in ( select id  from  getAllMemberByGroupId(@GroupId))''; 
end 
if(@MemberId >0 )
begin 
   set @where +=  '' and d.Assignee  = @MemberId ''; 
end 


set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @IsReturn int,  @limit int, @IsClose int,  @fromDate datetime,@toDate datetime, @Isapply int , @Status int , @userId int, @Job int, @GroupId int, @MemberId int '';

EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @Isapply = @Isapply,
@IsClose= @IsClose,
@IsReturn =  @IsReturn,
@limit = @limit,@GroupId = @GroupId, @MemberId = @MemberId,
@fromDate = @From,@toDate =@To, @Status = @Status, @userId = @userId, @Job = @Job

end')
PRINT '  âœ“ Procedure sp_orderReport_getAll created';
-- Stored Procedure: sp_ParrentChild_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ParrentChild_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_ParrentChild_getAll...';
    DROP PROCEDURE [dbo].[sp_ParrentChild_getAll];
END

PRINT '  â†’ Creating procedure sp_ParrentChild_getAll...';
EXEC('CREATE procedure [dbo].[sp_ParrentChild_getAll]
(
  @Type int null,
  @Rel int null
)
as
begin
   select * from ParrentChild where [Type] = @Type and RelId = @Rel

end')
PRINT '  âœ“ Procedure sp_ParrentChild_getAll created';
-- Stored Procedure: sp_parrentChild_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_parrentChild_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_parrentChild_insert...';
    DROP PROCEDURE [dbo].[sp_parrentChild_insert];
END

PRINT '  â†’ Creating procedure sp_parrentChild_insert...';
EXEC('CREATE procedure [dbo].[sp_parrentChild_insert]
(
		@Text nvarchar(300) null,
		@RelId int null,
		@Type nvarchar(50) null
	
)
as
begin

INSERT INTO [dbo].[ParrentChild]
           (
			   [Text]
			   ,[RelId]
			   ,[Type] ,CreateAt
		   )
     VALUES
           (
				@Text,@RelId,@Type, getdate()	
		   
		   )

end')
PRINT '  âœ“ Procedure sp_parrentChild_insert created';
-- Stored Procedure: sp_parrentChild_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_parrentChild_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_parrentChild_update...';
    DROP PROCEDURE [dbo].[sp_parrentChild_update];
END

PRINT '  â†’ Creating procedure sp_parrentChild_update...';
EXEC('CREATE procedure [dbo].[sp_parrentChild_update]
(
		@Text nvarchar(300) null,
		@RelId int  null,
		@Type nvarchar(50) null,
		@id int null
	
)
as
begin

update  ParrentChild set [Text] = @Text  where id = @id

end')
PRINT '  âœ“ Procedure sp_parrentChild_update created';
-- Stored Procedure: sp_Partner_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Partner_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_Partner_getAll...';
    DROP PROCEDURE [dbo].[sp_Partner_getAll];
END

PRINT '  â†’ Creating procedure sp_Partner_getAll...';
EXEC('CREATE procedure [dbo].[sp_Partner_getAll]
(

@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@userId int = null, 

@Page int =1,
@Status int = -1,
@Limit int = 10,
@From datetime = null,
@To datetime = null 
)
as
begin
set @Limit = 100

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0'';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);



declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord, d.* 
  from  [Partner] d  '';


if(@Token is not null )
begin

 set @where += '' and (d.Name like  N''''%'' + @Token +''%'''''';
 set @where += '' or d.Code like  N''''%'' + @Token +''%'''')'';
end;







if( @Status >-1 )
begin 

set @where += '' and d.IsActive =  @Status ''; 

end 





set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime , @Status int , @userId int '';

EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @Status = @Status ,@userId = @userId

end')
PRINT '  âœ“ Procedure sp_Partner_getAll created';
-- Stored Procedure: sp_partner_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_partner_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_partner_insert...';
    DROP PROCEDURE [dbo].[sp_partner_insert];
END

PRINT '  â†’ Creating procedure sp_partner_insert...';
EXEC('CREATE procedure [dbo].[sp_partner_insert]
(
		@Name nvarchar(50) null,
		@ShortName nvarchar(50) null,
		@TaxCode nvarchar(50) null,
		@CreatedBy varchar(5) null
)
as
begin

declare @maxId  int =0 ;
 select
@maxId = max(id)
from [Partner]
set @maxId = @maxId +1;
if(@maxId is null)
begin 
  set @maxId =0;
end 
declare @tempUser  varchar(6);
if(@maxId < 10)
begin 
	set @tempUser = concat(''PA000'', @maxId)
end 
else if(@maxId < 100)
begin 
	set @tempUser = concat(''PA00'', @maxId)
end 

else if(@maxId < 1000)
begin 
	set @tempUser = concat(''PA0'', @maxId)
end 
else if(@maxId > 10000)
begin 
	set @tempUser = concat(''PA'', @maxId)
end 

INSERT INTO [dbo].[Partner]
           (
		   [Code]
           ,[Name]
           ,[Noted]
           ,[Deleted]
           ,[IsActive]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt],
		   TaxCode,
		   ShortName
		   
		   )
     VALUES
           (
		   @tempUser, @Name, '''',0,1,@CreatedBy,@CreatedBy, getdate(), getdate(),
		   @TaxCode,@ShortName
		   
		   
		   )

end')
PRINT '  âœ“ Procedure sp_partner_insert created';
-- Stored Procedure: sp_partner_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_partner_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_partner_update...';
    DROP PROCEDURE [dbo].[sp_partner_update];
END

PRINT '  â†’ Creating procedure sp_partner_update...';
EXEC('CREATE procedure [dbo].[sp_partner_update]
(
		@Name nvarchar(50) null,
		@Id int, 
		@ShortName nvarchar(50) null,
		@TaxCode nvarchar(50) null,
		@UpdatedBy varchar(5) null
)
as
begin

	update [dbo].[Partner]
	set Name   = @Name, UpdatedBy =@UpdatedBy ,
	ShortName = @ShortName, TaxCode = @TaxCode
	where id =@Id

end')
PRINT '  âœ“ Procedure sp_partner_update created';
-- Stored Procedure: sp_ProcessingCalTimeIndexModel_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ProcessingCalTimeIndexModel_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_ProcessingCalTimeIndexModel_getAll...';
    DROP PROCEDURE [dbo].[sp_ProcessingCalTimeIndexModel_getAll];
END

PRINT '  â†’ Creating procedure sp_ProcessingCalTimeIndexModel_getAll...';
EXEC('CREATE procedure [dbo].[sp_ProcessingCalTimeIndexModel_getAll]
(
@Token nvarchar(30),
@OrderBy varchar(30) = '''',
@userId int = null, 
@Page int =1,
@Limit int = 10,
@VendorId int =0,
@From datetime = null,
@TimeSelect datetime  =null,
@To datetime = null 
)
as
begin



declare @dayR int ;
declare @monthR int ;
declare @yearR int ;

set @dayR = day(@TimeSelect);
set @monthR = month(@TimeSelect);
set @yearR = year(@TimeSelect);
declare @where  nvarchar(max) = '' where  d.isCal =1 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);

declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = '' select count(d.id) as Total, sum(d.DurationReal) as Duration , sum (d.duration) as Billsec ,d.Disposition, d.LineCode from ReportTalkTime d     '';

if(@monthR  > 0 )
begin
	set @where += '' and MONTH(d.callDate)= '' +  cast (@monthR as varchar(5)) + '' ''; 
end

if(@yearR  > 0 )
begin
	set @where += ''and  YEAR(d.callDate)= '' +  cast (@yearR as varchar(5)) + '' ''; 
end
if(@dayR  > 0 )
begin
	set @where += ''and  DAY(d.callDate)= '' +  cast (@dayR as varchar(5)) + '' ''; 
end

set @where += ''
group by d.linecode, d.Disposition '';

set @where +='' order by d.linecode desc''; 


set @mainClause = @mainClause +  @where



set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime '';
EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To

end')
PRINT '  âœ“ Procedure sp_ProcessingCalTimeIndexModel_getAll created';
-- Stored Procedure: sp_RecordingFile_getAllByOrderId
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_RecordingFile_getAllByOrderId]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_RecordingFile_getAllByOrderId...';
    DROP PROCEDURE [dbo].[sp_RecordingFile_getAllByOrderId];
END

PRINT '  â†’ Creating procedure sp_RecordingFile_getAllByOrderId...';
EXEC('create procedure [dbo].[sp_RecordingFile_getAllByOrderId]
(
	@orderId  int 
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0 and d.DurationReal >0  '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;

set @mainClause = '' select count(d.id) over() as TotalRecord ,d.*,
d.FileRecording as Recordingfile, 
d.linecode as Src, 
d.PhoneLog as dst
from ReportTalkTime d  '';

declare @orderCode varchar(20);
set @orderCode ='''';

select top 1 @orderCode = Code from [Order]  where id = @orderId

if(@orderCode != '''' )
begin
	set @where += '' and d.NoAgree = @orderCode '' + '' ''; 
end
set @where +='' order by d.CreateAt desc''; 
set @mainClause = @mainClause +  @where
set @params =N'' @orderCode varchar(20)   '';
EXECUTE sp_executesql @mainClause,@params,  @orderCode = @orderCode


end')
PRINT '  âœ“ Procedure sp_RecordingFile_getAllByOrderId created';
-- Stored Procedure: sp_RecordingFile_getAllv3
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_RecordingFile_getAllv3]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_RecordingFile_getAllv3...';
    DROP PROCEDURE [dbo].[sp_RecordingFile_getAllv3];
END

PRINT '  â†’ Creating procedure sp_RecordingFile_getAllv3...';
EXEC('CREATE procedure [dbo].[sp_RecordingFile_getAllv3]
(
@PhoneLog varchar(20)  =null, 
@LineCode varchar(10) =null,
@Disposition  varchar(20) = null, 
@Token varchar(30) =  null,

@VendorId int =null, 


@from datetime  =null, 

@to datetime  = null, 

@NoAgree varchar(30) = '''',


@TimeTalkBegin int =  0, 
@TimeTalkEnd int = 600,

@Limit int  = 10,

@Page  int = 1, 

@OrderBy varchar (50) = null,
@UserId int = null ,
@dateType int = null,
@MemberId int  = -1,
@GroupId  int = -1
	
)
as
begin

declare @where  nvarchar(max) = '' where  isnull(d.Deleted,0) = 0 and d.DurationReal >0 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);
declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = '' select count(d.id) over() as TotalRecord ,d.*,
d.FileRecording as Recordingfile, 
dbo.getUserName(d.userid) as UserName,
d.linecode as Src, 
d.PhoneLog as dst, 

dbo.getOrderIdByCode(d.NoAgree)  as orderid
from ReportTalkTime d  '';




if(@PhoneLog <> '''')
begin
	set @where += '' and d.PhoneLog = @PhoneLog ''; 
end

if(@UserId >0 )
begin
	set @where += '' and d.LineCode in  ( select linecode  from  dbo.getAllLineCodeViewByUserIdOriginal(@UserId)) ''; 
end 

if(@LineCode > 0)
begin
	set @where += '' and d.LineCode = '' +  cast (@LineCode as varchar(10)) + '' ''; 
end

if(@GroupId >0 )
begin
	set @where += '' and d.LineCode in  ( select linecode  from  dbo.getAllLineCodeViewByGroupId(@GroupId)) ''; 
end

if(@MemberId >0 )
begin
	set @where += '' and d.LineCode in  ( select linecode  from  dbo.getAllLineCodeViewByUserIdOriginal(@MemberId)) ''; 
end 

if(@NoAgree != '''' )
begin
	set @where += '' and d.NoAgree = @NoAgree '' + '' ''; 
end

if(@TimeTalkBegin > -1)
begin
	set @where += '' and d.DurationReal >= @TimeTalkBegin'' + '' ''; 
end

if(@TimeTalkEnd > -1)
begin
	set @where += '' and d.DurationReal <= @TimeTalkEnd'' + '' ''; 
end




if(@from is not null )
begin 
set @where += '' 
and d.CreateAt  >= @fromDate
 ''; 
end 


if(@to is not null )
begin 
set @where += '' 
and  d.CreateAt  <= @toDate
''; 
end 

set @where +='' order by d.CreateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where



set @params =N'' @offset int, @limit int, @fromDate datetime,@toDate datetime, @dateType int, @UserId int ,  @VendorId int, @TimeTalkBegin int, @TimeTalkEnd int, @PhoneLog varchar(30), @NoAgree varchar(30), @GroupId int, @MemberId int  '';
EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit, @UserId = @UserId, @VendorId = @VendorId, @TimeTalkBegin = @TimeTalkBegin,@TimeTalkEnd = @TimeTalkEnd , @PhoneLog = @PhoneLog, @NoAgree = @NoAgree,@GroupId= @GroupId,@MemberId= @MemberId,
@fromDate = @from,@toDate =@to, @dateType = @dateType

select @mainClause

end')
PRINT '  âœ“ Procedure sp_RecordingFile_getAllv3 created';
-- Stored Procedure: sp_regionals_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_regionals_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_regionals_getAll...';
    DROP PROCEDURE [dbo].[sp_regionals_getAll];
END

PRINT '  â†’ Creating procedure sp_regionals_getAll...';
EXEC('CREATE procedure [dbo].[sp_regionals_getAll]
as
begin 
  select * from regionals order by priorites asc
end')
PRINT '  âœ“ Procedure sp_regionals_getAll created';
-- Stored Procedure: sp_RelationItem_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_RelationItem_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_RelationItem_getAll...';
    DROP PROCEDURE [dbo].[sp_RelationItem_getAll];
END

PRINT '  â†’ Creating procedure sp_RelationItem_getAll...';
EXEC('create procedure [dbo].[sp_RelationItem_getAll]
(
	@UserName varchar(8) = ''''
)
as
begin
 select * from RelationItem where UserName  = @UserName
end')
PRINT '  âœ“ Procedure sp_RelationItem_getAll created';
-- Stored Procedure: sp_RelationItem_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_RelationItem_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_RelationItem_insert...';
    DROP PROCEDURE [dbo].[sp_RelationItem_insert];
END

PRINT '  â†’ Creating procedure sp_RelationItem_insert...';
EXEC('--item.UserName,
 --item.Relationcode,
 --item.Name,
 --item.Phone,
 --item.Noted,
 --item.AddressInfo,
 --item.CreatedBy
 CREATE procedure [dbo].[sp_RelationItem_insert]
(
			@UserName nvarchar(50) null,
			@Relationcode varchar(4) null,	
			@Name nvarchar(50) null,
			@Phone nvarchar(50) null,
			@Noted nvarchar(500) null, 
			@AddressInfo nvarchar(500) null, 
			@CreatedBy int null
)
as
begin
			INSERT INTO [dbo].[RelationItem]
			([UserName]
			,[Relationcode]
			,[Name]
			,[Phone]
			,[Noted]
			,[AddressInfo]
			,[Deleted]
			,[IsActive]
			,[CreatedBy]
			,[UpdatedBy]
			,[CreateAt]
			,[UpdateAt])
			VALUES
			(@UserName
			,@Relationcode
			,@Name
			,@Phone
			,@Noted
			,@AddressInfo
			,0
			,1
			,@CreatedBy
			,@CreatedBy
			, getdate()
			,getdate()
			)

end')
PRINT '  âœ“ Procedure sp_RelationItem_insert created';
-- Stored Procedure: sp_RelationItem_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_RelationItem_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_RelationItem_update...';
    DROP PROCEDURE [dbo].[sp_RelationItem_update];
END

PRINT '  â†’ Creating procedure sp_RelationItem_update...';
EXEC('--item.UserName,
 --item.Relationcode,
 --item.Name,
 --item.Phone,
 --item.Noted,
 --item.AddressInfo,
 --item.CreatedBy
 create procedure [dbo].[sp_RelationItem_update]
(
			@id int null, 
			@Relationcode varchar(4) null,	
			@Name nvarchar(50) null,
			@Phone nvarchar(50) null,
			@Noted nvarchar(500) null, 
			@AddressInfo nvarchar(500) null, 
			@UpdatedBy int 
)
as
begin
update RelationItem set  
Relationcode = @Relationcode, Name = @Name, 
Phone  = @Phone,  AddressInfo = @AddressInfo,
Noted = @Noted, UpdatedBy=  @UpdatedBy , UpdateAt = getdate()
where id = @id 
end')
PRINT '  âœ“ Procedure sp_RelationItem_update created';
-- Stored Procedure: sp_ReportTalkTime_Insert2
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ReportTalkTime_Insert2]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_ReportTalkTime_Insert2...';
    DROP PROCEDURE [dbo].[sp_ReportTalkTime_Insert2];
END

PRINT '  â†’ Creating procedure sp_ReportTalkTime_Insert2...';
EXEC('create procedure [dbo].[sp_ReportTalkTime_Insert2]
(
		@Id int out , 
		@IsActive int  = 0,
		@LineCode varchar(20) = null,
		@NoAgree varchar(20) = null ,
		@PhoneLog varchar(100) =null ,
		@FileRecording varchar (200) =null,
		@VendorId int =null,
		@Duration int = null,
		@CompanyId  int = null ,
		@CampangnId int  =null, 
		@CallDate datetime  =null, 
		@EventTime  datetime  = null, 
		@Linkedid varchar(100) = null,
		@Disposition varchar(20) = null, 
		@DurationReal decimal (18,2) = null, 
		@DurationBill int  = null,
		@sourceCall int  =null, 
		@isCal bit  = null,
		@CreatedBy varchar(5)  = null , 
		@UpdatedBy varchar(5) =null, 
		@CreateAt datetime  =null, 
		@UpdateAt datetime = null
		
)
as
begin
if(@LineCode = ''5000'' or   @LineCode = ''5001'' or @LineCode = ''5002'')
begin 
	return;
end 


declare @iduser int;
set @iduser = 0;

select top 1 @iduser = e.Id  from Employees e where
e.LineCode = @LineCode and ISNULL( e.Deleted,0) =0

declare @idinput int = null;
set @idinput = ( select top 1 id from ReportTalkTime where CallDate = @CallDate
and  sourceCall = @sourceCall and LineCode = @LineCode and Disposition = @Disposition)
set @isCal  = 1;
declare @noAgreeInput nvarchar(30) = null;

set @noAgreeInput = (select top 1 d.Code  
from [Order] d inner join Candidate e on d.CandidateId = e.Id  where e.Phone = @PhoneLog
order by d.id desc )

if(@noAgreeInput is null)
begin 
	set @isCal = 0
end 
declare @endOfCallTemp datetime;
set @endOfCallTemp = @CallDate;
if ( @Duration >0)
begin 
set @endOfCallTemp =  DateAdd(ss, @Duration,  @CallDate);
end 
if(@idinput is null or  @idinput < 1 )
begin 
	insert into ReportTalkTime
		( 
		userid,
		isCal,
		sourceCall,
		LineCode, NoAgree, PhoneLog , FileRecording, VendorId, 
		Duration, CompanyId,CampangnId,CallDate, 
		EventTime , Linkedid , Disposition, DurationReal,
 
		DurationBill, CreatedBy, UpdatedBy,CreateAt, UpdateAt,
		[Deleted],
		endOfCall
		) 

		values(
		@iduser,
		@isCal,
		@sourceCall,
		@LineCode, @noAgreeInput, @PhoneLog , @FileRecording, @VendorId, 
		@Duration, @CompanyId,@CampangnId,@CallDate, 
		@EventTime , @Linkedid , @Disposition, @DurationReal,
 
		@DurationBill, @CreatedBy, @CreatedBy,getdate(),getdate(),
			0,
	@endOfCallTemp
	)

end 

end')
PRINT '  âœ“ Procedure sp_ReportTalkTime_Insert2 created';
-- Stored Procedure: sp_reportTalkTimeGroupByDay_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_reportTalkTimeGroupByDay_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_reportTalkTimeGroupByDay_insert...';
    DROP PROCEDURE [dbo].[sp_reportTalkTimeGroupByDay_insert];
END

PRINT '  â†’ Creating procedure sp_reportTalkTimeGroupByDay_insert...';
EXEC('CREATE procedure [dbo].[sp_reportTalkTimeGroupByDay_insert]
(
		@Id int out, 
		@LineCode varchar(20) null,
		@SumCall varchar(20) null,
		@SumNoAgree varchar(100) null,
		@BusinessTime datetime  null,
		@PerPercent decimal (18,2) null, 
		@IsActive int  = -1,
		@SumAn int null, 
		@VendorId int null, 
		@SumNoBussy int null, 
		@SumNOCancel int null, 
		@SumNOAswer int null, 
		@SumNOChanel int null, 

		@SumNOServe int null, 
		@TimeWaiting decimal (18,2) null, 
		@Timcall decimal (18,2) null, 
		@TimeTalking decimal (18,2) null, 
		@YearR  int null, 
		@MonthR int null, 
		@DayR int null,
		@SumNoFail int null, 
		@CreatedBy int null,
		@userId int =null
)
as
begin

declare @idUser  int ;
set @idUser = 0;

select @idUser = ( select top 1 id from  Employees where LineCode = @LineCode and  ISNULL( Deleted,0) =0   );
select  
@SumNoAgree = count(distinct(lg.ProfileId))
from LogCall lg

where lg.UserId = @idUser
and  MONTH(lg.CreateAt) = @MonthR and DAY(lg.CreateAt) = @DayR
and Year(lg.CreateAt) = @YearR

 insert into ReportTalkTimeGroupByDay( LineCode,SumCall,SumNoAgree, BusinessTime,
 
 PerPercent, SumAn, VendorId,SumNoBussy,SumNOCancel,SumNOAswer, SumNOChanel,SumNOServe, TimeWaiting,
 Timcall,TimeTalking,YearR, MonthR, DayR, SumNoFail, CreatedBy, UpdatedBy, CreateAt,UpdateAt,userId
 )
 values(
 @LineCode,@SumCall,@SumNoAgree, @BusinessTime,
 
 @PerPercent, @SumAn, @VendorId,@SumNoBussy,@SumNOCancel,@SumNOAswer, @SumNOChanel,@SumNOServe, @TimeWaiting,
 @Timcall,@TimeTalking,@YearR, @MonthR, @DayR, @SumNoFail, @CreatedBy,@CreatedBy, getdate(), getdate(),@idUser
 )

 end')
PRINT '  âœ“ Procedure sp_reportTalkTimeGroupByDay_insert created';
-- Stored Procedure: sp_reportTalkTimeGroupByDay_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_reportTalkTimeGroupByDay_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_reportTalkTimeGroupByDay_update...';
    DROP PROCEDURE [dbo].[sp_reportTalkTimeGroupByDay_update];
END

PRINT '  â†’ Creating procedure sp_reportTalkTimeGroupByDay_update...';
EXEC('CREATE procedure [dbo].[sp_reportTalkTimeGroupByDay_update]
(
		@Id int,
		@LineCode varchar(20) null,
		@SumCall varchar(20) null,
		@SumNoAgree varchar(100) null,
		@BusinessTime datetime null,
		@PerPercent decimal (18,2) null, 
		@SumAn int null, 
		@VendorId int null, 
		@SumNoBussy int null, 
		@SumNOCancel int null, 
		@SumNOAswer int null, 
		@SumNOChanel int null, 

		@SumNOServe int null, 
		@TimeWaiting decimal (18,2) null, 
		@Timcall decimal (18,2) null, 
		@TimeTalking decimal (18,2) null, 
		@YearR  int null, 
		@MonthR int null, 
		@DayR int null,
		@SumNoFail int null, 
		@userId int = null, 
	
		@UpdatedBy int null
)
as
begin

declare @idUser  int ;

select @idUser = ( select top 1 id from  Employees where LineCode = @LineCode and  ISNULL( Deleted,0) =0   );
select  
@SumNoAgree = count(distinct(lg.ProfileId))
from LogCall lg
where lg.UserId = @idUser
and  MONTH(lg.CreateAt) = @MonthR and DAY(lg.CreateAt) = @DayR
and Year(lg.CreateAt) = @YearR

declare @endlastCallTemp datetime;
set @endlastCallTemp = null;
select @endlastCallTemp = max ( e.endOfCall) from ReportTalkTime e 
where
e.userid = @idUser and 
e.CallDate >= CAST(GETDATE() AS DATE)

if(@endlastCallTemp is null)
begin 
		set @endlastCallTemp =  null;
end 
 update  ReportTalkTimeGroupByDay
 set  
SumCall = @Sumcall,SumNoAgree = @SumNoAgree, BusinessTime = @BusinessTime,
 
 PerPercent = @PerPercent , SumAn = @SumAn,
 VendorId = @VendorId ,SumNoBussy = @SumNoBussy,SumNOCancel = @SumNOCancel,
 SumNOAswer = @SumNOAswer , SumNOChanel = @SumNOChanel,SumNOServe = @SumNOServe, TimeWaiting = @TimeWaiting,
 Timcall = @Timcall ,TimeTalking = @TimeTalking,
 YearR = @YearR, MonthR = @MonthR, DayR =@DayR, SumNoFail = @SumNoFail, 
  userId =@idUser ,
 UpdatedBy = @UpdatedBy,UpdateAt = getdate(),
 lastCall = @endlastCallTemp
 where id =@id


 end')
PRINT '  âœ“ Procedure sp_reportTalkTimeGroupByDay_update created';
-- Stored Procedure: sp_ScheduleInterview_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ScheduleInterview_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_ScheduleInterview_getAll...';
    DROP PROCEDURE [dbo].[sp_ScheduleInterview_getAll];
END

PRINT '  â†’ Creating procedure sp_ScheduleInterview_getAll...';
EXEC('CREATE procedure [dbo].[sp_ScheduleInterview_getAll]
(
@Token nvarchar(30) ='''',
@OrderBy varchar(30) = '''',
@RelId int = -1,
@RelCode varchar(10) = '''',
@Type int =0,

@Page int =1,
@Status int = -1,
@Limit int = 1000,
@From datetime = null,
@To datetime = null
)
as
begin

set @Limit =1000;

declare @roleCode  varchar(3);
set @roleCode = '''';


declare @where  nvarchar(max) = '' where  1= 1 '';
declare @mainClause nvarchar(max);
declare @params nvarchar(300);

declare @offset int = 0;
set @offset = (@page-1)*@Limit;
set @mainClause = ''  select count(d.id) over() as TotalRecord,
d.* from  ScheduleInterview d  '';


if( @RelId > 0 )
begin 
	set @where += '' and   d.RelId = @RelId ''; 
end 


if( @Type > -1 )
begin 
	set @where += '' and   d.Type = @Type ''; 
end 


set @where +='' order by d.UpdateAt desc''; 
set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY''
set @mainClause = @mainClause +  @where
set @params =N'' @offset int, @limit int,
@fromDate datetime,@toDate datetime , 
@RelId int, @Type int  '';

EXECUTE sp_executesql @mainClause,@params, @offset = @offset, @limit = @limit,
@fromDate = @From,@toDate =@To, @RelId = @RelId , @Type = @Type



end')
PRINT '  âœ“ Procedure sp_ScheduleInterview_getAll created';
-- Stored Procedure: sp_ScheduleInterview_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ScheduleInterview_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_ScheduleInterview_insert...';
    DROP PROCEDURE [dbo].[sp_ScheduleInterview_insert];
END

PRINT '  â†’ Creating procedure sp_ScheduleInterview_insert...';
EXEC('create procedure [dbo].[sp_ScheduleInterview_insert]
(
	@RelId int null,
	@RelCode varchar(20) null,
	@Type int = null ,
	@ScheduleDate datetime  =  null,
	@AddressInfo nvarchar (500) null,
	@Noted nvarchar(500) =null,
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
           ,[Deleted]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt])
     VALUES
           (@RelId, 
		   @RelCode ,@Type,@ScheduleDate,
		   @AddressInfo,@Noted,0,0,@CreatedBy,
		   @CreatedBy, getdate(), getdate() )


end')
PRINT '  âœ“ Procedure sp_ScheduleInterview_insert created';
-- Stored Procedure: sp_tax_getAll
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_tax_getAll...';
    DROP PROCEDURE [dbo].[sp_tax_getAll];
END

PRINT '  â†’ Creating procedure sp_tax_getAll...';
EXEC('create procedure [dbo].[sp_tax_getAll]
(
	@userid varchar(8) = ''''
)
as
begin
			select top 1 *  from TaxItem where UserName  = @userid order by id desc
end')
PRINT '  âœ“ Procedure sp_tax_getAll created';
-- Stored Procedure: sp_tax_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_tax_insert...';
    DROP PROCEDURE [dbo].[sp_tax_insert];
END

PRINT '  â†’ Creating procedure sp_tax_insert...';
EXEC('CREATE PROCEDURE [dbo].[sp_tax_insert]
    @UserName NVARCHAR(100),
    @CodeId NVARCHAR(50) = NULL,
    @Number NVARCHAR(50) = NULL,
    @PageTax INT = NULL,
    @BiaSo INT = NULL,
    @RegBHYT NVARCHAR(50) = NULL,
    @CreatedBy INT = NULL,
    @PITDate DATETIME = NULL,
    @EffectedFrom DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[TaxItem] (
        [UserName], [CodeId], [Number], [PageTax], [BiaSo], [RegBHYT], [CreatedBy], [PITDate], [EffectedFrom]
    )
    VALUES (
        @UserName, @CodeId, @Number, @PageTax, @BiaSo, @RegBHYT, @CreatedBy, @PITDate, @EffectedFrom
    );
END')
PRINT '  âœ“ Procedure sp_tax_insert created';
-- Stored Procedure: sp_tax_udpate
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_udpate]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_tax_udpate...';
    DROP PROCEDURE [dbo].[sp_tax_udpate];
END

PRINT '  â†’ Creating procedure sp_tax_udpate...';
EXEC('CREATE PROCEDURE [dbo].[sp_tax_udpate]
    @Id INT,
    @UserName NVARCHAR(100),
    @CodeId NVARCHAR(50) = NULL,
    @Number NVARCHAR(50) = NULL,
    @PageTax INT = NULL,
    @BiaSo INT = NULL,
    @RegBHYT NVARCHAR(50) = NULL,
    @UpdatedBy INT = NULL,
    @PITDate DATETIME = NULL,
    @EffectedFrom DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[TaxItem]
    SET [UserName] = @UserName,
        [CodeId] = @CodeId,
        [Number] = @Number,
        [PageTax] = @PageTax,
        [BiaSo] = @BiaSo,
        [RegBHYT] = @RegBHYT,
        [UpdatedBy] = @UpdatedBy,
        [PITDate] = @PITDate,
        [EffectedFrom] = @EffectedFrom
    WHERE [Id] = @Id;
END')
PRINT '  âœ“ Procedure sp_tax_udpate created';
-- Stored Procedure: sp_UpdateCandidateAndOrderMarketting
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_UpdateCandidateAndOrderMarketting]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_UpdateCandidateAndOrderMarketting...';
    DROP PROCEDURE [dbo].[sp_UpdateCandidateAndOrderMarketting];
END

PRINT '  â†’ Creating procedure sp_UpdateCandidateAndOrderMarketting...';
EXEC('CREATE procedure [dbo].[sp_UpdateCandidateAndOrderMarketting]
(
@Name nvarchar(50) NULL,
@Email varchar(30) null,
@PhoneNumber varchar(20) NULL,
@Status int Null, 
@Dob datetime null,
@Source int null, 
@NotedCan nvarchar(500),
@CVLink varchar(300) NULL, 
@JobId int NULL,
@Document nvarchar(300) NULL,
@NotedOrder nvarchar(500) NULL, 
@userId INT null,
@idCan int null,
@Regional int = -1
)
as
begin
update Candidate   set 
CVLink = @CVLink,
ShortDes = @Document,
[Name] = @Name,
Noted = @NotedCan,
source = @Source,
Email = @Email,
Phone = @PhoneNumber,
dob = @Dob
where id = @idCan

if(@JobId < 1 )
begin 
return;
end 
  

  declare @maxId1 int =0 ;

set  @maxId1 = IDENT_CURRENT(''[Order]'');


if(@maxId1 is null)
begin 
	set @maxId1 =0;
end

set @maxId1 = @maxId1 +1;
declare @tempUser1  varchar(6);
if(@maxId1 < 10)
begin 
	set @tempUser1 = concat(''OD000'', @maxId1)
end 
if(@maxId1 < 100)
begin 
	set @tempUser1 = concat(''OD00'', @maxId1)
end 

if(@maxId1 < 1000)
begin 
	set @tempUser1 = concat(''OD0'', @maxId1)
end 
if(@maxId1 > 10000)
begin 
	set @tempUser1 = concat(''OD'', @maxId1)
end 

declare @partnerId1 int;
declare @ProjectId1 int;
select @partnerId1 = PartnerId, @ProjectId1 = ProjectId 
from JobItem where id = @JobId 


INSERT INTO [dbo].[Order]
           (
		    partnerId,
		    ProjectId,
		    [Code]
           ,[Status]
           ,[CandidateId]
           ,[JobId]
           ,[ShortDes]
           ,[CVLink]
           ,[Noted]
           ,[Deleted]
           ,[IsActive]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt],
		   source ,
		   DateGet ,
		   [Enable],
		   Assignee,
		   Regional
		   )
VALUES
(
 @partnerId1,@ProjectId1,@tempUser1,46,
 @idCan, @JobId,@Document,@CVLink,@NotedOrder,
 0,1,@userId,@userId,getdate(), getdate(),@Source,null, 0, null, @Regional
		   
)

insert into OrderImpactHIstory( OrderCode, 
ObjectInfo, OldStatus,NewStatus, Noted, 
Deleted, IsActive, CreatedBy, CreateAt, TxtTimer,
DateFrom, TxtPlace) 
values ( @tempUser1,'''' , 0, @Status, N''Tạo mới đơn hàng'', 0, 1, @userId,
GETDATE(), '''', null, '''')


end')
PRINT '  âœ“ Procedure sp_UpdateCandidateAndOrderMarketting created';
-- Stored Procedure: sp_UpdateOnboardStatus
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_UpdateOnboardStatus]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure sp_UpdateOnboardStatus...';
    DROP PROCEDURE [dbo].[sp_UpdateOnboardStatus];
END

PRINT '  â†’ Creating procedure sp_UpdateOnboardStatus...';
EXEC('CREATE procedure [dbo].[sp_UpdateOnboardStatus]
as
begin
update OnboardMember set result = 1, 
UpdateAt = GETDATE() 
where Warrantydate <= getdate() 
and result =0
end')
PRINT '  âœ“ Procedure sp_UpdateOnboardStatus created';
-- Stored Procedure: UpdateInfoOrder
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UpdateInfoOrder]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  â†’ Dropping existing procedure UpdateInfoOrder...';
    DROP PROCEDURE [dbo].[UpdateInfoOrder];
END

PRINT '  â†’ Creating procedure UpdateInfoOrder...';
EXEC('CREATE procedure [dbo].[UpdateInfoOrder]
(
	@PhoneNumber varchar(20) NULL,
	@FulName nvarchar(50) NULL,
	@Dob datetime NULL,
	@RequestId [int] NULL,
	@Email varchar(50) null, 
	@JobId int null,
	@ShortDes [nvarchar](500) NULL,
	@CVLink [varchar](500) NULL,
	@Noted [nvarchar](500) NULL,
	@CreatedBy [int] NULL,
	@Source int  = 0,
	@Assignee int null,
	@Isapply int null,
	@Regional int = null, 
	@SchoolName nvarchar(500) = '''',
	@RankLevel int = -1, 
	@Gender int = -1, 
	@Introduction nvarchar(max) = '''',
	@Experience int =-1
)
as
begin
declare  @DateGet datetime;
declare  @Enable int;

declare @templead int;
set @templead = 0;

select @templead = id from Employees where  RoleCode =3 and  id = @CreatedBy

declare @statusInput int;
set @statusInput = 7;


declare @roleId int =0;

select @roleId = RoleCode  from Employees  where id = @CreatedBy

if(@roleId = 6 or @roleId = 7)
begin 
 set @roleId = @roleId;

end 
else 
begin 

if(@Source = 0)
begin 
	set  @DateGet = getdate();
	set @Assignee = @CreatedBy;
	set @Enable =  1;

end 

end 




declare @partnerId1 int;
declare @ProjectId1 int;

if(@JobId >0)
begin 
	select @partnerId1 = PartnerId, @ProjectId1 = ProjectId 
	from JobItem where id = @JobId 
end 

declare @orderId int;

if(@RequestId >0)
begin 
	select @orderId = Id 
	from [Order] where id = @RequestId 
end 
declare @candidateId int;
set @candidateId = 0;


if( @orderId > 0)
begin 

				select @candidateId = CandidateId  from [Order] 
				where id = @orderId 
			
				update [Order]
				set ProjectId = @ProjectId1, partnerId =@partnerId1,
				JobId = @JobId, ShortDes =@ShortDes, CVLink = @CVLink,
				Noted = @Noted, 
				UpdatedBy = @CreatedBy, 
				Assignee =@Assignee,
				UpdateAt = getdate(), 
				Regional = @Regional,
				SchoolName = @SchoolName,
				RankLevel = @RankLevel,
				Gender = @Gender,
				Introduction = @Introduction,
				Experience = @Experience
				where   id = @orderId


				update Candidate 
				set  [Name] = @FulName,
				Phone = @PhoneNumber,
				Email =@Email,
				dob = @Dob,
				Assignee = @Assignee,
				CVLink = @CVLink,
				Noted = @Noted,
				ShortDes = @ShortDes
				where id = @candidateId 
				return;
end


declare @maxId  int =0 ;
set  @maxId = IDENT_CURRENT(''[Order]'');
if(@maxId is null)
begin 
	set @maxId =0;
end
set @maxId = @maxId +1;
declare @tempUser  varchar(6);
if(@maxId < 10)
begin 
	set @tempUser = concat(''UV000'', @maxId)
end 
else if(@maxId < 100)
begin 
	set @tempUser = concat(''UV00'', @maxId)
end 

else if(@maxId < 1000)
begin 
	set @tempUser = concat(''UV0'', @maxId)
end 
else 
begin 
	set @tempUser = concat(''UV'', @maxId)
end 


INSERT INTO [dbo].[Candidate]
           (
		    [Code]
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
		    Assignee
		   )
        VALUES
           (
		    @tempUser,
			@FulName,@Email,'''',
			@CVLink,@ShortDes,
			@PhoneNumber,@Noted,
			 @statusInput,
			 0,1,
			 @CreatedBy,@CreatedBy,getdate(),getdate(),@Dob,0,@Assignee
			)
declare @idNewCan int;
select @idNewCan = SCOPE_IDENTITY();


		   INSERT INTO [dbo].[Order]
           (
		   partnerId,
		   ProjectId,
		    [Code]
           ,[Status]
           ,[CandidateId]
           ,[JobId]
           ,[ShortDes]
           ,[CVLink]
           ,[Noted]
           ,[Deleted]
           ,[IsActive]
           ,[CreatedBy]
           ,[UpdatedBy]
           ,[CreateAt]
           ,[UpdateAt],
		   source ,
		   DateGet ,
		   [Enable],
		   Assignee,
		   Isapply, 
		   Regional,  SchoolName, RankLevel, Gender, Introduction, Experience
		   )
			VALUES
			(
				 @partnerId1,@ProjectId1, @tempUser,@statusInput,
				 @idNewCan,@JobId,
				 @ShortDes,
				 @CVLink,@Noted,0,1,
				 @CreatedBy,@CreatedBy,GETDATE(),getdate(),
				 @Source,
				 @DateGet, @Enable, @Assignee,
				 @Isapply,
				 @Regional, @SchoolName, @RankLevel, @Gender, @Introduction, @Experience
		   
			)

insert into OrderImpactHIstory( OrderCode, 
ObjectInfo, OldStatus,NewStatus, Noted, 
Deleted, IsActive, CreatedBy, CreateAt, TxtTimer,
DateFrom, TxtPlace) 
values ( @tempUser,'''' , 0, @statusInput, N''Tạo mới đơn hàng'', 0, 1, @CreatedBy,
GETDATE(), '''', null, '''')

end')
PRINT '  âœ“ Procedure UpdateInfoOrder created';

PRINT '';
PRINT '================================================';
PRINT 'Migration V002 completed successfully!';
PRINT '================================================';

COMMIT TRANSACTION;
