PRINT 'Applying migration V081: add leave approval queue email templates...';
GO

IF NOT EXISTS (
    SELECT 1
    FROM EmailTemplates
    WHERE Code = 'LEAVE_PENDING_HCNS' AND ISNULL(Deleted, 0) = 0
)
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
        'LEAVE_PENDING_HCNS',
        N'Don nghi phep cho HCNS xu ly',
        N'Don nghi phep cua {{EmployeeName}} cho HCNS xu ly',
        N'<p>Kinh gui Phong HCNS,</p>
<p>Don nghi phep cua {{EmployeeName}} da duoc Team Lead duyet va dang cho HCNS xu ly.</p>
<ul>
    <li>Loai nghi: {{LeaveType}}</li>
    <li>Tu ngay: {{FromDate}}</li>
    <li>Den ngay: {{ToDate}}</li>
    <li>So ngay nghi: {{TotalDays}}</li>
    <li>Ly do: {{Reason}}</li>
</ul>
<p>Vui long dang nhap he thong de tiep tuc xu ly don.</p>
<p>Tran trong,<br />He thong quan ly nhan su</p>',
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

IF NOT EXISTS (
    SELECT 1
    FROM EmailTemplates
    WHERE Code = 'LEAVE_PENDING_BGD' AND ISNULL(Deleted, 0) = 0
)
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
        'LEAVE_PENDING_BGD',
        N'Don nghi phep cho BGD phe duyet',
        N'Don nghi phep cua {{EmployeeName}} cho BGD phe duyet',
        N'<p>Kinh gui Ban Giam doc,</p>
<p>Don nghi phep cua {{EmployeeName}} da duoc HCNS kiem tra va dang cho BGD phe duyet.</p>
<ul>
    <li>Loai nghi: {{LeaveType}}</li>
    <li>Tu ngay: {{FromDate}}</li>
    <li>Den ngay: {{ToDate}}</li>
    <li>So ngay nghi: {{TotalDays}}</li>
    <li>Ly do: {{Reason}}</li>
</ul>
<p>Vui long dang nhap he thong de xem va phe duyet don.</p>
<p>Tran trong,<br />He thong quan ly nhan su</p>',
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

PRINT 'V081 completed successfully.';
