-- =============================================
-- Migration: V021__Leave_Workflow_Update
-- Author: System
-- Description: Update Leave system for multi-level approval (Lead -> HCNS -> BGĐ)
-- =============================================

PRINT 'Applying migration V021: Updating Leave Workflow...';

-- 1. Add columns to LeaveRequests
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequests]') AND name = N'HandoverEmployeeId')
    ALTER TABLE [dbo].[LeaveRequests] ADD [HandoverEmployeeId] INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequests]') AND name = N'Attachment')
    ALTER TABLE [dbo].[LeaveRequests] ADD [Attachment] NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequests]') AND name = N'LeadApproverId')
    ALTER TABLE [dbo].[LeaveRequests] ADD [LeadApproverId] INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequests]') AND name = N'LeadApproveAt')
    ALTER TABLE [dbo].[LeaveRequests] ADD [LeadApproveAt] DATETIME NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequests]') AND name = N'LeadComment')
    ALTER TABLE [dbo].[LeaveRequests] ADD [LeadComment] NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequests]') AND name = N'HCNSApproverId')
    ALTER TABLE [dbo].[LeaveRequests] ADD [HCNSApproverId] INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequests]') AND name = N'HCNSApproveAt')
    ALTER TABLE [dbo].[LeaveRequests] ADD [HCNSApproveAt] DATETIME NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequests]') AND name = N'HCNSComment')
    ALTER TABLE [dbo].[LeaveRequests] ADD [HCNSComment] NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequests]') AND name = N'BGDApproverId')
    ALTER TABLE [dbo].[LeaveRequests] ADD [BGDApproverId] INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequests]') AND name = N'BGDApproveAt')
    ALTER TABLE [dbo].[LeaveRequests] ADD [BGDApproveAt] DATETIME NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequests]') AND name = N'BGDComment')
    ALTER TABLE [dbo].[LeaveRequests] ADD [BGDComment] NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LeaveRequests]') AND name = N'IsActingApproval')
    ALTER TABLE [dbo].[LeaveRequests] ADD [IsActingApproval] BIT DEFAULT 0;

-- 2. Update Employees table for Leave Balance
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'AllowedLeaveDays')
    ALTER TABLE [dbo].[Employees] ADD [AllowedLeaveDays] DECIMAL(18,2) DEFAULT 12;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'UsedLeaveDays')
    ALTER TABLE [dbo].[Employees] ADD [UsedLeaveDays] DECIMAL(18,2) DEFAULT 0;

-- 3. Update Stored Procedures

-- Update sp_Leave_Save to include HandoverEmployeeId
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Leave_Save]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Leave_Save];
GO

CREATE PROCEDURE [dbo].[sp_Leave_Save]
    @Id int = 0,
    @EmployeeId int,
    @LeaveTypeCode varchar(8),
    @FromDate datetime,
    @ToDate datetime,
    @NumDays decimal(18,2),
    @Reason nvarchar(max),
    @HandoverEmployeeId int = NULL,
    @UserId int
AS
BEGIN
    SET NOCOUNT ON;
    IF @Id > 0
    BEGIN
        UPDATE LeaveRequests SET
            LeaveTypeCode = @LeaveTypeCode,
            FromDate = @FromDate,
            ToDate = @ToDate,
            NumDays = @NumDays,
            Reason = @Reason,
            HandoverEmployeeId = @HandoverEmployeeId,
            UpdatedBy = @UserId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;
        SELECT @Id;
    END
    ELSE
    BEGIN
        INSERT INTO LeaveRequests (EmployeeId, LeaveTypeCode, FromDate, ToDate, NumDays, Reason, HandoverEmployeeId, Status, CreatedBy, CreateAt, UpdatedBy, UpdateAt, Deleted)
        VALUES (@EmployeeId, @LeaveTypeCode, @FromDate, @ToDate, @NumDays, @Reason, @HandoverEmployeeId, 0, @UserId, GETDATE(), @UserId, GETDATE(), 0);
        SELECT SCOPE_IDENTITY();
    END
END
GO

-- Update sp_Leave_GetAll to include details
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Leave_GetAll]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Leave_GetAll];
GO

CREATE PROCEDURE [dbo].[sp_Leave_GetAll]
    @EmployeeId int = NULL,
    @Status int = NULL,
    @FromDate datetime = NULL,
    @ToDate datetime = NULL,
    @Page int = 1,
    @Limit int = 20
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Offset int = (@Page - 1) * @Limit;

    SELECT 
        l.*,
        e.FullName as EmployeeName,
        h.FullName as HandoverEmployeeName,
        mt.Name as LeaveTypeName,
        l.ApproverId, -- Backward compatibility
        ap.FullName as ApproverName, -- Backward compatibility
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
    ORDER BY l.CreateAt DESC
    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
END
GO

-- Update sp_Leave_GetById
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Leave_GetById]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Leave_GetById];
GO

CREATE PROCEDURE [dbo].[sp_Leave_GetById]
    @Id int
AS
BEGIN
    SELECT 
        l.*,
        e.FullName as EmployeeName,
        h.FullName as HandoverEmployeeName,
        mt.Name as LeaveTypeName,
        le.FullName as LeadApproverName,
        hc.FullName as HCNSApproverName,
        bg.FullName as BGDApproverName
    FROM LeaveRequests l
    JOIN Employees e ON l.EmployeeId = e.Id
    JOIN MasterData mt ON l.LeaveTypeCode = mt.Code AND mt.TypeData = 30
    LEFT JOIN Employees h ON l.HandoverEmployeeId = h.Id
    LEFT JOIN Employees le ON l.LeadApproverId = le.Id
    LEFT JOIN Employees hc ON l.HCNSApproverId = hc.Id
    LEFT JOIN Employees bg ON l.BGDApproverId = bg.Id
    WHERE l.Id = @Id;
END
GO

-- NEW sp_Leave_Approve_Workflow
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Leave_Approve_Workflow]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Leave_Approve_Workflow];
GO

CREATE PROCEDURE [dbo].[sp_Leave_Approve_Workflow]
    @Id int,
    @Action nvarchar(20), -- 'Agree', 'Reject', 'Acting'
    @ApproverId int,
    @RoleCode nvarchar(50), -- '1' (Admin), 'TL' (Lead), 'HCNS' (HR), 'BGD' (Board)
    @Comment nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CurrentStatus int;
    DECLARE @EmpId int;
    DECLARE @NumDays decimal(18,2);
    DECLARE @LeaveType varchar(8);

    SELECT @CurrentStatus = Status, @EmpId = EmployeeId, @NumDays = NumDays, @LeaveType = LeaveTypeCode 
    FROM LeaveRequests WHERE Id = @Id;

    IF @Action = 'Reject'
    BEGIN
        UPDATE LeaveRequests SET 
            Status = 5, -- Rejected
            Comment = @Comment,
            ApproverId = @ApproverId,
            ApproveAt = GETDATE(),
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;
        RETURN;
    END

    -- Lead Approval (Level 1)
    IF @CurrentStatus = 0 AND (@RoleCode = 'TL' OR @RoleCode = '1' OR @RoleCode = '3') -- Role 3 is TL
    BEGIN
        UPDATE LeaveRequests SET 
            Status = 1, -- Pending HCNS
            LeadApproverId = @ApproverId,
            LeadApproveAt = GETDATE(),
            LeadComment = @Comment,
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;
    END
    -- HCNS Approval (Level 2)
    ELSE IF @CurrentStatus = 1 AND (@RoleCode = 'HCNS' OR @RoleCode = '2' OR @RoleCode = '1') -- Role 2 is HCNS/TC
    BEGIN
        UPDATE LeaveRequests SET 
            Status = 2, -- Pending BGD
            HCNSApproverId = @ApproverId,
            HCNSApproveAt = GETDATE(),
            HCNSComment = @Comment,
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;
    END
    -- BGD Approval (Level 3 - Final)
    ELSE IF @CurrentStatus = 2 AND (@RoleCode = 'BGD' OR @RoleCode = '1')
    BEGIN
        UPDATE LeaveRequests SET 
            Status = 3, -- Approved
            BGDApproverId = @ApproverId,
            BGDApproveAt = GETDATE(),
            BGDComment = @Comment,
            ApproverId = @ApproverId,
            ApproveAt = GETDATE(),
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;

        -- UPDATE LEAVE BALANCE
        IF @LeaveType = 'NP' -- Only Year Leave subtracts from balance or based on policy
        BEGIN
            UPDATE Employees SET UsedLeaveDays = ISNULL(UsedLeaveDays, 0) + @NumDays WHERE Id = @EmpId;
        END
    END
    -- HCNS Acting Approval for BGD
    ELSE IF (@CurrentStatus = 2 OR @CurrentStatus = 1) AND @Action = 'Acting' AND (@RoleCode = 'HCNS' OR @RoleCode = '2' OR @RoleCode = '1')
    BEGIN
        UPDATE LeaveRequests SET 
            Status = 4, -- Approved (Acting)
            IsActingApproval = 1,
            HCNSApproverId = @ApproverId,
            HCNSApproveAt = GETDATE(),
            HCNSComment = N'[ACTING AS BGD] ' + @Comment,
            ApproverId = @ApproverId,
            ApproveAt = GETDATE(),
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;

        -- UPDATE LEAVE BALANCE
        IF @LeaveType = 'NP'
        BEGIN
            UPDATE Employees SET UsedLeaveDays = ISNULL(UsedLeaveDays, 0) + @NumDays WHERE Id = @EmpId;
        END
    END
END
GO

PRINT '✓ Migration V021 completed successfully';
