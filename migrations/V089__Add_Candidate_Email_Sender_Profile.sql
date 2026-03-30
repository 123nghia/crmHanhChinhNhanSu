-- =============================================
-- Migration: V089__Add_Candidate_Email_Sender_Profile
-- Description: Add dedicated sender profile for candidate emails
-- =============================================

PRINT 'Applying migration V089: add candidate email sender profile...';

IF COL_LENGTH('dbo.EmailSettings', 'CandidateFromEmail') IS NULL
BEGIN
    ALTER TABLE dbo.EmailSettings ADD CandidateFromEmail nvarchar(200) NULL;
END
GO

IF COL_LENGTH('dbo.EmailSettings', 'CandidateFromName') IS NULL
BEGIN
    ALTER TABLE dbo.EmailSettings ADD CandidateFromName nvarchar(200) NULL;
END
GO

IF COL_LENGTH('dbo.EmailSettings', 'CandidateSignature') IS NULL
BEGIN
    ALTER TABLE dbo.EmailSettings ADD CandidateSignature nvarchar(max) NULL;
END
GO

UPDATE dbo.EmailSettings
SET CandidateFromEmail = ISNULL(NULLIF(CandidateFromEmail, ''), ISNULL(NULLIF(HrFromEmail, ''), FromEmail)),
    CandidateFromName = ISNULL(NULLIF(CandidateFromName, ''), ISNULL(NULLIF(HrFromName, ''), FromName)),
    CandidateSignature = ISNULL(NULLIF(CandidateSignature, ''), HrSignature)
WHERE ISNULL(Deleted, 0) = 0;
GO

UPDATE dbo.EmailTemplates
SET SenderType = 'CANDIDATE'
WHERE Code IN ('INTERVIEW_SCHEDULE')
  AND ISNULL(Deleted, 0) = 0;
GO

PRINT 'Migration V089 completed successfully.';
