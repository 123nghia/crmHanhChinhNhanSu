PRINT 'Applying migration V096: disable manager CC for leave templates...';
GO

UPDATE EmailTemplates
SET CcManager = 0,
    UpdateAt = GETDATE()
WHERE Code IN
(
    'LEAVE_CREATE',
    'LEAVE_APPROVE',
    'LEAVE_REJECT',
    'LEAVE_PENDING_HCNS',
    'LEAVE_PENDING_BGD'
)
  AND ISNULL(CcManager, 0) <> 0;
GO

PRINT 'Migration V096 applied successfully.';
