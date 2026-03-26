-- =============================================
-- Migration: V049__Add_ScheduleInterview_GetById
-- Author: System
-- Date: 2026-01-16
-- Description: Add sp_ScheduleInterview_GetById stored procedure
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V049: Add sp_ScheduleInterview_GetById...';

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
    LEFT JOIN Candidates c ON s.RelId = c.Id AND s.RelCode = ''CAND''
    LEFT JOIN Employees e ON s.InterviewerId = e.Id
    WHERE s.Id = @Id AND ISNULL(s.Deleted, 0) = 0;
END
');

PRINT 'Migration V049 completed successfully.';

COMMIT TRANSACTION;
