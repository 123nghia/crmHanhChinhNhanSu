-- =============================================
-- Migration: V038__Interview_Schedule_Workflow
-- Author: System
-- Date: 2026-01-15
-- Description: Extend ScheduleInterview for interview workflow (status, mode, interviewer)
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V038: Interview Schedule Workflow...';

-- ============================================
-- 1. Extend ScheduleInterview table
-- ============================================
IF COL_LENGTH('dbo.ScheduleInterview', 'InterviewerId') IS NULL
BEGIN
    PRINT '  + Adding InterviewerId to ScheduleInterview...';
    ALTER TABLE [dbo].[ScheduleInterview]
    ADD [InterviewerId] [int] NULL;
END
ELSE
BEGIN
    PRINT '  + InterviewerId already exists, skipping.';
END

IF COL_LENGTH('dbo.ScheduleInterview', 'InterviewMode') IS NULL
BEGIN
    PRINT '  + Adding InterviewMode to ScheduleInterview...';
    ALTER TABLE [dbo].[ScheduleInterview]
    ADD [InterviewMode] [int] NULL;
END
ELSE
BEGIN
    PRINT '  + InterviewMode already exists, skipping.';
END

-- ============================================
-- 2. Update sp_ScheduleInterview_getAll
-- ============================================
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
    set @mainClause = '' select count(d.id) over() as TotalRecord, d.* from ScheduleInterview d '';

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
PRINT '  + Procedure sp_ScheduleInterview_getAll created';

-- ============================================
-- 3. Update sp_ScheduleInterview_insert
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
         0,
         @CreatedBy,
         @CreatedBy,
         getdate(),
         getdate());
end');
PRINT '  + Procedure sp_ScheduleInterview_insert created';

-- ============================================
-- 4. Add sp_ScheduleInterview_update
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
        UpdatedBy = ISNULL(@UpdatedBy, UpdatedBy),
        UpdateAt = getdate()
    where Id = @Id;
end');
PRINT '  + Procedure sp_ScheduleInterview_update created';

-- ============================================
-- 5. Add AppPages entry for ScheduleInterview
-- ============================================
PRINT '  + Syncing AppPages for ScheduleInterview...';
IF NOT EXISTS (SELECT 1 FROM AppPages WHERE Code = 'ScheduleInterview')
BEGIN
    INSERT INTO AppPages (Code, Name, [Group], OrderIndex, IsActive)
    VALUES ('ScheduleInterview', N'Danh sach phong van', 'Human Resource', 4, 1);

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    VALUES ('1', 'ScheduleInterview', 1, 1, 1, 1, 1);
END
ELSE
BEGIN
    PRINT '  + ScheduleInterview already exists, skipping.';
END

PRINT 'Migration V038 completed successfully';

COMMIT TRANSACTION;
