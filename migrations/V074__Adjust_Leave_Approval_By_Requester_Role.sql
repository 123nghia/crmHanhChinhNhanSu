-- =============================================
-- Migration: V074__Adjust_Leave_Approval_By_Requester_Role
-- Author: System
-- Description: Adjust leave approval flow by requester role (TC = 4-level, management = BGD direct)
-- =============================================

PRINT 'Applying migration V074: Adjust Leave Approval By Requester Role...';

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

        SELECT @CurrentStatus = Status FROM LeaveRequests WHERE Id = @Id;
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

    IF @Action = 'Create'
    BEGIN
        IF @InitialStatus = 0 AND @ManagerId IS NOT NULL
        BEGIN
            INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type)
            VALUES (@ManagerId, @EmployeeId, N'Nhân viên ' + ISNULL(@EmpName, N'') + N' đã gửi đơn xin nghỉ phép chờ bạn duyệt.', '/Leave/LeaveApproval', 'LeaveRequest');
        END
        ELSE IF @InitialStatus = 2
        BEGIN
            INSERT INTO AppNotifications (ReceiverId, SenderId, Message, Link, Type)
            SELECT Id, @EmployeeId, N'Có đơn nghỉ phép của ' + ISNULL(@EmpName, N'') + N' chờ BGĐ duyệt.', '/Leave/LeaveApproval', 'LeaveRequest'
            FROM Employees
            WHERE RoleCode IN ('1', '8') AND ISNULL(Deleted, 0) = 0;
        END
    END

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
        VALUES (@EmpId, @ApproverId, N'Đơn xin nghỉ phép của bạn đã bị từ chối bởi ' + ISNULL(@ApproverName, N'') + N'. Lý do: ' + ISNULL(@Comment, N''), '/Leave/LeaveRequest', 'LeaveRequest');

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
        SELECT Id, @ApproverId, N'Có đơn nghỉ phép của ' + ISNULL(@EmpName, N'') + N' đã được Team Lead duyệt, chờ HCNS xử lý.', '/Leave/LeaveApproval', 'LeaveRequest'
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
        SELECT Id, @ApproverId, N'Có đơn nghỉ phép của ' + ISNULL(@EmpName, N'') + N' chờ BGĐ duyệt.', '/Leave/LeaveApproval', 'LeaveRequest'
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
        VALUES (@EmpId, @ApproverId, N'Đơn xin nghỉ phép của bạn đã được duyệt hoàn tất bởi BGĐ.', '/Leave/LeaveRequest', 'LeaveRequest');
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
        VALUES (@EmpId, @ApproverId, N'Đơn xin nghỉ phép của bạn đã được HCNS duyệt thay Ban Giám đốc.', '/Leave/LeaveRequest', 'LeaveRequest');
    END
END
GO

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
            OR ISNULL(@roleCode, '') IN ('1', '8', '9')
            OR l.EmployeeId IN (SELECT id FROM getAllUserByUserId(@UserId))
          )
    ORDER BY l.CreateAt DESC
    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
END
GO

PRINT '✓ Migration V074 completed successfully';