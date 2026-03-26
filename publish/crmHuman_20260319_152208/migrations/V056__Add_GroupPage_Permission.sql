-- =============================================
-- Migration: V056__Add_GroupPage_Permission
-- Description: Add GroupPage to AppPages and seed RoleAllow.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM AppPages WHERE Code = 'GroupPage')
BEGIN
    INSERT INTO AppPages (Code, Name, [Group], OrderIndex, IsActive)
    VALUES ('GroupPage', N'Danh sach nhom', 'Human Resource', 3, 1);
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type = 'U')
BEGIN
    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '1', 'GroupPage', 1, 1, 1, 1, 1
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '1' AND PageCode = 'GroupPage');

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT r.RoleCode, 'GroupPage', r.IsView, r.IsAdd, r.IsEdit, r.IsDelete, r.IsApprove
    FROM RoleAllow r
    WHERE r.PageCode = 'Employee'
      AND r.IsView = 1
      AND NOT EXISTS (
          SELECT 1 FROM RoleAllow ra
          WHERE ra.RoleCode = r.RoleCode AND ra.PageCode = 'GroupPage'
      );
END
GO
