-- =============================================
-- Migration: V041__Backfill_Candidate_Username
-- Author: System
-- Date: 2026-01-16
-- Description: Backfill UserName for existing candidates using their Code
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V041: Backfill Candidate Username...';

-- Update UserName with Code for candidates who have no UserName
UPDATE [dbo].[Candidate]
SET [UserName] = [Code]
WHERE ([UserName] IS NULL OR LTRIM(RTRIM([UserName])) = '')
  AND [Code] IS NOT NULL;

PRINT '  + Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' candidate records.';

PRINT 'Migration V041 completed successfully';

COMMIT TRANSACTION;
