-- =============================================
-- Migration: V093__Add_Departments_And_Department_Based_Leave_Routing
-- Description:
--   1. Seed default departments for employee administration.
--   2. Default existing employees without department to Recruitment.
--   3. Update leave routing:
--      - Employees outside department "Tuyển dụng" go directly to BGD.
--      - Team leads / group leads go directly to BGD.
--      - HR is only copied for direct-to-BGD flow.
--      - Recruitment staff go to TL first only when their effective manager is TL.
--        Otherwise they start from HCNS.
-- =============================================

PRINT 'Applying migration V093: Add departments and department-based leave routing...';

IF NOT EXISTS (
    SELECT 1
    FROM MasterData
    WHERE TypeData = 5
      AND Name = N'Tuyển dụng'
      AND ISNULL(Deleted, 0) = 0
)
BEGIN
    EXEC [dbo].[sp_masterData_insert]
        @Name = N'Tuyển dụng',
        @Noted = N'',
        @TypeData = 5,
        @ApplyFor = 2,
        @CreatedBy = '1';
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM MasterData
    WHERE TypeData = 5
      AND Name IN (N'Hành Chính - Nhân Sự', N'Hành Chính _ Nhân sự', N'Hành Chính _ Nhân Sự')
      AND ISNULL(Deleted, 0) = 0
)
BEGIN
    EXEC [dbo].[sp_masterData_insert]
        @Name = N'Hành Chính - Nhân Sự',
        @Noted = N'',
        @TypeData = 5,
        @ApplyFor = 2,
        @CreatedBy = '1';
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM MasterData
    WHERE TypeData = 5
      AND Name = N'IT'
      AND ISNULL(Deleted, 0) = 0
)
BEGIN
    EXEC [dbo].[sp_masterData_insert]
        @Name = N'IT',
        @Noted = N'',
        @TypeData = 5,
        @ApplyFor = 2,
        @CreatedBy = '1';
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM MasterData
    WHERE TypeData = 5
      AND Name IN (N'BGĐ', N'BGD')
      AND ISNULL(Deleted, 0) = 0
)
BEGIN
    EXEC [dbo].[sp_masterData_insert]
        @Name = N'BGĐ',
        @Noted = N'',
        @TypeData = 5,
        @ApplyFor = 2,
        @CreatedBy = '1';
END
GO

DECLARE @RecruitmentDepartmentCode VARCHAR(4);

SELECT TOP 1
    @RecruitmentDepartmentCode = Code
FROM MasterData
WHERE TypeData = 5
  AND Name = N'Tuyển dụng'
  AND ISNULL(Deleted, 0) = 0
ORDER BY Id;

IF NULLIF(LTRIM(RTRIM(ISNULL(@RecruitmentDepartmentCode, ''))), '') IS NOT NULL
BEGIN
    UPDATE e
    SET DepartmentCode = @RecruitmentDepartmentCode,
        UpdateAt = GETDATE()
    FROM Employees e
    WHERE ISNULL(e.Deleted, 0) = 0
      AND (
            NULLIF(LTRIM(RTRIM(ISNULL(e.DepartmentCode, ''))), '') IS NULL
            OR NOT EXISTS
            (
                SELECT 1
                FROM MasterData md
                WHERE md.TypeData = 5
                  AND md.Code = e.DepartmentCode
                  AND ISNULL(md.Deleted, 0) = 0
            )
      );
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

    IF @CurrentStatus = 0 AND (@RoleCode = 'TL' OR @RoleCode = '3' OR @RoleCode = '1')
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

;WITH EffectiveRouting AS
(
    SELECT
        l.Id AS LeaveId,
        l.Status AS CurrentStatus,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(ISNULL(e.DepartmentCode, ''))), '') IS NULL
                 OR NOT EXISTS (
                    SELECT 1
                    FROM MasterData md
                    WHERE md.TypeData = 5
                      AND md.Code = e.DepartmentCode
                      AND ISNULL(md.Deleted, 0) = 0
                 )
                 OR NOT EXISTS (
                    SELECT 1
                    FROM MasterData md
                    WHERE md.TypeData = 5
                      AND md.Code = e.DepartmentCode
                      AND md.Name = N'Tuyển dụng'
                      AND ISNULL(md.Deleted, 0) = 0
                 )
                 OR ISNULL(e.RoleCode, '') IN ('3', 'TL')
                 OR EXISTS (
                    SELECT 1
                    FROM [Group] g2
                    WHERE TRY_CONVERT(int, g2.ManagerId) = e.Id
                      AND ISNULL(g2.Deleted, 0) = 0
                 )
                 OR EXISTS (
                    SELECT 1
                    FROM Employees subordinate
                    WHERE subordinate.ManagerId = e.Id
                      AND subordinate.Id <> e.Id
                      AND ISNULL(subordinate.Deleted, 0) = 0
                 )
                THEN 2
            WHEN ISNULL(managerEmp.RoleCode, '') IN ('3', 'TL')
                THEN 0
            ELSE 1
        END AS TargetStatus
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
    OUTER APPLY
    (
        SELECT
            CASE
                WHEN TRY_CONVERT(int, g.ManagerId) > 0 AND TRY_CONVERT(int, g.ManagerId) <> e.Id
                    THEN TRY_CONVERT(int, g.ManagerId)
                WHEN ISNULL(e.ManagerId, 0) > 0 AND e.ManagerId <> e.Id
                    THEN e.ManagerId
                ELSE NULL
            END AS EffectiveManagerId
    ) managerInfo
    LEFT JOIN Employees managerEmp
        ON managerEmp.Id = managerInfo.EffectiveManagerId
       AND ISNULL(managerEmp.Deleted, 0) = 0
    WHERE ISNULL(l.Deleted, 0) = 0
      AND l.Status IN (0, 1)
)
UPDATE l
SET l.Status = er.TargetStatus,
    l.UpdateAt = GETDATE()
FROM LeaveRequests l
JOIN EffectiveRouting er
    ON er.LeaveId = l.Id
WHERE er.TargetStatus > er.CurrentStatus;
GO

;WITH EffectiveRouting AS
(
    SELECT
        l.Id AS LeaveId,
        l.Status AS CurrentStatus,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(ISNULL(e.DepartmentCode, ''))), '') IS NULL
                 OR NOT EXISTS (
                    SELECT 1
                    FROM MasterData md
                    WHERE md.TypeData = 5
                      AND md.Code = e.DepartmentCode
                      AND ISNULL(md.Deleted, 0) = 0
                 )
                 OR NOT EXISTS (
                    SELECT 1
                    FROM MasterData md
                    WHERE md.TypeData = 5
                      AND md.Code = e.DepartmentCode
                      AND md.Name = N'Tuyển dụng'
                      AND ISNULL(md.Deleted, 0) = 0
                 )
                 OR ISNULL(e.RoleCode, '') IN ('3', 'TL')
                 OR EXISTS (
                    SELECT 1
                    FROM [Group] g2
                    WHERE TRY_CONVERT(int, g2.ManagerId) = e.Id
                      AND ISNULL(g2.Deleted, 0) = 0
                 )
                 OR EXISTS (
                    SELECT 1
                    FROM Employees subordinate
                    WHERE subordinate.ManagerId = e.Id
                      AND subordinate.Id <> e.Id
                      AND ISNULL(subordinate.Deleted, 0) = 0
                 )
                THEN 2
            WHEN ISNULL(managerEmp.RoleCode, '') IN ('3', 'TL')
                THEN 0
            ELSE 1
        END AS TargetStatus
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
    OUTER APPLY
    (
        SELECT
            CASE
                WHEN TRY_CONVERT(int, g.ManagerId) > 0 AND TRY_CONVERT(int, g.ManagerId) <> e.Id
                    THEN TRY_CONVERT(int, g.ManagerId)
                WHEN ISNULL(e.ManagerId, 0) > 0 AND e.ManagerId <> e.Id
                    THEN e.ManagerId
                ELSE NULL
            END AS EffectiveManagerId
    ) managerInfo
    LEFT JOIN Employees managerEmp
        ON managerEmp.Id = managerInfo.EffectiveManagerId
       AND ISNULL(managerEmp.Deleted, 0) = 0
    WHERE ISNULL(l.Deleted, 0) = 0
      AND l.Status IN (1, 2)
)
UPDATE h
SET h.StatusAfter = er.TargetStatus
FROM LeaveHistories h
JOIN EffectiveRouting er
    ON er.LeaveId = h.LeaveId
WHERE h.Action = 'Create'
  AND h.StatusAfter < er.TargetStatus
  AND NOT EXISTS
  (
      SELECT 1
      FROM LeaveHistories h2
      WHERE h2.LeaveId = h.LeaveId
        AND h2.Id > h.Id
  );
GO

PRINT 'Migration V093 applied successfully.';
