-- =============================================
-- Migration: V022__Leave_History_And_Notifications
-- Author: System
-- Description: Implement Audit Trail (History) and Notifications for Leave Management
-- =============================================

PRINT 'Applying migration V022: History and Notifications...';

-- 1. Create LeaveHistories table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LeaveHistories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LeaveHistories](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [LeaveId] [int] NOT NULL,
        [Action] [nvarchar](50) NOT NULL, -- 'Create', 'Update', 'Agree', 'Reject', 'Acting', 'Cancel'
        [ActionBy] [int] NOT NULL, -- EmployeeId
        [ActionTime] [datetime] DEFAULT GETDATE(),
        [Comment] [nvarchar](max) NULL,
        [StatusAfter] [int] NULL,
        PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT '  ✓ Table LeaveHistories created.';
END

-- 2. Create AppNotifications table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AppNotifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AppNotifications](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [ReceiverId] [int] NOT NULL,
        [SenderId] [int] NULL,
        [Message] [nvarchar](max) NOT NULL,
        [Link] [nvarchar](255) NULL,
        [Type] [nvarchar](50) NULL, -- 'LeaveRequest'
        [IsRead] [bit] DEFAULT 0,
        [CreateAt] [datetime] DEFAULT GETDATE(),
        PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT '  ✓ Table AppNotifications created.';
END

-- 3. Update Stored Procedures

-- Update sp_Leave_Save to include History and Initial Notification
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
    DECLARE @NewId int;
    DECLARE @Action nvarchar(50);
    DECLARE @ManagerId int;
    DECLARE @EmpName nvarchar(200);

    SELECT @EmpName = FullName, @ManagerId = ManagerId FROM Employees WHERE Id = @EmployeeId;

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
        SET @NewId = @Id;
        SET @Action = 'Update';
    END
    ELSE
    BEGIN
        INSERT INTO LeaveRequests (EmployeeId, LeaveTypeCode, FromDate, ToDate, NumDays, Reason, HandoverEmployeeId, Status, CreatedBy, CreateAt, UpdatedBy, UpdateAt, Deleted)
        VALUES (@EmployeeId, @LeaveTypeCode, @FromDate, @ToDate, @NumDays, @Reason, @HandoverEmployeeId, 0, @UserId, GETDATE(), @UserId, GETDATE(), 0);
        SET @NewId = SCOPE_IDENTITY();
        SET @Action = 'Create';
    END

    -- Insert History
    INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
    VALUES (@NewId, @Action, @UserId, GETDATE(), @Reason, 0);

    -- Notification to Manager (Lead) if it's a new request or resubmission
    IF @Action = 'Create' AND @ManagerId IS NOT NULL
    BEGIN
        INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type)
        VALUES (@ManagerId, @EmployeeId, N'Nhân viên ' + @EmpName + N' đã gửi đơn xin nghỉ phép chờ bạn duyệt.', '/Leave/LeaveApproval', 'LeaveRequest');
    END

    SELECT @NewId;
END
GO

-- Update sp_Leave_Approve_Workflow to include History and Notifications
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
    DECLARE @EmpName nvarchar(200);
    DECLARE @ApproverName nvarchar(200);

    SELECT @CurrentStatus = Status, @EmpId = EmployeeId, @NumDays = NumDays, @LeaveType = LeaveTypeCode 
    FROM LeaveRequests WHERE Id = @Id;

    SELECT @EmpName = FullName FROM Employees WHERE Id = @EmpId;
    SELECT @ApproverName = FullName FROM Employees WHERE Id = @ApproverId;

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

        -- History
        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Reject', @ApproverId, GETDATE(), @Comment, 5);

        -- Notify Employee
        INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type)
        VALUES (@EmpId, @ApproverId, N'Đơn xin nghỉ phép của bạn đã bị từ chối bởi ' + @ApproverName + N'. Lý do: ' + @Comment, '/Leave/LeaveRequest', 'LeaveRequest');

        RETURN;
    END

    -- Lead Approval (Level 1)
    IF @CurrentStatus = 0 AND (@RoleCode = 'TL' OR @RoleCode = '1' OR @RoleCode = '3')
    BEGIN
        UPDATE LeaveRequests SET 
            Status = 1, -- Pending HCNS
            LeadApproverId = @ApproverId,
            LeadApproveAt = GETDATE(),
            LeadComment = @Comment,
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;

        -- History
        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Agree', @ApproverId, GETDATE(), N'[Lead Approved] ' + @Comment, 1);

        -- Notify HCNS (All employees with Role '2')
        INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type)
        SELECT Id, @ApproverId, N'Có đơn nghỉ phép của ' + @EmpName + N' đã được Lead duyệt, chờ HCNS xử lý.', '/Leave/LeaveApproval', 'LeaveRequest'
        FROM Employees WHERE RoleCode = '2' AND ISNULL(Deleted,0)=0;
    END
    -- HCNS Approval (Level 2)
    ELSE IF @CurrentStatus = 1 AND (@RoleCode = 'HCNS' OR @RoleCode = '2' OR @RoleCode = '1')
    BEGIN
        UPDATE LeaveRequests SET 
            Status = 2, -- Pending BGD
            HCNSApproverId = @ApproverId,
            HCNSApproveAt = GETDATE(),
            HCNSComment = @Comment,
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;

        -- History
        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Agree', @ApproverId, GETDATE(), N'[HCNS Approved] ' + @Comment, 2);

        -- Notify BGD (Admin/Role '1')
        INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type)
        SELECT Id, @ApproverId, N'Có đơn nghỉ phép của ' + @EmpName + N' chờ Ban Giám đốc duyệt.', '/Leave/LeaveApproval', 'LeaveRequest'
        FROM Employees WHERE RoleCode = '1' AND ISNULL(Deleted,0)=0;
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
        IF @LeaveType = 'NP'
        BEGIN
            UPDATE Employees SET UsedLeaveDays = ISNULL(UsedLeaveDays, 0) + @NumDays WHERE Id = @EmpId;
        END

        -- History
        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Agree', @ApproverId, GETDATE(), N'[BGD Approved] ' + @Comment, 3);

        -- Notify Employee
        INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type)
        VALUES (@EmpId, @ApproverId, N'Đơn xin nghỉ phép của bạn đã được duyệt hoàn tất bởi BGĐ.', '/Leave/LeaveRequest', 'LeaveRequest');
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

        -- History
        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Acting', @ApproverId, GETDATE(), N'[HCNS Acting for BGD] ' + @Comment, 4);

        -- Notify Employee
        INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type)
        VALUES (@EmpId, @ApproverId, N'Đơn xin nghỉ phép của bạn đã được HCNS duyệt thay Ban Giám đốc.', '/Leave/LeaveRequest', 'LeaveRequest');
    END
END
GO

-- Create sp_Leave_GetHistory
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Leave_GetHistory]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Leave_GetHistory];
GO

CREATE PROCEDURE [dbo].[sp_Leave_GetHistory]
    @LeaveId int
AS
BEGIN
    SELECT 
        h.*,
        e.FullName as ActionByName
    FROM LeaveHistories h
    JOIN Employees e ON h.ActionBy = e.Id
    WHERE h.LeaveId = @LeaveId
    ORDER BY h.ActionTime DESC;
END
GO

PRINT '✓ Migration V022 completed successfully';
