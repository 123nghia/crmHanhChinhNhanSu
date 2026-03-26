-- =============================================
-- Migration: V061__Add_Attendance_Management
-- Description: Add attendance management tables, procedures, and permissions
-- =============================================

PRINT 'Applying migration V061: Attendance management...';

-- ============================================
-- 1. Create AttendanceRecords table
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AttendanceRecords]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[AttendanceRecords](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [EmployeeId] [int] NULL,
        [FingerprintCode] [varchar](50) NOT NULL,
        [EmployeeName] [nvarchar](255) NULL,
        [DepartmentName] [nvarchar](255) NULL,
        [PositionName] [nvarchar](255) NULL,
        [WorkDate] [date] NOT NULL,
        [DayName] [nvarchar](20) NULL,
        [CheckIn] [time](0) NULL,
        [CheckOut] [time](0) NULL,
        [WorkDay] [decimal](6,2) NULL,
        [WorkHours] [decimal](6,2) NULL,
        [WorkDayPlus] [decimal](6,2) NULL,
        [WorkHoursPlus] [decimal](6,2) NULL,
        [LateMinutes] [int] NULL,
        [EarlyMinutes] [int] NULL,
        [Shift1] [decimal](6,2) NULL,
        [Shift2] [decimal](6,2) NULL,
        [Shift3] [decimal](6,2) NULL,
        [ShiftName] [nvarchar](100) NULL,
        [Symbol] [nvarchar](20) NULL,
        [SymbolPlus] [nvarchar](20) NULL,
        [TotalHours] [decimal](6,2) NULL,
        [SourceFile] [nvarchar](255) NULL,
        [RowIndex] [int] NULL,
        [CreatedBy] [int] NULL,
        [CreateAt] [datetime] NULL DEFAULT GETDATE(),
        [UpdatedBy] [int] NULL,
        [UpdateAt] [datetime] NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Attendance_Fingerprint_Date' AND object_id = OBJECT_ID(N'[dbo].[AttendanceRecords]'))
BEGIN
    CREATE UNIQUE INDEX [UX_Attendance_Fingerprint_Date]
    ON [dbo].[AttendanceRecords]([FingerprintCode], [WorkDate]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Attendance_EmployeeId_Date' AND object_id = OBJECT_ID(N'[dbo].[AttendanceRecords]'))
BEGIN
    CREATE INDEX [IX_Attendance_EmployeeId_Date]
    ON [dbo].[AttendanceRecords]([EmployeeId], [WorkDate]);
END
GO

-- ============================================
-- 2. Stored Procedures
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Attendance_Upsert]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Attendance_Upsert];
GO

CREATE PROCEDURE [dbo].[sp_Attendance_Upsert]
(
    @EmployeeId int = NULL,
    @FingerprintCode varchar(50),
    @EmployeeName nvarchar(255) = NULL,
    @DepartmentName nvarchar(255) = NULL,
    @PositionName nvarchar(255) = NULL,
    @WorkDate date,
    @DayName nvarchar(20) = NULL,
    @CheckIn time(0) = NULL,
    @CheckOut time(0) = NULL,
    @WorkDay decimal(6,2) = NULL,
    @WorkHours decimal(6,2) = NULL,
    @WorkDayPlus decimal(6,2) = NULL,
    @WorkHoursPlus decimal(6,2) = NULL,
    @LateMinutes int = NULL,
    @EarlyMinutes int = NULL,
    @Shift1 decimal(6,2) = NULL,
    @Shift2 decimal(6,2) = NULL,
    @Shift3 decimal(6,2) = NULL,
    @ShiftName nvarchar(100) = NULL,
    @Symbol nvarchar(20) = NULL,
    @SymbolPlus nvarchar(20) = NULL,
    @TotalHours decimal(6,2) = NULL,
    @SourceFile nvarchar(255) = NULL,
    @RowIndex int = NULL,
    @UserId int = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM AttendanceRecords WHERE FingerprintCode = @FingerprintCode AND WorkDate = @WorkDate)
    BEGIN
        UPDATE AttendanceRecords
        SET
            EmployeeId = COALESCE(@EmployeeId, EmployeeId),
            EmployeeName = @EmployeeName,
            DepartmentName = @DepartmentName,
            PositionName = @PositionName,
            DayName = @DayName,
            CheckIn = @CheckIn,
            CheckOut = @CheckOut,
            WorkDay = @WorkDay,
            WorkHours = @WorkHours,
            WorkDayPlus = @WorkDayPlus,
            WorkHoursPlus = @WorkHoursPlus,
            LateMinutes = @LateMinutes,
            EarlyMinutes = @EarlyMinutes,
            Shift1 = @Shift1,
            Shift2 = @Shift2,
            Shift3 = @Shift3,
            ShiftName = @ShiftName,
            Symbol = @Symbol,
            SymbolPlus = @SymbolPlus,
            TotalHours = @TotalHours,
            SourceFile = @SourceFile,
            RowIndex = @RowIndex,
            UpdatedBy = @UserId,
            UpdateAt = GETDATE()
        WHERE FingerprintCode = @FingerprintCode AND WorkDate = @WorkDate;
    END
    ELSE
    BEGIN
        INSERT INTO AttendanceRecords
        (
            EmployeeId, FingerprintCode, EmployeeName, DepartmentName, PositionName,
            WorkDate, DayName, CheckIn, CheckOut,
            WorkDay, WorkHours, WorkDayPlus, WorkHoursPlus,
            LateMinutes, EarlyMinutes,
            Shift1, Shift2, Shift3, ShiftName,
            Symbol, SymbolPlus, TotalHours,
            SourceFile, RowIndex,
            CreatedBy, CreateAt, UpdatedBy, UpdateAt
        )
        VALUES
        (
            @EmployeeId, @FingerprintCode, @EmployeeName, @DepartmentName, @PositionName,
            @WorkDate, @DayName, @CheckIn, @CheckOut,
            @WorkDay, @WorkHours, @WorkDayPlus, @WorkHoursPlus,
            @LateMinutes, @EarlyMinutes,
            @Shift1, @Shift2, @Shift3, @ShiftName,
            @Symbol, @SymbolPlus, @TotalHours,
            @SourceFile, @RowIndex,
            @UserId, GETDATE(), @UserId, GETDATE()
        );
    END
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Attendance_GetSummary]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Attendance_GetSummary];
GO

CREATE PROCEDURE [dbo].[sp_Attendance_GetSummary]
(
    @FromDate date = NULL,
    @ToDate date = NULL,
    @EmployeeId int = NULL,
    @Token nvarchar(255) = '',
    @offset int = 0,
    @limit int = 50,
    @UserId int = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @roleCode varchar(4) = NULL;
    DECLARE @userName varchar(50) = NULL;
    DECLARE @isFullAccess bit = 0;

    SELECT @roleCode = RoleCode, @userName = UserName
    FROM Employees
    WHERE Id = @UserId;

    IF (@roleCode IN ('1','8') OR (@roleCode = '2' AND @userName = 'VS061'))
        SET @isFullAccess = 1;

    ;WITH BaseData AS
    (
        SELECT
            a.EmployeeId,
            a.FingerprintCode,
            a.EmployeeName,
            a.DepartmentName,
            a.PositionName,
            a.WorkDate,
            a.CheckIn,
            a.CheckOut,
            a.WorkDay,
            a.WorkHours,
            a.WorkHoursPlus,
            a.LateMinutes,
            a.EarlyMinutes,
            a.Symbol,
            a.TotalHours,
            e.FullName,
            e.UserName,
            e.DepartmentCode,
            e.PositionCode,
            dbo.getDisplayMasterdata(e.DepartmentCode) AS DepartmentText,
            dbo.getDisplayMasterdata(e.PositionCode) AS PositionText,
            e.FingerprintCode AS EmployeeFingerprint
        FROM AttendanceRecords a
        LEFT JOIN Employees e ON a.EmployeeId = e.Id
        WHERE (@FromDate IS NULL OR a.WorkDate >= @FromDate)
          AND (@ToDate IS NULL OR a.WorkDate <= @ToDate)
          AND (@EmployeeId IS NULL OR a.EmployeeId = @EmployeeId)
          AND (
                @Token = ''
                OR e.FullName LIKE N'%' + @Token + '%'
                OR e.UserName LIKE N'%' + @Token + '%'
                OR a.EmployeeName LIKE N'%' + @Token + '%'
                OR a.FingerprintCode LIKE N'%' + @Token + '%'
              )
          AND (
                @UserId IS NULL OR @UserId <= 0
                OR @isFullAccess = 1
                OR (@roleCode = '3' AND a.EmployeeId IN (SELECT id FROM getAllUserByUserId(@UserId)))
                OR (@roleCode NOT IN ('1','2','3','8') AND a.EmployeeId = @UserId)
                OR (@roleCode = '2' AND @isFullAccess = 0 AND a.EmployeeId = @UserId)
              )
    )
    SELECT
        COUNT(1) OVER() AS TotalRecord,
        ISNULL(EmployeeId, 0) AS EmployeeId,
        COALESCE(EmployeeFingerprint, FingerprintCode) AS FingerprintCode,
        COALESCE(FullName, EmployeeName) AS FullName,
        COALESCE(DepartmentText, DepartmentName) AS DepartmentText,
        COALESCE(PositionText, PositionName) AS PositionText,
        SUM(ISNULL(WorkDay, 0)) AS TotalWorkDays,
        SUM(ISNULL(WorkHours, 0)) AS TotalWorkHours,
        SUM(ISNULL(WorkHoursPlus, 0)) AS TotalOvertimeHours,
        SUM(ISNULL(TotalHours, 0)) AS TotalHours,
        SUM(CASE WHEN ISNULL(LateMinutes, 0) > 0 THEN 1 ELSE 0 END) AS LateCount,
        SUM(ISNULL(LateMinutes, 0)) AS LateMinutes,
        SUM(CASE WHEN ISNULL(EarlyMinutes, 0) > 0 THEN 1 ELSE 0 END) AS EarlyCount,
        SUM(ISNULL(EarlyMinutes, 0)) AS EarlyMinutes,
        SUM(CASE WHEN ISNULL(WorkDay, 0) = 0 AND (ISNULL(Symbol, '') <> '' OR (CheckIn IS NULL AND CheckOut IS NULL)) THEN 1 ELSE 0 END) AS OffCount
    FROM BaseData
    GROUP BY EmployeeId, EmployeeFingerprint, FingerprintCode, FullName, EmployeeName, DepartmentText, DepartmentName, PositionText, PositionName
    ORDER BY COALESCE(FullName, EmployeeName)
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Attendance_GetDetails]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_Attendance_GetDetails];
GO

CREATE PROCEDURE [dbo].[sp_Attendance_GetDetails]
(
    @EmployeeId int = NULL,
    @FingerprintCode varchar(50) = NULL,
    @FromDate date = NULL,
    @ToDate date = NULL,
    @UserId int = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @roleCode varchar(4) = NULL;
    DECLARE @userName varchar(50) = NULL;
    DECLARE @isFullAccess bit = 0;

    SELECT @roleCode = RoleCode, @userName = UserName
    FROM Employees
    WHERE Id = @UserId;

    IF (@roleCode IN ('1','8') OR (@roleCode = '2' AND @userName = 'VS061'))
        SET @isFullAccess = 1;

    SELECT
        a.WorkDate,
        a.DayName,
        a.CheckIn,
        a.CheckOut,
        a.WorkDay,
        a.WorkHours,
        a.WorkDayPlus,
        a.WorkHoursPlus,
        a.LateMinutes,
        a.EarlyMinutes,
        a.ShiftName,
        a.Symbol,
        a.SymbolPlus,
        a.TotalHours,
        a.FingerprintCode
    FROM AttendanceRecords a
    WHERE (@FromDate IS NULL OR a.WorkDate >= @FromDate)
      AND (@ToDate IS NULL OR a.WorkDate <= @ToDate)
      AND (
            (@EmployeeId IS NOT NULL AND a.EmployeeId = @EmployeeId)
            OR (@EmployeeId IS NULL AND @FingerprintCode IS NOT NULL AND a.FingerprintCode = @FingerprintCode)
          )
      AND (
            @UserId IS NULL OR @UserId <= 0
            OR @isFullAccess = 1
            OR (@roleCode = '3' AND a.EmployeeId IN (SELECT id FROM getAllUserByUserId(@UserId)))
            OR (@roleCode NOT IN ('1','2','3','8') AND a.EmployeeId = @UserId)
            OR (@roleCode = '2' AND @isFullAccess = 0 AND a.EmployeeId = @UserId)
          )
    ORDER BY a.WorkDate;
END
GO

-- ============================================
-- 3. Seed AppPages and RoleAllow
-- ============================================
IF NOT EXISTS (SELECT * FROM AppPages WHERE Code = 'Attendance')
BEGIN
    INSERT INTO AppPages (Code, Name, [Group], OrderIndex, IsActive)
    VALUES ('Attendance', N'Quản lý chấm công', 'Human Resource', 12, 1);
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type in (N'U'))
BEGIN
    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '1', 'Attendance', 1, 1, 1, 1, 1
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '1' AND PageCode = 'Attendance');

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '2', 'Attendance', 1, 1, 0, 0, 0
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '2' AND PageCode = 'Attendance');

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '3', 'Attendance', 1, 0, 0, 0, 0
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '3' AND PageCode = 'Attendance');

    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT '8', 'Attendance', 1, 0, 0, 0, 0
    WHERE NOT EXISTS (SELECT 1 FROM RoleAllow WHERE RoleCode = '8' AND PageCode = 'Attendance');
END
GO

PRINT 'Migration V061 completed successfully.';
