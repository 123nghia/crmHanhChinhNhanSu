-- =============================================
-- Migration: V101__Refine_Late_Early_Routing
-- Description:
--   Update late/early workflow so that:
--   - Recruitment staff who are not managers go through Manager -> HCNS -> final.
--   - All other employees (including managers) go directly to BGD -> final.
--   Existing pending requests are aligned to the new routing.
-- =============================================

PRINT 'Applying migration V101: refine late/early routing...';
GO

IF OBJECT_ID(N'[dbo].[LateEarlyRequests]', N'U') IS NULL
BEGIN
    PRINT 'LateEarlyRequests table not found. Skipping V101.';
    RETURN;
END
GO

IF OBJECT_ID('tempdb..#LateEarlyRouting') IS NOT NULL
    DROP TABLE #LateEarlyRouting;
GO

CREATE TABLE #LateEarlyRouting
(
    RequestId INT NOT NULL PRIMARY KEY,
    CurrentStatus INT NOT NULL,
    TargetStatus INT NOT NULL
);
GO

;WITH Routing AS
(
    SELECT
        r.Id AS RequestId,
        r.Status AS CurrentStatus,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(ISNULL(e.DepartmentCode, ''))), '') IS NULL
                 OR NOT EXISTS
                 (
                     SELECT 1
                     FROM MasterData md
                     WHERE md.TypeData = 5
                       AND md.Code = e.DepartmentCode
                       AND md.Name = N'Tuyển dụng'
                       AND ISNULL(md.Deleted, 0) = 0
                 )
                 OR ISNULL(e.RoleCode, '') IN ('3', 'TL')
                 OR EXISTS
                 (
                     SELECT 1
                     FROM [Group] g
                     WHERE TRY_CONVERT(int, g.ManagerId) = e.Id
                       AND ISNULL(g.Deleted, 0) = 0
                 )
                 OR EXISTS
                 (
                     SELECT 1
                     FROM Employees subordinate
                     WHERE subordinate.ManagerId = e.Id
                       AND subordinate.Id <> e.Id
                       AND ISNULL(subordinate.Deleted, 0) = 0
                 )
                THEN 1
            ELSE 0
        END AS IsDirectBgdFlow
    FROM LateEarlyRequests r
    JOIN Employees e
        ON e.Id = r.EmployeeId
       AND ISNULL(e.Deleted, 0) = 0
    WHERE ISNULL(r.Deleted, 0) = 0
      AND r.Status IN (0, 1, 2, 3)
)
INSERT INTO #LateEarlyRouting (RequestId, CurrentStatus, TargetStatus)
SELECT
    RequestId,
    CurrentStatus,
    CASE
        WHEN CurrentStatus = 3 THEN 4
        WHEN IsDirectBgdFlow = 1 THEN 2
        WHEN CurrentStatus >= 2 THEN 4
        ELSE CurrentStatus
    END AS TargetStatus
FROM Routing
WHERE
    CASE
        WHEN CurrentStatus = 3 THEN 4
        WHEN IsDirectBgdFlow = 1 THEN 2
        WHEN CurrentStatus >= 2 THEN 4
        ELSE CurrentStatus
    END <> CurrentStatus;
GO

UPDATE r
SET
    r.Status = route.TargetStatus,
    r.ApproverId = CASE
        WHEN route.CurrentStatus = 2 AND route.TargetStatus = 4 THEN ISNULL(r.HCNSApproverId, r.ApproverId)
        WHEN route.CurrentStatus = 3 AND route.TargetStatus = 4 THEN ISNULL(r.BGDApproverId, r.ApproverId)
        ELSE r.ApproverId
    END,
    r.ApproveAt = CASE
        WHEN route.CurrentStatus = 2 AND route.TargetStatus = 4 THEN ISNULL(r.HCNSApproveAt, ISNULL(r.ApproveAt, GETDATE()))
        WHEN route.CurrentStatus = 3 AND route.TargetStatus = 4 THEN ISNULL(r.BGDApproveAt, ISNULL(r.ApproveAt, GETDATE()))
        ELSE r.ApproveAt
    END,
    r.Comment = CASE
        WHEN route.CurrentStatus = 2
             AND route.TargetStatus = 4
             AND NULLIF(LTRIM(RTRIM(ISNULL(r.Comment, ''))), '') IS NULL
            THEN r.HCNSComment
        WHEN route.CurrentStatus = 3
             AND route.TargetStatus = 4
             AND NULLIF(LTRIM(RTRIM(ISNULL(r.Comment, ''))), '') IS NULL
            THEN r.BGDComment
        ELSE r.Comment
    END,
    r.UpdateAt = GETDATE(),
    r.UpdatedBy = CASE
        WHEN route.CurrentStatus = 2 AND route.TargetStatus = 4 THEN ISNULL(r.HCNSApproverId, ISNULL(r.UpdatedBy, r.CreatedBy))
        WHEN route.CurrentStatus = 3 AND route.TargetStatus = 4 THEN ISNULL(r.BGDApproverId, ISNULL(r.UpdatedBy, r.CreatedBy))
        ELSE ISNULL(r.UpdatedBy, r.CreatedBy)
    END
FROM LateEarlyRequests r
JOIN #LateEarlyRouting route
    ON route.RequestId = r.Id;
GO

INSERT INTO LateEarlyHistory
(
    RequestId,
    Action,
    ActionBy,
    ActionTime,
    Comment,
    StatusAfter,
    CreateAt,
    CreatedBy,
    UpdateAt,
    UpdatedBy,
    Deleted
)
SELECT
    r.Id,
    'SystemRouteUpdate',
    ISNULL(r.UpdatedBy, ISNULL(r.CreatedBy, 0)),
    GETDATE(),
    CASE
        WHEN route.CurrentStatus IN (0, 1) AND route.TargetStatus = 2
            THEN N'[Migration V101] Chuyển đơn sang chờ BGĐ duyệt theo luồng mới.'
        WHEN route.CurrentStatus = 2 AND route.TargetStatus = 4
            THEN N'[Migration V101] Hoàn tất duyệt tại HCNS theo luồng mới.'
        WHEN route.CurrentStatus = 3 AND route.TargetStatus = 4
            THEN N'[Migration V101] Hoàn tất duyệt tại BGĐ theo luồng mới.'
        ELSE N'[Migration V101] Đồng bộ trạng thái theo luồng mới.'
    END,
    route.TargetStatus,
    GETDATE(),
    ISNULL(r.UpdatedBy, ISNULL(r.CreatedBy, 0)),
    GETDATE(),
    ISNULL(r.UpdatedBy, ISNULL(r.CreatedBy, 0)),
    0
FROM LateEarlyRequests r
JOIN #LateEarlyRouting route
    ON route.RequestId = r.Id;
GO

DROP TABLE #LateEarlyRouting;
GO

PRINT 'Migration V101 applied successfully.';
