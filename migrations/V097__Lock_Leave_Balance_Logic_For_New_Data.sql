PRINT 'Applying migration V097: lock leave balance logic for new data...';
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Employee_AdjustUsedLeaveDaysDelta]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Employee_AdjustUsedLeaveDaysDelta];
GO

CREATE PROCEDURE [dbo].[sp_Employee_AdjustUsedLeaveDaysDelta]
    @EmployeeId int,
    @Delta decimal(18,2),
    @UpdatedBy int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF ISNULL(@EmployeeId, 0) <= 0 OR ISNULL(@Delta, 0) = 0
    BEGIN
        RETURN;
    END

    UPDATE Employees
    SET UsedLeaveDays =
            CASE
                WHEN ISNULL(UsedLeaveDays, 0) + @Delta < 0 THEN 0
                ELSE ISNULL(UsedLeaveDays, 0) + @Delta
            END,
        UpdateAt = GETDATE(),
        UpdatedBy = COALESCE(@UpdatedBy, UpdatedBy)
    WHERE Id = @EmployeeId
      AND ISNULL(Deleted, 0) = 0;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Employee_UpdateLeaveBalance]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Employee_UpdateLeaveBalance];
GO

CREATE PROCEDURE [dbo].[sp_Employee_UpdateLeaveBalance]
(
    @EmployeeId int,
    @AllowedLeaveDays decimal(18,2) = NULL,
    @CarryOverLeaveDays decimal(18,2) = NULL,
    @UsedLeaveDays decimal(18,2) = NULL,
    @ExpiredLeaveDays decimal(18,2) = NULL,
    @UpdatedBy int = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Employees
    SET
        AllowedLeaveDays = COALESCE(@AllowedLeaveDays, AllowedLeaveDays),
        CarryOverLeaveDays = COALESCE(@CarryOverLeaveDays, CarryOverLeaveDays),
        ExpiredLeaveDays = COALESCE(@ExpiredLeaveDays, ExpiredLeaveDays),
        UpdateAt = GETDATE(),
        UpdatedBy = @UpdatedBy
    WHERE Id = @EmployeeId;

    SELECT @@ROWCOUNT AS AffectedRows;
END
GO

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
    SET XACT_ABORT ON;

    DECLARE @NewId int;
    DECLARE @Action nvarchar(50);
    DECLARE @ManagerId int;
    DECLARE @DirectManagerId int;
    DECLARE @GroupId int;
    DECLARE @GroupManagerId int;
    DECLARE @CurrentStatus int;
    DECLARE @EmployeeRoleCode varchar(10);
    DECLARE @DepartmentCode varchar(10);
    DECLARE @EffectiveManagerRoleCode varchar(10);
    DECLARE @InitialStatus int;
    DECLARE @HasDepartment bit = 0;
    DECLARE @IsRecruitmentDepartment bit = 0;
    DECLARE @IsRequesterTeamLead bit = 0;
    DECLARE @DuplicateLeaveId int = NULL;
    DECLARE @NormalizedReason nvarchar(max) = LTRIM(RTRIM(ISNULL(@Reason, N'')));
    DECLARE @EffectiveHandoverEmployeeId int = ISNULL(@HandoverEmployeeId, 0);
    DECLARE @LockResource nvarchar(255);
    DECLARE @AppLockResult int;
    DECLARE @ExistingStatus int = NULL;
    DECLARE @ExistingNumDays decimal(18,2) = 0;
    DECLARE @ExistingLeaveTypeCode varchar(8) = NULL;
    DECLARE @ApprovedAnnualBefore decimal(18,2) = 0;
    DECLARE @ApprovedAnnualAfter decimal(18,2) = 0;
    DECLARE @UsedDelta decimal(18,2) = 0;

    SET @LockResource = CONCAT(
        N'LeaveSave:',
        @EmployeeId,
        N':',
        CONVERT(varchar(64), HASHBYTES(
            'SHA2_256',
            CONCAT(
                ISNULL(@LeaveTypeCode, ''),
                N'|',
                CONVERT(varchar(10), CAST(@FromDate AS date), 120),
                N'|',
                CONVERT(varchar(10), CAST(@ToDate AS date), 120),
                N'|',
                CONVERT(varchar(30), ISNULL(@NumDays, 0)),
                N'|',
                @EffectiveHandoverEmployeeId,
                N'|',
                @NormalizedReason
            )
        ), 2)
    );

    BEGIN TRY
        BEGIN TRAN;

        EXEC @AppLockResult = sp_getapplock
            @Resource = @LockResource,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @AppLockResult < 0
        BEGIN
            ROLLBACK TRAN;
            SELECT -4;
            RETURN;
        END

        SELECT TOP 1
            @DuplicateLeaveId = l.Id
        FROM LeaveRequests l WITH (UPDLOCK, HOLDLOCK)
        WHERE ISNULL(l.Deleted, 0) = 0
          AND l.Status NOT IN (5, 6)
          AND l.EmployeeId = @EmployeeId
          AND l.Id <> ISNULL(@Id, 0)
          AND ISNULL(l.LeaveTypeCode, '') = ISNULL(@LeaveTypeCode, '')
          AND CAST(l.FromDate AS date) = CAST(@FromDate AS date)
          AND CAST(l.ToDate AS date) = CAST(@ToDate AS date)
          AND ISNULL(l.NumDays, 0) = ISNULL(@NumDays, 0)
          AND ISNULL(l.HandoverEmployeeId, 0) = @EffectiveHandoverEmployeeId
          AND LTRIM(RTRIM(ISNULL(l.Reason, N''))) = @NormalizedReason
        ORDER BY
            CASE
                WHEN l.Status IN (3, 4) THEN 3
                WHEN l.Status IN (2, 1, 0) THEN 2
                ELSE 1
            END DESC,
            l.Id DESC;

        IF @DuplicateLeaveId IS NOT NULL
        BEGIN
            ROLLBACK TRAN;
            SELECT -3;
            RETURN;
        END

        SELECT
            @DirectManagerId = ManagerId,
            @EmployeeRoleCode = RoleCode,
            @DepartmentCode = DepartmentCode
        FROM Employees
        WHERE Id = @EmployeeId
          AND ISNULL(Deleted, 0) = 0;

        SELECT TOP 1
            @GroupId = gm.GroupId
        FROM GroupMember gm
        WHERE gm.MemberId = @EmployeeId
          AND ISNULL(gm.Deleted, 0) = 0
        ORDER BY gm.Id DESC;

        IF ISNULL(@GroupId, 0) > 0
        BEGIN
            SELECT
                @GroupManagerId = TRY_CONVERT(int, g.ManagerId)
            FROM [Group] g
            WHERE g.Id = @GroupId
              AND ISNULL(g.Deleted, 0) = 0;
        END

        SET @ManagerId =
            CASE
                WHEN ISNULL(@GroupManagerId, 0) > 0 AND @GroupManagerId <> @EmployeeId THEN @GroupManagerId
                WHEN ISNULL(@DirectManagerId, 0) > 0 AND @DirectManagerId <> @EmployeeId THEN @DirectManagerId
                ELSE NULL
            END;

        SELECT
            @EffectiveManagerRoleCode = RoleCode
        FROM Employees
        WHERE Id = @ManagerId
          AND ISNULL(Deleted, 0) = 0;

        SET @HasDepartment =
            CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM MasterData md
                    WHERE md.TypeData = 5
                      AND md.Code = NULLIF(LTRIM(RTRIM(ISNULL(@DepartmentCode, ''))), '')
                      AND ISNULL(md.Deleted, 0) = 0
                ) THEN 1
                ELSE 0
            END;

        SET @IsRecruitmentDepartment =
            CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM MasterData md
                    WHERE md.TypeData = 5
                      AND md.Code = NULLIF(LTRIM(RTRIM(ISNULL(@DepartmentCode, ''))), '')
                      AND md.Name = N'Tuyển dụng'
                      AND ISNULL(md.Deleted, 0) = 0
                ) THEN 1
                ELSE 0
            END;

        SET @IsRequesterTeamLead =
            CASE
                WHEN ISNULL(@EmployeeRoleCode, '') IN ('3', 'TL') THEN 1
                WHEN EXISTS (
                    SELECT 1
                    FROM [Group] g
                    WHERE TRY_CONVERT(int, g.ManagerId) = @EmployeeId
                      AND ISNULL(g.Deleted, 0) = 0
                ) THEN 1
                WHEN EXISTS (
                    SELECT 1
                    FROM Employees e
                    WHERE e.ManagerId = @EmployeeId
                      AND e.Id <> @EmployeeId
                      AND ISNULL(e.Deleted, 0) = 0
                ) THEN 1
                ELSE 0
            END;

        SET @InitialStatus =
            CASE
                WHEN @HasDepartment = 0 OR @IsRecruitmentDepartment = 0 OR @IsRequesterTeamLead = 1 THEN 2
                WHEN ISNULL(@EffectiveManagerRoleCode, '') IN ('3', 'TL') THEN 0
                ELSE 1
            END;

        IF @Id > 0
        BEGIN
            SELECT
                @ExistingStatus = Status,
                @ExistingNumDays = NumDays,
                @ExistingLeaveTypeCode = LeaveTypeCode
            FROM LeaveRequests
            WHERE Id = @Id
              AND ISNULL(Deleted, 0) = 0;

            SET @ApprovedAnnualBefore =
                CASE
                    WHEN ISNULL(@ExistingLeaveTypeCode, '') = 'NP'
                         AND ISNULL(@ExistingStatus, -1) IN (3, 4)
                        THEN ISNULL(@ExistingNumDays, 0)
                    ELSE 0
                END;

            UPDATE LeaveRequests
            SET LeaveTypeCode = @LeaveTypeCode,
                FromDate = @FromDate,
                ToDate = @ToDate,
                NumDays = @NumDays,
                Reason = @NormalizedReason,
                HandoverEmployeeId = @HandoverEmployeeId,
                UpdatedBy = @UserId,
                UpdateAt = GETDATE()
            WHERE Id = @Id;

            SET @NewId = @Id;
            SET @Action = 'Update';

            SELECT @CurrentStatus = Status
            FROM LeaveRequests
            WHERE Id = @Id;

            SET @ApprovedAnnualAfter =
                CASE
                    WHEN ISNULL(@LeaveTypeCode, '') = 'NP'
                         AND ISNULL(@CurrentStatus, -1) IN (3, 4)
                        THEN ISNULL(@NumDays, 0)
                    ELSE 0
                END;

            SET @UsedDelta = ISNULL(@ApprovedAnnualAfter, 0) - ISNULL(@ApprovedAnnualBefore, 0);
            IF ISNULL(@UsedDelta, 0) <> 0
            BEGIN
                EXEC sp_Employee_AdjustUsedLeaveDaysDelta @EmployeeId, @UsedDelta, @UserId;
            END
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
                @NormalizedReason,
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
        VALUES (@NewId, @Action, @UserId, GETDATE(), @NormalizedReason, @CurrentStatus);

        COMMIT TRAN;
        SELECT @NewId;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity int = ERROR_SEVERITY();
        DECLARE @ErrorState int = ERROR_STATE();

        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRAN;
        END

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
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
    DECLARE @NewStatus int;
    DECLARE @EmpId int;
    DECLARE @NumDays decimal(18,2);
    DECLARE @LeaveType varchar(8);
    DECLARE @ApprovedAnnualBefore decimal(18,2) = 0;
    DECLARE @ApprovedAnnualAfter decimal(18,2) = 0;
    DECLARE @UsedDelta decimal(18,2) = 0;

    SELECT
        @CurrentStatus = Status,
        @EmpId = EmployeeId,
        @NumDays = NumDays,
        @LeaveType = LeaveTypeCode
    FROM LeaveRequests
    WHERE Id = @Id
      AND ISNULL(Deleted, 0) = 0;

    IF @EmpId IS NULL
    BEGIN
        RAISERROR(N'Khong tim thay don nghi phep.', 16, 1);
        RETURN;
    END

    IF @EmpId = @ApproverId AND (@RoleCode = '1' OR @RoleCode = '8' OR @RoleCode = 'BGD')
    BEGIN
        RAISERROR(N'Admin/BGD khong duoc tu xu ly don cua chinh minh.', 16, 1);
        RETURN;
    END

    SET @ApprovedAnnualBefore =
        CASE
            WHEN ISNULL(@LeaveType, '') = 'NP' AND ISNULL(@CurrentStatus, -1) IN (3, 4)
                THEN ISNULL(@NumDays, 0)
            ELSE 0
        END;

    IF @Action = 'Reject' AND @CurrentStatus IN (0, 1, 2)
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
        WHERE Id = @Id;

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
        WHERE Id = @Id;

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
        WHERE Id = @Id;

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
        WHERE Id = @Id;

        INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
        VALUES (@Id, 'Acting', @ApproverId, GETDATE(), N'[HCNS Acting for BGD] ' + ISNULL(@Comment, N''), 4);

        SET @NewStatus = 4;
    END
    ELSE
    BEGIN
        RAISERROR(N'Khong the xu ly don nghi phep o trang thai hien tai.', 16, 1);
        RETURN;
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
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Leave_Delete]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Leave_Delete];
GO

CREATE PROCEDURE [dbo].[sp_Leave_Delete]
    @Id int,
    @UpdatedBy int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EmpId int;
    DECLARE @CurrentStatus int;
    DECLARE @NumDays decimal(18,2);
    DECLARE @LeaveType varchar(8);
    DECLARE @ApprovedAnnualBefore decimal(18,2) = 0;
    DECLARE @ReverseDelta decimal(18,2) = 0;

    SELECT
        @EmpId = EmployeeId,
        @CurrentStatus = Status,
        @NumDays = NumDays,
        @LeaveType = LeaveTypeCode
    FROM LeaveRequests
    WHERE Id = @Id
      AND ISNULL(Deleted, 0) = 0;

    IF @EmpId IS NULL
    BEGIN
        SELECT 0;
        RETURN;
    END

    SET @ApprovedAnnualBefore =
        CASE
            WHEN ISNULL(@LeaveType, '') = 'NP' AND ISNULL(@CurrentStatus, -1) IN (3, 4)
                THEN ISNULL(@NumDays, 0)
            ELSE 0
        END;

    UPDATE LeaveRequests
    SET Deleted = 1,
        UpdateAt = GETDATE(),
        UpdatedBy = @UpdatedBy
    WHERE Id = @Id
      AND ISNULL(Deleted, 0) = 0;

    IF @@ROWCOUNT = 0
    BEGIN
        SELECT 0;
        RETURN;
    END

    IF ISNULL(@ApprovedAnnualBefore, 0) <> 0
    BEGIN
        SET @ReverseDelta = 0 - ISNULL(@ApprovedAnnualBefore, 0);
        EXEC sp_Employee_AdjustUsedLeaveDaysDelta @EmpId, @ReverseDelta, @UpdatedBy;
    END

    INSERT INTO LeaveHistories (LeaveId, Action, ActionBy, ActionTime, Comment, StatusAfter)
    VALUES (@Id, 'Delete', @UpdatedBy, GETDATE(), N'[Deleted]', @CurrentStatus);

    SELECT 1;
END
GO
