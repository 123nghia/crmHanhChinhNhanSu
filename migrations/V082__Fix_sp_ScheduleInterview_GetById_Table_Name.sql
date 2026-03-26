-- =============================================
-- Migration: V082__Fix_sp_ScheduleInterview_GetById_Table_Name
-- Author: Codex
-- Date: 2026-03-24
-- Description: Fix sp_ScheduleInterview_GetById to join Candidate instead of Candidates
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V082: Fix sp_ScheduleInterview_GetById table name...';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ScheduleInterview_GetById]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_ScheduleInterview_GetById];

EXEC(N'
CREATE PROCEDURE [dbo].[sp_ScheduleInterview_GetById]
(
    @Id int
)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT s.*,
           c.Name as CandidateName,
           e.FullName as InterviewerName
    FROM ScheduleInterview s
    LEFT JOIN Candidate c ON s.RelId = c.Id AND s.RelCode = ''CAND''
    LEFT JOIN Employees e ON s.InterviewerId = e.Id
    WHERE s.Id = @Id AND ISNULL(s.Deleted, 0) = 0;
END
');

PRINT 'Migration V082 completed successfully.';

COMMIT TRANSACTION;
