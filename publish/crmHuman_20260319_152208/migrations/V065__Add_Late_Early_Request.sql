-- =============================================
-- Migration: V065__Add_Late_Early_Request
-- Description: Add late/early request workflow tables and permissions
-- =============================================

PRINT 'Applying migration V065: Late/Early request...';

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LateEarlyRequests]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LateEarlyRequests](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EmployeeId] INT NOT NULL,
        [RequestType] VARCHAR(10) NOT NULL,
        [RequestDate] DATE NOT NULL,
        [StartTime] DATETIME NOT NULL,
        [EndTime] DATETIME NOT NULL,
        [Reason] NVARCHAR(MAX) NOT NULL,
        [Status] INT NOT NULL DEFAULT 0,
        [LeadApproverId] INT NULL,
        [LeadApproveAt] DATETIME NULL,
        [LeadComment] NVARCHAR(MAX) NULL,
        [HCNSApproverId] INT NULL,
        [HCNSApproveAt] DATETIME NULL,
        [HCNSComment] NVARCHAR(MAX) NULL,
        [BGDApproverId] INT NULL,
        [BGDApproveAt] DATETIME NULL,
        [BGDComment] NVARCHAR(MAX) NULL,
        [AdminApproverId] INT NULL,
        [AdminApproveAt] DATETIME NULL,
        [AdminComment] NVARCHAR(MAX) NULL,
        [ApproverId] INT NULL,
        [ApproveAt] DATETIME NULL,
        [Comment] NVARCHAR(MAX) NULL,
        [CreateAt] DATETIME DEFAULT GETDATE(),
        [CreatedBy] INT NULL,
        [UpdateAt] DATETIME NULL,
        [UpdatedBy] INT NULL,
        [Deleted] BIT DEFAULT 0
    );

    CREATE INDEX IX_LateEarlyRequests_Employee ON LateEarlyRequests (EmployeeId, RequestDate);
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LateEarlyHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LateEarlyHistory](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RequestId] INT NOT NULL,
        [Action] NVARCHAR(50) NOT NULL,
        [ActionBy] INT NOT NULL,
        [ActionTime] DATETIME DEFAULT GETDATE(),
        [Comment] NVARCHAR(MAX) NULL,
        [StatusAfter] INT NOT NULL,
        [CreateAt] DATETIME DEFAULT GETDATE(),
        [CreatedBy] INT NULL,
        [UpdateAt] DATETIME NULL,
        [UpdatedBy] INT NULL,
        [Deleted] BIT DEFAULT 0
    );

    CREATE INDEX IX_LateEarlyHistory_Request ON LateEarlyHistory (RequestId);
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AppPages]') AND type in (N'U'))
BEGIN
    IF NOT EXISTS (SELECT 1 FROM AppPages WHERE Code = 'LateEarlyRequest')
    BEGIN
        INSERT INTO AppPages (Code, Name, [Group], OrderIndex, IsActive)
        VALUES ('LateEarlyRequest', N'Đi trễ / Về sớm - Tạo yêu cầu', N'Nhân sự', 66, 1);
    END

    IF NOT EXISTS (SELECT 1 FROM AppPages WHERE Code = 'LateEarlyApproval')
    BEGIN
        INSERT INTO AppPages (Code, Name, [Group], OrderIndex, IsActive)
        VALUES ('LateEarlyApproval', N'Đi trễ / Về sớm - Duyệt', N'Nhân sự', 67, 1);
    END
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type in (N'U'))
BEGIN
    DECLARE @Roles TABLE (RoleCode varchar(20), CanApprove bit);
    INSERT INTO @Roles (RoleCode, CanApprove) VALUES
        ('1', 1),
        ('2', 0),
        ('3', 1),
        ('4', 0),
        ('6', 0),
        ('7', 0),
        ('8', 1),
        ('9', 1);

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT RoleCode, 'LateEarlyRequest', 1, 1, 1, 1, 0
    FROM @Roles r
    WHERE NOT EXISTS (
        SELECT 1 FROM RoleAllow ra
        WHERE ra.RoleCode = r.RoleCode AND ra.PageCode = 'LateEarlyRequest'
    );

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT RoleCode, 'LateEarlyApproval', 1, 0, 0, 0, CanApprove
    FROM @Roles r
    WHERE NOT EXISTS (
        SELECT 1 FROM RoleAllow ra
        WHERE ra.RoleCode = r.RoleCode AND ra.PageCode = 'LateEarlyApproval'
    );
END

PRINT 'Migration V065 completed successfully.';
