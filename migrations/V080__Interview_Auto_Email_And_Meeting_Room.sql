-- =============================================
-- Migration: V080__Interview_Auto_Email_And_Meeting_Room
-- Description: Link meeting room bookings with interview schedules and add interview email template
-- =============================================

PRINT 'Applying migration V080: Interview auto email and meeting room...';

IF COL_LENGTH('dbo.MeetingBookings', 'ScheduleInterviewId') IS NULL
BEGIN
    ALTER TABLE dbo.MeetingBookings
    ADD ScheduleInterviewId INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_MeetingBookings_ScheduleInterview'
)
BEGIN
    ALTER TABLE dbo.MeetingBookings
    ADD CONSTRAINT FK_MeetingBookings_ScheduleInterview
        FOREIGN KEY (ScheduleInterviewId) REFERENCES dbo.ScheduleInterview(Id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_MeetingBookings_ScheduleInterviewId'
      AND object_id = OBJECT_ID(N'dbo.MeetingBookings')
)
BEGIN
    CREATE UNIQUE INDEX UX_MeetingBookings_ScheduleInterviewId
    ON dbo.MeetingBookings (ScheduleInterviewId)
    WHERE ScheduleInterviewId IS NOT NULL AND Deleted = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE Code = 'INTERVIEW_SCHEDULE' AND ISNULL(Deleted, 0) = 0)
BEGIN
    INSERT INTO EmailTemplates
    (
        Code,
        Name,
        Subject,
        Body,
        CcManager,
        CcEmails,
        BccEmails,
        IsActive,
        Deleted,
        CreatedBy,
        UpdatedBy,
        CreateAt,
        UpdateAt
    )
    VALUES
    (
        'INTERVIEW_SCHEDULE',
        N'Thu moi phong van',
        N'Thu moi phong van - {{CandidateName}}',
        N'
        <p>Xin chao {{CandidateName}},</p>
        <p>Lich phong van cua ban {{InterviewAction}}.</p>
        <p>Thong tin chi tiet:</p>
        <ul>
            <li>Vong phong van: {{InterviewRound}}</li>
            <li>Thoi gian: {{ScheduleDate}}</li>
            <li>Hinh thuc: {{InterviewMode}}</li>
            <li>Dia diem / Link: {{AddressInfo}}</li>
            <li>Nguoi phong van: {{InterviewerName}}</li>
        </ul>
        <p>{{Noted}}</p>
        <p>Tran trong,<br/>He thong quan ly nhan su</p>',
        0,
        NULL,
        NULL,
        1,
        0,
        1,
        1,
        GETDATE(),
        GETDATE()
    );
END
GO

PRINT 'V080 completed successfully';
