-- =============================================
-- Migration: V079__Move_Leave_Notifications_To_Business
-- Description: Remove leave app notifications from SQL procedures and move them to business layer
-- =============================================

PRINT 'Applying migration V079: Move leave notifications to business layer...';

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
    DECLARE @EmployeeRoleCode varchar(10);
    DECLARE @InitialStatus int;
    DECLARE @CurrentStatus int;

    SELECT
        @EmpName = FullName,
        @ManagerId = ManagerId,
        @EmployeeRoleCode = RoleCode
    FROM Employees
    WHERE Id = @EmployeeId;

    SET @InitialStatus = CASE WHEN ISNULL(@EmployeeRoleCode, '') = '2' THEN 0 ELSE 2 END;

    IF @Id > 0
    BEGIN
        UPDATE LeaveRequests
        SET LeaveTypeCode = @LeaveTypeCode,
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

        SELECT @CurrentStatus = Status
        FROM LeaveRequests
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO LeaveRequests
        (
            EmployeeId,
            LeaveTypeCode,
            FromDate,
            ToDate,
            NumDays,
            Reason,
            HandoverEmployeeId,
            Status,
            CreatedBy,
            CreateAt,
            UpdatedBy,
            UpdateAt,
            Deleted
        )
        VALUES
        (
            @EmployeeId,
            @LeaveTypeCode,
            @FromDate,
            @ToDate,
            @NumDays,
            @Reason,
            @HandoverEmployeeId,
            @InitialStatus,
            @UserId,
            GETDATE(),
            @UserId,
            GETDATE(),
            0
        );

        SET @NewId = SCOPE_IDENTITY();
        SET @Action = 'Create';
        SET @CurrentStatus = @InitialStatus;
    END

    INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
    VALUES (@NewId, @Action, @UserId, GETDATE(), @Reason, @CurrentStatus);

    SELECT @NewId;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Leave_Approve_Workflow]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Leave_Approve_Workflow];
GO

CREATE PROCEDURE [dbo].[sp_Leave_Approve_Workflow]
    @Id int,
    @Action nvarchar(20),
    @ApproverId int,
    @RoleCode nvarchar(50),
    @Comment nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentStatus int;
    DECLARE @EmpId int;
    DECLARE @NumDays decimal(18,2);
    DECLARE @LeaveType varchar(8);
    DECLARE @RequesterRoleCode varchar(10);

    SELECT
        @CurrentStatus = Status,
        @EmpId = EmployeeId,
        @NumDays = NumDays,
        @LeaveType = LeaveTypeCode
    FROM LeaveRequests
    WHERE Id = @Id;

    IF @EmpId = @ApproverId AND (@RoleCode = '1' OR @RoleCode = '8' OR @RoleCode = 'BGD')
    BEGIN
        RAISERROR(N'Admin/BGD khong duoc tu xu ly don cua chinh minh.', 16, 1);
        RETURN;
    END

    SELECT @RequesterRoleCode = RoleCode
    FROM Employees
    WHERE Id = @EmpId;

    IF @Action = 'Reject'
    BEGIN
        UPDATE LeaveRequests
        SET Status = 5,
            Comment = @Comment,
            ApproverId = @ApproverId,
            ApproveAt = GETDATE(),
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;

        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Reject', @ApproverId, GETDATE(), @Comment, 5);

        RETURN;
    END

    IF @CurrentStatus = 0 AND ISNULL(@RequesterRoleCode, '') = '2' AND (@RoleCode = 'TL' OR @RoleCode = '3' OR @RoleCode = '1')
    BEGIN
        UPDATE LeaveRequests
        SET Status = 1,
            LeadApproverId = @ApproverId,
            LeadApproveAt = GETDATE(),
            LeadComment = @Comment,
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;

        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Agree', @ApproverId, GETDATE(), N'[Lead Approved] ' + ISNULL(@Comment, N''), 1);
    END
    ELSE IF @CurrentStatus = 1 AND ISNULL(@RequesterRoleCode, '') = '2' AND (@RoleCode = 'HCNS' OR @RoleCode = '9' OR @RoleCode = '1')
    BEGIN
        UPDATE LeaveRequests
        SET Status = 2,
            HCNSApproverId = @ApproverId,
            HCNSApproveAt = GETDATE(),
            HCNSComment = @Comment,
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;

        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Agree', @ApproverId, GETDATE(), N'[HCNS Approved] ' + ISNULL(@Comment, N''), 2);
    END
    ELSE IF @CurrentStatus = 2 AND (@RoleCode = 'BGD' OR @RoleCode = '8' OR @RoleCode = '1')
    BEGIN
        UPDATE LeaveRequests
        SET Status = 3,
            BGDApproverId = @ApproverId,
            BGDApproveAt = GETDATE(),
            BGDComment = @Comment,
            ApproverId = @ApproverId,
            ApproveAt = GETDATE(),
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;

        IF @LeaveType = 'NP'
        BEGIN
            UPDATE Employees
            SET UsedLeaveDays = ISNULL(UsedLeaveDays, 0) + @NumDays
            WHERE Id = @EmpId;
        END

        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Agree', @ApproverId, GETDATE(), N'[BGD Approved] ' + ISNULL(@Comment, N''), 3);
    END
    ELSE IF ((@CurrentStatus = 1 AND ISNULL(@RequesterRoleCode, '') = '2') OR @CurrentStatus = 2)
        AND @Action = 'Acting'
        AND (@RoleCode = 'HCNS' OR @RoleCode = '9' OR @RoleCode = '1')
    BEGIN
        UPDATE LeaveRequests
        SET Status = 4,
            IsActingApproval = 1,
            HCNSApproverId = @ApproverId,
            HCNSApproveAt = GETDATE(),
            HCNSComment = N'[ACTING AS BGD] ' + ISNULL(@Comment, N''),
            ApproverId = @ApproverId,
            ApproveAt = GETDATE(),
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;

        IF @LeaveType = 'NP'
        BEGIN
            UPDATE Employees
            SET UsedLeaveDays = ISNULL(UsedLeaveDays, 0) + @NumDays
            WHERE Id = @EmpId;
        END

        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Acting', @ApproverId, GETDATE(), N'[HCNS Acting for BGD] ' + ISNULL(@Comment, N''), 4);
    END
END
GO

PRINT 'V079 completed successfully';
