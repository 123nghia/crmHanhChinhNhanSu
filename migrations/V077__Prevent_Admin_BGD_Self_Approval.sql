-- =============================================
-- Migration: V077__Prevent_Admin_BGD_Self_Approval
-- Description: Prevent Admin/BGD from approving their own leave requests
-- =============================================

PRINT 'Applying migration V077: Prevent Admin/BGD self approval...';

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
    DECLARE @EmpName nvarchar(200);
    DECLARE @ApproverName nvarchar(200);
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

    SELECT
        @EmpName = FullName,
        @RequesterRoleCode = RoleCode
    FROM Employees
    WHERE Id = @EmpId;

    SELECT @ApproverName = FullName FROM Employees WHERE Id = @ApproverId;

    IF @Action = 'Reject'
    BEGIN
        UPDATE LeaveRequests SET
            Status = 5,
            Comment = @Comment,
            ApproverId = @ApproverId,
            ApproveAt = GETDATE(),
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;

        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Reject', @ApproverId, GETDATE(), @Comment, 5);

        INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type)
        VALUES (@EmpId, @ApproverId, N'Don xin nghi phep cua ban da bi tu choi boi ' + ISNULL(@ApproverName, N'') + N'. Ly do: ' + ISNULL(@Comment, N''), '/Leave/LeaveRequest', 'LeaveRequest');

        RETURN;
    END

    IF @CurrentStatus = 0 AND ISNULL(@RequesterRoleCode, '') = '2' AND (@RoleCode = 'TL' OR @RoleCode = '3' OR @RoleCode = '1')
    BEGIN
        UPDATE LeaveRequests SET
            Status = 1,
            LeadApproverId = @ApproverId,
            LeadApproveAt = GETDATE(),
            LeadComment = @Comment,
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;

        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Agree', @ApproverId, GETDATE(), N'[Lead Approved] ' + ISNULL(@Comment, N''), 1);

        INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type)
        SELECT Id, @ApproverId, N'Co don nghi phep cua ' + ISNULL(@EmpName, N'') + N' da duoc Team Lead duyet, cho HCNS xu ly.', '/Leave/LeaveApproval', 'LeaveRequest'
        FROM Employees
        WHERE RoleCode = '9' AND ISNULL(Deleted, 0) = 0;
    END
    ELSE IF @CurrentStatus = 1 AND ISNULL(@RequesterRoleCode, '') = '2' AND (@RoleCode = 'HCNS' OR @RoleCode = '9' OR @RoleCode = '1')
    BEGIN
        UPDATE LeaveRequests SET
            Status = 2,
            HCNSApproverId = @ApproverId,
            HCNSApproveAt = GETDATE(),
            HCNSComment = @Comment,
            UpdatedBy = @ApproverId,
            UpdateAt = GETDATE()
        WHERE Id = @Id;

        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Agree', @ApproverId, GETDATE(), N'[HCNS Approved] ' + ISNULL(@Comment, N''), 2);

        INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type)
        SELECT Id, @ApproverId, N'Co don nghi phep cua ' + ISNULL(@EmpName, N'') + N' cho BGD duyet.', '/Leave/LeaveApproval', 'LeaveRequest'
        FROM Employees
        WHERE RoleCode IN ('1', '8') AND ISNULL(Deleted, 0) = 0;
    END
    ELSE IF @CurrentStatus = 2 AND (@RoleCode = 'BGD' OR @RoleCode = '8' OR @RoleCode = '1')
    BEGIN
        UPDATE LeaveRequests SET
            Status = 3,
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

        INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type)
        VALUES (@EmpId, @ApproverId, N'Don xin nghi phep cua ban da duoc duyet hoan tat boi BGD.', '/Leave/LeaveRequest', 'LeaveRequest');
    END
    ELSE IF ((@CurrentStatus = 1 AND ISNULL(@RequesterRoleCode, '') = '2') OR @CurrentStatus = 2)
        AND @Action = 'Acting'
        AND (@RoleCode = 'HCNS' OR @RoleCode = '9' OR @RoleCode = '1')
    BEGIN
        UPDATE LeaveRequests SET
            Status = 4,
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

        INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type)
        VALUES (@EmpId, @ApproverId, N'Don xin nghi phep cua ban da duoc HCNS duyet thay Ban Giam doc.', '/Leave/LeaveRequest', 'LeaveRequest');
    END
END
GO

PRINT '✓ Migration V077 completed successfully';
