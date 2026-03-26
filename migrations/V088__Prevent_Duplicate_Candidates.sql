PRINT 'Applying migration V088: prevent duplicate candidates...';

;WITH CandidateRefs AS
(
    SELECT
        c.Id,
        c.UserName,
        c.Name,
        c.Phone,
        ISNULL(c.Email, '') AS Email,
        ISNULL(c.Position, -1) AS Position,
        ISNULL(c.DepartmentId, -1) AS DepartmentId,
        (
            ISNULL((SELECT COUNT(1) FROM ScheduleInterview s WHERE s.RelId = c.Id AND ISNULL(s.Deleted, 0) = 0), 0) +
            ISNULL((SELECT COUNT(1) FROM DocumentData d WHERE d.RelId = c.Id AND ISNULL(d.Deleted, 0) = 0), 0) +
            ISNULL((SELECT COUNT(1) FROM [Order] o WHERE o.CandidateId = c.Id), 0) +
            ISNULL((SELECT COUNT(1) FROM OnboardMember om WHERE om.CandidateId = c.Id), 0) +
            ISNULL((SELECT COUNT(1) FROM BHXHItem b WHERE b.RelId = c.Id), 0) +
            ISNULL((SELECT COUNT(1) FROM ParrentChild p WHERE p.RelId = c.Id), 0)
        ) AS RefCount
    FROM Candidate c
    WHERE ISNULL(c.Deleted, 0) = 0
      AND NULLIF(LTRIM(RTRIM(c.UserName)), '') IS NOT NULL
),
DuplicateRows AS
(
    SELECT
        Id,
        ROW_NUMBER() OVER (
            PARTITION BY UserName, Name, Phone, Email, Position, DepartmentId
            ORDER BY CASE WHEN RefCount > 0 THEN 0 ELSE 1 END, Id DESC
        ) AS Rn,
        COUNT(1) OVER (
            PARTITION BY UserName, Name, Phone, Email, Position, DepartmentId
        ) AS GroupCount,
        RefCount
    FROM CandidateRefs
)
UPDATE c
SET
    c.Deleted = 1,
    c.UpdateAt = GETDATE()
FROM Candidate c
INNER JOIN DuplicateRows d ON d.Id = c.Id
WHERE d.GroupCount > 1
  AND d.Rn > 1
  AND d.RefCount = 0;
GO

UPDATE Candidate
SET Deleted = 0
WHERE Deleted IS NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Candidate_UserName_Active'
      AND object_id = OBJECT_ID(N'[dbo].[Candidate]')
)
AND NOT EXISTS
(
    SELECT UserName
    FROM Candidate
    WHERE ISNULL(Deleted, 0) = 0
      AND NULLIF(LTRIM(RTRIM(UserName)), '') IS NOT NULL
    GROUP BY UserName
    HAVING COUNT(1) > 1
)
BEGIN
    CREATE UNIQUE INDEX [UX_Candidate_UserName_Active]
        ON [dbo].[Candidate]([UserName])
        WHERE [Deleted] = 0
          AND [UserName] IS NOT NULL
          AND [UserName] <> '';
END
ELSE
BEGIN
    PRINT 'Skipping UX_Candidate_UserName_Active because duplicate usernames still exist or index is already present.';
END
GO

PRINT 'Migration V088 completed successfully.';
