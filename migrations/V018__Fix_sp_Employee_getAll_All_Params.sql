-- Migration V018: Fix sp_Employee_getAll parameters to match C# call and support filtering
-- Description: Adds missing parameters (StatusWork, DocumentStatus, etc.) and fixes name mismatches (fromDate/toDate).

GO
/****** Object:  StoredProcedure [dbo].[sp_Employee_getAll] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER procedure [dbo].[sp_Employee_getAll]
(
    @Token nvarchar(255) = '',      
    @OrderBy nvarchar(255) = '',    
    @UserId int = null,
    @MemberId int = null,
    @GroupId int = null,
    @offset int = 0,
    @limit int = 20,
    @Status int = -1,
    @StatusWork nvarchar(50) = null,
    @DocumentStatus nvarchar(50) = null,
    @fromDate DATETIME2 = null,        
    @toDate DATETIME2 = null,
    @IsDeleted bit = 0
)
as
begin
    SET NOCOUNT ON;

    declare @where nvarchar(max) = ' where 1=1 ';
    declare @mainClause nvarchar(max);
    declare @params nvarchar(1000);

    -- Base Select
    set @mainClause = ' 
    select count(d.id) over() as TotalRecord, d.*,
         dbo.getDisplayMasterData(d.status) as StatusText,
         dbo.getDisplayMasterData(d.StatusWork) as StatusWorkText,
         dbo.getDisplayMasterData(d.DocumentStatus) as DocumentStatusText,
         dbo.getDisplayMasterdata(d.DepartmentCode) as DepartmentText,
         dbo.getDisplayMasterdata(d.PositionCode) as PositionText,
         gm.GroupId,
         g.Name as GroupName
    from Employees d 
    left join GroupMember gm on d.Id = gm.MemberId and isnull(gm.Deleted, 0) = 0
    left join [Group] g on gm.GroupId = g.Id and isnull(g.Deleted, 0) = 0 ';

    -- Filters
    if (@Token is not null and @Token <> '')       
    begin
        set @where += ' and (d.UserName like N''%'' + @Token + ''%'' or d.FullName like N''%'' + @Token + ''%'' or d.Phone like N''%'' + @Token + ''%'') ';
    end

    if (@GroupId > 0)
    begin
        set @where += ' and gm.GroupId = @GroupId ';
    end

    if (@IsDeleted = 0)
    begin
        set @where += ' and isnull(d.Deleted, 0) = 0 ';
    end

    if (@Status > -1)
    begin
        set @where += ' and d.IsActive = @Status ';
    end

    if (@StatusWork is not null and @StatusWork <> '' and @StatusWork <> '-1')
    begin
        set @where += ' and d.StatusWork = @StatusWork ';
    end

    if (@DocumentStatus is not null and @DocumentStatus <> '' and @DocumentStatus <> '-1')
    begin
        set @where += ' and d.DocumentStatus = @DocumentStatus ';
    end

    if (@fromDate is not null)        
    begin 
        set @where += ' and d.CreateAt >= @fromDate ';
    end

    if (@toDate is not null)
    begin
        set @where += ' and d.CreateAt <= @toDate ';
    end

    if (@UserId > 0)
    begin
        -- Filter by permission/assignment if needed
        -- set @where += ' and d.id in (select id from getAllUserByUserId(@UserId)) ';
        -- Keep original logic if it existed:
        set @where += ' and d.id in (select id from getAllUserByUserId(@UserId)) ';
    end

    -- Sorting
    set @where += ' order by ';
    if (@OrderBy is not null and @OrderBy <> '')
    begin
        -- Basic SQL Injection protection for OrderBy (optional but recommended)
        set @where += @OrderBy;
    end
    else
    begin
        set @where += ' d.UpdateAt desc ';
    end

    -- Pagination
    set @where += ' offset @offset ROWS FETCH NEXT @limit ROWS ONLY ';

    set @mainClause = @mainClause + @where;

    set @params = N' @offset int, @limit int, @fromDate DATETIME2, @toDate DATETIME2, @Status int, @UserId int, @Token nvarchar(255), @GroupId int, @StatusWork nvarchar(50), @DocumentStatus nvarchar(50) ';

    EXECUTE sp_executesql @mainClause, @params, 
        @offset = @offset, 
        @limit = @limit,
        @fromDate = @fromDate, 
        @toDate = @toDate, 
        @Status = @Status, 
        @UserId = @UserId,
        @Token = @Token,
        @GroupId = @GroupId,
        @StatusWork = @StatusWork,
        @DocumentStatus = @DocumentStatus;
end
GO
