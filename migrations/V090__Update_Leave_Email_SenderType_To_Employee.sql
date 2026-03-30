PRINT 'Applying migration V090: update leave email sender type to employee...';

UPDATE dbo.EmailTemplates
SET SenderType = 'EMPLOYEE',
    UpdateAt = GETDATE()
WHERE Code IN ('LEAVE_APPROVE', 'LEAVE_REJECT', 'LEAVE_PENDING_HCNS', 'LEAVE_PENDING_BGD')
  AND ISNULL(Deleted, 0) = 0;

PRINT 'Migration V090 completed successfully.';
