IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AttendanceHolidays]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[AttendanceHolidays]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [HolidayName] NVARCHAR(200) NOT NULL,
        [FromDate] DATE NOT NULL,
        [ToDate] DATE NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_AttendanceHolidays_IsActive] DEFAULT (1),
        [Deleted] BIT NOT NULL CONSTRAINT [DF_AttendanceHolidays_Deleted] DEFAULT (0),
        [CreatedBy] INT NULL,
        [CreateAt] DATETIME NOT NULL CONSTRAINT [DF_AttendanceHolidays_CreateAt] DEFAULT (GETDATE()),
        [UpdatedBy] INT NULL,
        [UpdateAt] DATETIME NOT NULL CONSTRAINT [DF_AttendanceHolidays_UpdateAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_AttendanceHolidays] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_AttendanceHolidays_DateRange'
      AND object_id = OBJECT_ID(N'[dbo].[AttendanceHolidays]')
)
BEGIN
    CREATE INDEX [IX_AttendanceHolidays_DateRange]
        ON [dbo].[AttendanceHolidays]([FromDate], [ToDate], [IsActive], [Deleted]);
END
