-- =============================================
-- Migration: V092__Skip_Lead_Status_When_Lead_Is_HCNS
-- Description: When the first approver is HCNS, new leave requests should start at Pending HCNS instead of Pending Lead.
--              Also normalize existing pending leave requests that are currently waiting for Lead but whose effective lead is HCNS.
-- =============================================

PRINT 'Applying migration V092: Skip lead status when lead is HCNS...';

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
    DECLARE @DirectManagerId int;
    DECLARE @GroupId int;
    DECLARE @GroupManagerId int;
    DECLARE @EmpName nvarchar(200);
    DECLARE @EmployeeRoleCode varchar(10);
    DECLARE @EffectiveManagerRoleCode varchar(10);
    DECLARE @InitialStatus int;
    DECLARE @CurrentStatus int;

    SELECT
        @EmpName = FullName,
        @DirectManagerId = ManagerId,
        @EmployeeRoleCode = RoleCode
    FROM Employees
    WHERE Id = @EmployeeId;

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

    SET @InitialStatus =
        CASE
            WHEN ISNULL(@EmployeeRoleCode, '') <> '2' THEN 2
            WHEN ISNULL(@EffectiveManagerRoleCode, '') = '9' THEN 1
            ELSE 0
        END;

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

;WITH EffectiveLead AS
(
    SELECT
        l.Id AS LeaveId,
        e.RoleCode AS EmployeeRoleCode,
        COALESCE(
            CASE
                WHEN TRY_CONVERT(int, g.ManagerId) > 0 AND TRY_CONVERT(int, g.ManagerId) <> e.Id
                    THEN TRY_CONVERT(int, g.ManagerId)
            END,
            CASE
                WHEN ISNULL(e.ManagerId, 0) > 0 AND e.ManagerId <> e.Id
                    THEN e.ManagerId
            END
        ) AS EffectiveLeadId
    FROM LeaveRequests l
    JOIN Employees e
        ON e.Id = l.EmployeeId
       AND ISNULL(e.Deleted, 0) = 0
    OUTER APPLY
    (
        SELECT TOP 1 gm.GroupId
        FROM GroupMember gm
        WHERE gm.MemberId = e.Id
          AND ISNULL(gm.Deleted, 0) = 0
        ORDER BY gm.Id DESC
    ) gm
    LEFT JOIN [Group] g
        ON g.Id = gm.GroupId
       AND ISNULL(g.Deleted, 0) = 0
    WHERE ISNULL(l.Deleted, 0) = 0
      AND l.Status = 0
)
UPDATE l
SET l.Status = 1,
    l.UpdateAt = GETDATE()
FROM LeaveRequests l
JOIN EffectiveLead el
    ON el.LeaveId = l.Id
JOIN Employees leadEmp
    ON leadEmp.Id = el.EffectiveLeadId
   AND ISNULL(leadEmp.Deleted, 0) = 0
WHERE el.EmployeeRoleCode = '2'
  AND leadEmp.RoleCode = '9';

UPDATE h
SET h.StatusAfter = 1
FROM LeaveHistories h
JOIN LeaveRequests l
    ON l.Id = h.LeaveId
WHERE ISNULL(l.Deleted, 0) = 0
  AND l.Status = 1
  AND h.Action = 'Create'
  AND h.StatusAfter = 0
  AND NOT EXISTS
  (
      SELECT 1
      FROM LeaveHistories h2
      WHERE h2.LeaveId = h.LeaveId
        AND h2.Id > h.Id
  );
GO

PRINT 'Migration V092 applied successfully.';
