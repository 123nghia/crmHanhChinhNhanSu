-- =============================================
-- Migration: V064__Add_Meeting_Room_Booking
-- Description: Create meeting room booking tables and permissions
-- =============================================

PRINT 'Applying migration V064: Meeting room booking...';

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MeetingRooms]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MeetingRooms](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Location] NVARCHAR(200) NULL,
        [Capacity] INT NULL,
        [IsActive] BIT DEFAULT 1,
        [Deleted] BIT DEFAULT 0,
        [CreatedAt] DATETIME DEFAULT GETDATE(),
        [CreatedBy] INT NULL,
        [UpdatedAt] DATETIME NULL,
        [UpdatedBy] INT NULL
    );
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MeetingBookings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MeetingBookings](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RoomId] INT NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Note] NVARCHAR(MAX) NULL,
        [StartTime] DATETIME NOT NULL,
        [EndTime] DATETIME NOT NULL,
        [CreatedAt] DATETIME DEFAULT GETDATE(),
        [CreatedBy] INT NULL,
        [UpdatedAt] DATETIME NULL,
        [UpdatedBy] INT NULL,
        [Deleted] BIT DEFAULT 0,
        CONSTRAINT [FK_MeetingBookings_Room] FOREIGN KEY ([RoomId]) REFERENCES [dbo].[MeetingRooms]([Id])
    );

    CREATE INDEX IX_MeetingBookings_Room_Start ON MeetingBookings (RoomId, StartTime);
END

IF NOT EXISTS (SELECT 1 FROM MeetingRooms)
BEGIN
    INSERT INTO MeetingRooms (Name, Location, Capacity, IsActive, CreatedAt)
    VALUES
        (N'Phòng họp A', N'Tầng 1', 10, 1, GETDATE()),
        (N'Phòng họp B', N'Tầng 2', 8, 1, GETDATE()),
        (N'Phòng họp C', N'Tầng 3', 12, 1, GETDATE());
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AppPages]') AND type in (N'U'))
BEGIN
    IF NOT EXISTS (SELECT 1 FROM AppPages WHERE Code = 'MeetingRoom')
    BEGIN
        INSERT INTO AppPages (Code, Name, [Group], OrderIndex, IsActive)
        VALUES ('MeetingRoom', N'Phòng họp', N'Thông tin chung', 65, 1);
    END
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleAllow]') AND type in (N'U'))
BEGIN
    ;WITH Roles AS (
        SELECT RoleCode, CanManage FROM (VALUES
            ('1', 1),
            ('2', 0),
            ('3', 0),
            ('4', 0),
            ('6', 0),
            ('7', 0),
            ('8', 1),
            ('9', 1)
        ) v(RoleCode, CanManage)
    )
    INSERT INTO RoleAllow (RoleCode, PageCode, IsView, IsAdd, IsEdit, IsDelete, IsApprove)
    SELECT RoleCode, 'MeetingRoom', 1, 1, CanManage, CanManage, 0
    FROM Roles r
    WHERE NOT EXISTS (
        SELECT 1 FROM RoleAllow ra
        WHERE ra.RoleCode = r.RoleCode AND ra.PageCode = 'MeetingRoom'
    );
END

PRINT 'Migration V064 completed successfully.';
