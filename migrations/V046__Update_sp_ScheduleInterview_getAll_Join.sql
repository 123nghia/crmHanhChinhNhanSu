-- =============================================
-- Migration: V046__Update_sp_ScheduleInterview_getAll_Join
-- Author: Antigravity
-- Date: 2026-01-16
-- Description: Update sp_ScheduleInterview_getAll to join with Candidate and include display names.
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V046: Update sp_ScheduleInterview_getAll with Join...';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ScheduleInterview_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  + Dropping existing procedure sp_ScheduleInterview_getAll...';
    DROP PROCEDURE [dbo].[sp_ScheduleInterview_getAll];
END

PRINT '  + Creating procedure sp_ScheduleInterview_getAll...';
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
    @To datetime = null
)
as
begin
    set @Limit = 1000;

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

    set @where += '' order by d.UpdateAt desc '';
    set @where += '' offset @offset ROWS FETCH NEXT @limit ROWS ONLY '';

    set @mainClause = @mainClause + @where;
    set @params = N''@offset int, @limit int,
        @fromDate datetime, @toDate datetime,
        @RelId int, @RelCode varchar(10), @Type int,
        @Status int, @InterviewerId int, @InterviewMode int'';

    EXECUTE sp_executesql @mainClause, @params,
        @offset = @offset, @limit = @limit,
        @fromDate = @From, @toDate = @To,
        @RelId = @RelId, @RelCode = @RelCode, @Type = @Type,
        @Status = @Status, @InterviewerId = @InterviewerId, @InterviewMode = @InterviewMode;
end');
PRINT '  + Procedure sp_ScheduleInterview_getAll updated with Candidate fields';

COMMIT TRANSACTION;
