PRINT 'Applying migration V109: attendance processing resilience...';

IF COL_LENGTH('dbo.AttendanceRecords', 'IsLocked') IS NULL
BEGIN
    ALTER TABLE dbo.AttendanceRecords
    ADD IsLocked bit NOT NULL
        CONSTRAINT DF_AttendanceRecords_IsLocked DEFAULT (0);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AttendanceLocks]') AND type = 'U')
BEGIN
    CREATE TABLE dbo.AttendanceLocks
    (
        Id int IDENTITY(1,1) NOT NULL,
        LockScope varchar(20) NOT NULL,
        RangeFrom date NOT NULL,
        RangeTo date NOT NULL,
        IsLocked bit NOT NULL CONSTRAINT DF_AttendanceLocks_IsLocked DEFAULT (1),
        Reason nvarchar(500) NULL,
        CreatedBy int NULL,
        CreateAt datetime2(0) NOT NULL CONSTRAINT DF_AttendanceLocks_CreateAt DEFAULT (SYSUTCDATETIME()),
        UpdatedBy int NULL,
        UpdateAt datetime2(0) NULL,
        Deleted bit NOT NULL CONSTRAINT DF_AttendanceLocks_Deleted DEFAULT (0),
        CONSTRAINT PK_AttendanceLocks PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT CK_AttendanceLocks_Range CHECK (RangeFrom <= RangeTo),
        CONSTRAINT CK_AttendanceLocks_Scope CHECK (LockScope IN ('DAY', 'MONTH'))
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceLocks_Range' AND object_id = OBJECT_ID(N'[dbo].[AttendanceLocks]'))
BEGIN
    CREATE INDEX IX_AttendanceLocks_Range
    ON dbo.AttendanceLocks(IsLocked, Deleted, RangeFrom, RangeTo);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AttendanceJobLogs]') AND type = 'U')
BEGIN
    CREATE TABLE dbo.AttendanceJobLogs
    (
        Id int IDENTITY(1,1) NOT NULL,
        JobType varchar(50) NOT NULL,
        RunTime datetime2(0) NOT NULL,
        Status varchar(30) NOT NULL,
        RangeFrom date NULL,
        RangeTo date NULL,
        DurationMs bigint NULL,
        Message nvarchar(1000) NULL,
        CreateAt datetime2(0) NOT NULL CONSTRAINT DF_AttendanceJobLogs_CreateAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AttendanceJobLogs PRIMARY KEY CLUSTERED (Id ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceJobLogs_JobType_RunTime' AND object_id = OBJECT_ID(N'[dbo].[AttendanceJobLogs]'))
BEGIN
    CREATE INDEX IX_AttendanceJobLogs_JobType_RunTime
    ON dbo.AttendanceJobLogs(JobType, RunTime DESC);
END
GO

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
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.AttendanceLocks WITH (UPDLOCK, HOLDLOCK)
            WHERE IsLocked = 1
              AND ISNULL(Deleted, 0) = 0
              AND @WorkDate >= RangeFrom
              AND @WorkDate <= RangeTo
        )
        BEGIN
            COMMIT TRANSACTION;
            RETURN;
        END

        IF EXISTS
        (
            SELECT 1
            FROM dbo.AttendanceRecords WITH (UPDLOCK, HOLDLOCK)
            WHERE FingerprintCode = @FingerprintCode
              AND WorkDate = @WorkDate
              AND ISNULL(IsLocked, 0) = 1
        )
        BEGIN
            COMMIT TRANSACTION;
            RETURN;
        END

        IF EXISTS
        (
            SELECT 1
            FROM dbo.AttendanceRecords WITH (UPDLOCK, HOLDLOCK)
            WHERE FingerprintCode = @FingerprintCode
              AND WorkDate = @WorkDate
        )
        BEGIN
            UPDATE dbo.AttendanceRecords
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
            WHERE FingerprintCode = @FingerprintCode
              AND WorkDate = @WorkDate
              AND ISNULL(IsLocked, 0) = 0;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.AttendanceRecords
            (
                EmployeeId, FingerprintCode, EmployeeName, DepartmentName, PositionName,
                WorkDate, DayName, CheckIn, CheckOut,
                WorkDay, WorkHours, WorkDayPlus, WorkHoursPlus,
                LateMinutes, EarlyMinutes,
                Shift1, Shift2, Shift3, ShiftName,
                Symbol, SymbolPlus, TotalHours,
                SourceFile, RowIndex,
                CreatedBy, CreateAt, UpdatedBy, UpdateAt, IsLocked
            )
            VALUES
            (
                @EmployeeId, @FingerprintCode, @EmployeeName, @DepartmentName, @PositionName,
                @WorkDate, @DayName, @CheckIn, @CheckOut,
                @WorkDay, @WorkHours, @WorkDayPlus, @WorkHoursPlus,
                @LateMinutes, @EarlyMinutes, @Shift1, @Shift2, @Shift3, @ShiftName,
                @Symbol, @SymbolPlus, @TotalHours,
                @SourceFile, @RowIndex,
                @UserId, GETDATE(), @UserId, GETDATE(), 0
            );
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END

        ;THROW;
    END CATCH
END
GO

PRINT 'Migration V109 completed successfully.';
