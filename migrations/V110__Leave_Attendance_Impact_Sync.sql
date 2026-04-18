PRINT 'Applying migration V110: leave attendance impact sync...';
GO

IF COL_LENGTH('dbo.LeaveRequests', 'AttendanceSyncStatus') IS NULL
BEGIN
    ALTER TABLE dbo.LeaveRequests ADD AttendanceSyncStatus varchar(30) NULL;
END
GO

IF COL_LENGTH('dbo.LeaveRequests', 'LastAttendanceSyncAt') IS NULL
BEGIN
    ALTER TABLE dbo.LeaveRequests ADD LastAttendanceSyncAt datetime NULL;
END
GO

IF COL_LENGTH('dbo.LeaveRequests', 'LastAttendanceSyncError') IS NULL
BEGIN
    ALTER TABLE dbo.LeaveRequests ADD LastAttendanceSyncError nvarchar(1000) NULL;
END
GO

IF COL_LENGTH('dbo.LeaveRequests', 'LastAttendanceSyncRangeFrom') IS NULL
BEGIN
    ALTER TABLE dbo.LeaveRequests ADD LastAttendanceSyncRangeFrom date NULL;
END
GO

IF COL_LENGTH('dbo.LeaveRequests', 'LastAttendanceSyncRangeTo') IS NULL
BEGIN
    ALTER TABLE dbo.LeaveRequests ADD LastAttendanceSyncRangeTo date NULL;
END
GO

IF COL_LENGTH('dbo.LeaveRequests', 'AttendanceSyncAttemptCount') IS NULL
BEGIN
    ALTER TABLE dbo.LeaveRequests
        ADD AttendanceSyncAttemptCount int NOT NULL
            CONSTRAINT DF_LeaveRequests_AttendanceSyncAttemptCount DEFAULT (0);
END
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.EmailTemplates') AND type = N'U')
BEGIN
    UPDATE dbo.EmailTemplates
    SET Body = ISNULL(Body, N'')
        + N'<p>Trang thai hien tai: {{StatusText}}</p>'
        + N'<p>Link he thong: <a href="{{LeaveUrl}}">Mo don nghi phep</a></p>',
        UpdateAt = GETDATE()
    WHERE Code IN ('LEAVE_CREATE', 'LEAVE_APPROVE', 'LEAVE_REJECT', 'LEAVE_PENDING_HCNS', 'LEAVE_PENDING_BGD')
      AND ISNULL(Deleted, 0) = 0
      AND ISNULL(Body, N'') NOT LIKE N'%{{LeaveUrl}}%'
      AND ISNULL(Body, N'') NOT LIKE N'%{{LeaveDetailUrl}}%';
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
    SET XACT_ABORT ON;

    DECLARE @CurrentStatus int;
    DECLARE @NewStatus int;
    DECLARE @EmpId int;
    DECLARE @NumDays decimal(18,2);
    DECLARE @LeaveType varchar(8);
    DECLARE @ApprovedAnnualBefore decimal(18,2) = 0;
    DECLARE @ApprovedAnnualAfter decimal(18,2) = 0;
    DECLARE @UsedDelta decimal(18,2) = 0;
    DECLARE @AppLockResult int;
    DECLARE @LockResource nvarchar(100) = CONCAT(N'LeaveApprove:', @Id);

    BEGIN TRY
        BEGIN TRAN;

        EXEC @AppLockResult = sp_getapplock
            @Resource = @LockResource,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @AppLockResult < 0
        BEGIN
            RAISERROR(N'Could not lock leave request for approval.', 16, 1);
        END

        SELECT
            @CurrentStatus = Status,
            @EmpId = EmployeeId,
            @NumDays = NumDays,
            @LeaveType = LeaveTypeCode
        FROM LeaveRequests WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @Id
          AND ISNULL(Deleted, 0) = 0;

        IF @EmpId IS NULL
        BEGIN
            RAISERROR(N'Leave request not found.', 16, 1);
        END

        IF @EmpId = @ApproverId AND (@RoleCode = '1' OR @RoleCode = '8' OR @RoleCode = 'BGD')
        BEGIN
            RAISERROR(N'Admin/BGD cannot process own leave request.', 16, 1);
        END

        SET @ApprovedAnnualBefore =
            CASE
                WHEN ISNULL(@LeaveType, '') = 'NP' AND ISNULL(@CurrentStatus, -1) IN (3, 4)
                    THEN ISNULL(@NumDays, 0)
                ELSE 0
            END;

        IF @Action = 'Reject'
           AND @CurrentStatus IN (0, 1, 2, 3, 4)
           AND (
                @RoleCode = '1'
                OR (@CurrentStatus = 0 AND @RoleCode IN ('TL', '3'))
                OR (@CurrentStatus = 1 AND @RoleCode IN ('HCNS', '9'))
                OR (@CurrentStatus IN (2, 3, 4) AND @RoleCode IN ('BGD', '8'))
           )
        BEGIN
            UPDATE LeaveRequests
            SET Status = 5,
                Comment = @Comment,
                ApproverId = @ApproverId,
                ApproveAt = GETDATE(),
                UpdatedBy = @ApproverId,
                UpdateAt = GETDATE()
            WHERE Id = @Id
              AND Status = @CurrentStatus
              AND ISNULL(Deleted, 0) = 0;

            IF @@ROWCOUNT = 0
            BEGIN
                RAISERROR(N'Leave status changed before reject.', 16, 1);
            END

            INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
            VALUES (@Id, 'Reject', @ApproverId, GETDATE(), @Comment, 5);

            SET @NewStatus = 5;
        END
        ELSE IF @CurrentStatus = 0 AND (@RoleCode = 'TL' OR @RoleCode = '3' OR @RoleCode = '1')
        BEGIN
            UPDATE LeaveRequests
            SET Status = 1,
                LeadApproverId = @ApproverId,
                LeadApproveAt = GETDATE(),
                LeadComment = @Comment,
                UpdatedBy = @ApproverId,
                UpdateAt = GETDATE()
            WHERE Id = @Id
              AND Status = @CurrentStatus
              AND ISNULL(Deleted, 0) = 0;

            IF @@ROWCOUNT = 0
            BEGIN
                RAISERROR(N'Leave status changed before lead approval.', 16, 1);
            END

            INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
            VALUES (@Id, 'Agree', @ApproverId, GETDATE(), N'[Lead Approved] ' + ISNULL(@Comment, N''), 1);

            SET @NewStatus = 1;
        END
        ELSE IF @CurrentStatus = 1 AND (@RoleCode = 'HCNS' OR @RoleCode = '9' OR @RoleCode = '1')
        BEGIN
            UPDATE LeaveRequests
            SET Status = 2,
                HCNSApproverId = @ApproverId,
                HCNSApproveAt = GETDATE(),
                HCNSComment = @Comment,
                UpdatedBy = @ApproverId,
                UpdateAt = GETDATE()
            WHERE Id = @Id
              AND Status = @CurrentStatus
              AND ISNULL(Deleted, 0) = 0;

            IF @@ROWCOUNT = 0
            BEGIN
                RAISERROR(N'Leave status changed before HCNS approval.', 16, 1);
            END

            INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
            VALUES (@Id, 'Agree', @ApproverId, GETDATE(), N'[HCNS Approved] ' + ISNULL(@Comment, N''), 2);

            SET @NewStatus = 2;
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
            WHERE Id = @Id
              AND Status = @CurrentStatus
              AND ISNULL(Deleted, 0) = 0;

            IF @@ROWCOUNT = 0
            BEGIN
                RAISERROR(N'Leave status changed before BGD approval.', 16, 1);
            END

            INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
            VALUES (@Id, 'Agree', @ApproverId, GETDATE(), N'[BGD Approved] ' + ISNULL(@Comment, N''), 3);

            SET @NewStatus = 3;
        END
        ELSE IF @Action = 'Acting'
            AND @CurrentStatus IN (1, 2)
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
            WHERE Id = @Id
              AND Status = @CurrentStatus
              AND ISNULL(Deleted, 0) = 0;

            IF @@ROWCOUNT = 0
            BEGIN
                RAISERROR(N'Leave status changed before acting approval.', 16, 1);
            END

            INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
            VALUES (@Id, 'Acting', @ApproverId, GETDATE(), N'[HCNS Acting for BGD] ' + ISNULL(@Comment, N''), 4);

            SET @NewStatus = 4;
        END
        ELSE
        BEGIN
            RAISERROR(N'Leave request cannot be processed in current status.', 16, 1);
        END

        SET @ApprovedAnnualAfter =
            CASE
                WHEN ISNULL(@LeaveType, '') = 'NP' AND ISNULL(@NewStatus, -1) IN (3, 4)
                    THEN ISNULL(@NumDays, 0)
                ELSE 0
            END;

        SET @UsedDelta = ISNULL(@ApprovedAnnualAfter, 0) - ISNULL(@ApprovedAnnualBefore, 0);
        IF ISNULL(@UsedDelta, 0) <> 0
        BEGIN
            EXEC sp_Employee_AdjustUsedLeaveDaysDelta @EmpId, @UsedDelta, @ApproverId;
        END

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRAN;
        END

        DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END
GO

PRINT 'Migration V110 completed.';
GO
