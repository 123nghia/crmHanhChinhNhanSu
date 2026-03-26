-- =============================================
-- Migration: V063__Add_HCNS_Role_Permissions
-- Description: Seed RoleAllow for HCNS role (default same as Admin)
-- =============================================

PRINT 'Applying migration V063: Seed HCNS role permissions...';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type in (N'U'))
BEGIN
    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '9', r.PageCode, r.IsView, r.IsAdd, r.IsEdit, r.IsDelete, r.IsApprove
    FROM RoleAllow r
    WHERE r.RoleCode = '1'
      AND NOT EXISTS (
          SELECT 1 FROM RoleAllow ra
          WHERE ra.RoleCode = '9' AND ra.PageCode = r.PageCode
      );
END
GO

PRINT 'Migration V063 completed successfully.';
