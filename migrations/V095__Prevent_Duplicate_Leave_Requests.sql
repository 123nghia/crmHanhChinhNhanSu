-- =============================================
-- Migration: V095__Prevent_Duplicate_Leave_Requests
-- Description:
--   Prevent duplicate leave requests caused by repeated save clicks
--   or concurrent requests by adding an app lock + duplicate check
--   inside sp_Leave_Save.
-- =============================================

PRINT 'Applying migration V095: prevent duplicate leave requests...';
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

PRINT 'Migration V095 applied successfully.';
