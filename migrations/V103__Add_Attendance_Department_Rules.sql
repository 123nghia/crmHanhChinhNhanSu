PRINT 'Applying migration V103: attendance department rules...';

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AttendanceDepartmentRules]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[AttendanceDepartmentRules]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [DepartmentCode] VARCHAR(50) NOT NULL,
        [WorkStartTime] TIME(0) NOT NULL,
        [LunchStartTime] TIME(0) NOT NULL,
        [LunchEndTime] TIME(0) NOT NULL,
        [WorkEndTime] TIME(0) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_AttendanceDepartmentRules_IsActive] DEFAULT (1),
        [Deleted] BIT NOT NULL CONSTRAINT [DF_AttendanceDepartmentRules_Deleted] DEFAULT (0),
        [CreatedBy] INT NULL,
        [CreateAt] DATETIME NOT NULL CONSTRAINT [DF_AttendanceDepartmentRules_CreateAt] DEFAULT (GETDATE()),
        [UpdatedBy] INT NULL,
        [UpdateAt] DATETIME NOT NULL CONSTRAINT [DF_AttendanceDepartmentRules_UpdateAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_AttendanceDepartmentRules] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_AttendanceDepartmentRules_DepartmentCode'
      AND object_id = OBJECT_ID(N'[dbo].[AttendanceDepartmentRules]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_AttendanceDepartmentRules_DepartmentCode]
    ON [dbo].[AttendanceDepartmentRules]([DepartmentCode])
    WHERE [Deleted] = 0;
END
GO

PRINT 'Migration V103 completed successfully.';
