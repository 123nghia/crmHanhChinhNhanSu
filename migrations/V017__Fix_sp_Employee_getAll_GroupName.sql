-- Migration V017: Fix sp_Employee_getAll to return correct GroupId and GroupName
-- Description: Updates the stored procedure to join with GroupMember and Group tables to provide actual group information.

GO
/****** Object:  StoredProcedure [dbo].[sp_Employee_getAll] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER procedure [dbo].[sp_Employee_getAll]
(
    @Token nvarchar(30) ='',      
    @OrderBy varchar(30) = '',    
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

    declare @where  nvarchar(max) = ' where  1= 1 ';
    declare @mainClause nvarchar(max);
    declare @params nvarchar(300);

    declare @offset int = 0;      
    set @offset = (@page-1)*@Limit;

    set @mainClause = ' select count(d.id) over() as TotalRecord, d.*,
         dbo.getDisplayMasterData(d.status) as StatusText,
         dbo.getDisplayMasterData(d.DocumentStatus) as DocumentStatusText,
         dbo.getDisplayMasterdata(d.DepartmentCode) as DepartmentText,
         dbo.getDisplayMasterdata(d.PositionCode) as PositionText,
         gm.GroupId,
         g.Name as GroupName
    from Employees d 
    left join GroupMember gm on d.Id = gm.MemberId and isnull(gm.Deleted,0) = 0
    left join [Group] g on gm.GroupId = g.Id and isnull(g.Deleted,0) = 0 ';

    if(@Token is not null and @Token <> '')       
    begin
        set @where += ' and (d.UserName like  N''%' + @Token +'%'' or d.FullName like  N''%' + @Token +'%'' or d.Phone like  N''%' + @Token +'%'')';
    end

    if(@GroupId > 0)
    begin
        set @where += ' and gm.GroupId = ' + cast(@GroupId as varchar(10));
    end

    if( @IsDeleted  < 1 )
    begin
        set @where += ' and isnull(d.Deleted,0) = 0  ';
    end

    if( @Status >-1 )
    begin
        set @where += ' and d.IsActive =  @Status ';
    end

    if(@From is not null )        
    begin 
        set @where += ' and (d.CreateAt >= @fromDate ) ';
    end

    if(@to is not null )
    begin
        set @where += ' and (d.CreateAt <= @toDate ) ';
    end

    if( @userId > 0 )
    begin
        set @where += ' and d.id in (select id from getAllUserByUserId(@userId)) ';
    end

    -- Sorting
    set @where += ' order by ';
    if(@OrderBy is not null and @OrderBy <> '')
    begin
        set @where += @OrderBy;
    end
    else
    begin
        set @where += ' d.UpdateAt desc ';
    end

    set @mainClause = @mainClause + @where;

    set @params = N' @offset int, @limit int, @fromDate datetime, @toDate datetime, @Status int, @userId int ';

    EXECUTE sp_executesql @mainClause, @params, 
        @offset = @offset, 
        @limit = @limit,
        @fromDate = @From, 
        @toDate = @To, 
        @Status = @Status, 
        @userId = @userId;
end
GO
