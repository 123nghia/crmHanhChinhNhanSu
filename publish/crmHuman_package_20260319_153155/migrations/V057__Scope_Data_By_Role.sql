-- =============================================
-- Migration: V057__Scope_Data_By_Role
-- Description: Apply role-based data scope for employee/candidate/interview/leave lists.
-- =============================================

PRINT 'Applying migration V057: Scope data by role...';

-- Update function getAllUserByUserId
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[getAllUserByUserId]') AND type IN (N'FN', N'IF', N'TF', N'FS', N'FT'))
    DROP FUNCTION [dbo].[getAllUserByUserId];
GO

CREATE FUNCTION [dbo].[getAllUserByUserId]
(
    @userId int = null
)
RETURNS @table table (id int)
AS
BEGIN
    DECLARE @roleId int = null;

    SELECT @roleId = e.RoleCode
    FROM Employees e
    WHERE e.Id = @userId;

    IF (@roleId IN (1, 2, 4, 8))
    BEGIN
        INSERT INTO @table
        SELECT Id FROM Employees WHERE ISNULL(Deleted, 0) = 0;
        RETURN;
    END

    IF (@roleId IN (3, 6))
    BEGIN
        DECLARE @groupid int;
        SET @groupid = 0;
        SELECT @groupid = Id FROM [Group] WHERE ManagerId = @userId AND ISNULL(Deleted, 0) = 0;
        IF (@groupid < 1)
        BEGIN
            INSERT INTO @table SELECT @userId;
            RETURN;
        END
        ELSE
        BEGIN
            INSERT INTO @table
            SELECT e.MemberId FROM GroupMember e WHERE e.GroupId = @groupid AND ISNULL(e.Deleted, 0) = 0
            UNION SELECT @userId;
            RETURN;
        END
    END

    IF (@userId IS NOT NULL)
    BEGIN
        INSERT INTO @table SELECT @userId;
        RETURN;
    END

    RETURN;
END
GO

-- Update sp_candidate_getAll to apply scope by user
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_candidate_getAll]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_candidate_getAll];
GO

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

if(@userId is not null and @userId > 0 and ISNULL(@roleCode, '''') not in (''1'',''2'',''4'',''8''))
begin 
    set @where += '' and ( d.ManagerId in ( select id  from  getAllUserByUserId(@userId)) 
        or d.CreatedBy in ( select id  from  getAllUserByUserId(@userId)) ) '';
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
GO

-- Update sp_ScheduleInterview_getAll to apply scope by user
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ScheduleInterview_getAll]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_ScheduleInterview_getAll];
GO

EXEC('CREATE procedure [dbo].[sp_ScheduleInterview_getAll]
(
    @Token nvarchar(30) = '''',
    @OrderBy varchar(30) = '''',
    @RelId int = -1,
    @RelCode varchar(10) = '''',
    @Type int = -1,
    @Page int = 1,
    @Status int = -1,
    @InterviewerId int = -1,
    @InterviewMode int = -1,
    @Limit int = 1000,
    @From datetime = null,
    @To datetime = null,
    @UserId int = null
)
as
begin
    set @Limit = 1000;

    declare @roleCode varchar(3);
    set @roleCode = '''';
    select top 1 @roleCode = RoleCode from Employees where Id = @UserId;

    declare @where nvarchar(max) = '' where 1 = 1 '';
    declare @mainClause nvarchar(max);
    declare @params nvarchar(400);
    declare @offset int = 0;

    set @offset = (@page - 1) * @Limit;
    set @mainClause = '' 
        select count(d.id) over() as TotalRecord, 
               c.Name as CandidateFullName,
               dbo.getDisplayMasterdata(c.Position) as PositionText,
               d.* 
        from ScheduleInterview d 
        left join Candidate c on d.RelId = c.Id
    '';

    if (@RelId > 0)
        set @where += '' and d.RelId = @RelId '';

    if (@RelCode <> '''')
        set @where += '' and d.RelCode = @RelCode '';

    if (@Type > -1)
        set @where += '' and d.Type = @Type '';

    if (@Status > -1)
        set @where += '' and d.status = @Status '';

    if (@InterviewerId > 0)
        set @where += '' and d.InterviewerId = @InterviewerId '';

    if (@InterviewMode > -1)
        set @where += '' and d.InterviewMode = @InterviewMode '';

    if (@From is not null)
        set @where += '' and d.ScheduleDate >= @fromDate '';

    if (@To is not null)
        set @where += '' and d.ScheduleDate <= @toDate '';

    if (@UserId is not null and @UserId > 0 and ISNULL(@roleCode, '''') not in (''1'',''2'',''4'',''8''))
        set @where += '' and ( d.InterviewerId in ( select id from getAllUserByUserId(@UserId))
            or c.ManagerId in ( select id from getAllUserByUserId(@UserId))
            or c.CreatedBy in ( select id from getAllUserByUserId(@UserId)) ) '';

    set @where += '' order by d.UpdateAt desc '';
    set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY '';

    set @mainClause = @mainClause + @where;
    set @params = N''@offset int, @limit int,
        @fromDate datetime, @toDate datetime,
        @RelId int, @RelCode varchar(10), @Type int,
        @Status int, @InterviewerId int, @InterviewMode int, @UserId int'';

    EXECUTE sp_executesql @mainClause, @params,
        @offset = @offset, @limit = @limit,
        @fromDate = @From, @toDate = @To,
        @RelId = @RelId, @RelCode = @RelCode, @Type = @Type,
        @Status = @Status, @InterviewerId = @InterviewerId, @InterviewMode = @InterviewMode,
        @UserId = @UserId;
end');
GO

-- Update sp_Leave_GetAll to apply scope by user
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Leave_GetAll]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Leave_GetAll];
GO

CREATE PROCEDURE [dbo].[sp_Leave_GetAll]
    @EmployeeId int = NULL,
    @Status int = NULL,
    @FromDate datetime = NULL,
    @ToDate datetime = NULL,
    @Page int = 1,
    @Limit int = 20,
    @UserId int = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Offset int = (@Page - 1) * @Limit;
    DECLARE @roleCode varchar(3) = NULL;

    SELECT @roleCode = RoleCode FROM Employees WHERE Id = @UserId;

    SELECT 
        l.*,
        e.FullName as EmployeeName,
        h.FullName as HandoverEmployeeName,
        mt.Name as LeaveTypeName,
        l.ApproverId,
        ap.FullName as ApproverName,
        le.FullName as LeadApproverName,
        hc.FullName as HCNSApproverName,
        bg.FullName as BGDApproverName,
        COUNT(*) OVER() as TotalRecord
    FROM LeaveRequests l
    JOIN Employees e ON l.EmployeeId = e.Id
    JOIN MasterData mt ON l.LeaveTypeCode = mt.Code AND mt.TypeData = 30
    LEFT JOIN Employees h ON l.HandoverEmployeeId = h.Id
    LEFT JOIN Employees ap ON l.ApproverId = ap.Id
    LEFT JOIN Employees le ON l.LeadApproverId = le.Id
    LEFT JOIN Employees hc ON l.HCNSApproverId = hc.Id
    LEFT JOIN Employees bg ON l.BGDApproverId = bg.Id
    WHERE l.Deleted = 0
      AND (@EmployeeId IS NULL OR l.EmployeeId = @EmployeeId)
      AND (@Status IS NULL OR l.Status = @Status)
      AND (@FromDate IS NULL OR l.FromDate >= @FromDate)
      AND (@ToDate IS NULL OR l.ToDate <= @ToDate)
      AND (
            @UserId IS NULL OR @UserId <= 0
            OR ISNULL(@roleCode, '') IN ('1','2','4','8')
            OR l.EmployeeId IN (SELECT id FROM getAllUserByUserId(@UserId))
          )
    ORDER BY l.CreateAt DESC
    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
END
GO

PRINT 'Migration V057 completed successfully.';
