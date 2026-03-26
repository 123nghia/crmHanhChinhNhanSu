PRINT 'Applying migration V078: Update leave email templates...';
GO

UPDATE EmailTemplates
SET
    Name = N'Đơn nghỉ phép mới',
    Subject = N'Đơn xin nghỉ phép mới từ {{EmployeeName}}',
    Body = N'<p>Xin chào {{ManagerName}},</p>
<p>Nhân viên {{EmployeeName}} vừa tạo một đơn xin nghỉ phép với thông tin như sau:</p>
<ul>
    <li>Loại nghỉ: {{LeaveType}}</li>
    <li>Từ ngày: {{FromDate}}</li>
    <li>Đến ngày: {{ToDate}}</li>
    <li>Số ngày nghỉ: {{TotalDays}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để xem và phê duyệt đơn.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>',
    UpdateAt = GETDATE()
WHERE Code = 'LEAVE_CREATE' AND ISNULL(Deleted, 0) = 0;
GO

UPDATE EmailTemplates
SET
    Name = N'Đơn nghỉ phép được phê duyệt',
    Subject = N'Đơn nghỉ phép của bạn đã được phê duyệt',
    Body = N'<p>Xin chào {{EmployeeName}},</p>
<p>Đơn xin nghỉ phép của bạn đã được phê duyệt.</p>
<p>Thông tin chi tiết:</p>
<ul>
    <li>Loại nghỉ: {{LeaveType}}</li>
    <li>Từ ngày: {{FromDate}}</li>
    <li>Đến ngày: {{ToDate}}</li>
    <li>Số ngày nghỉ: {{TotalDays}}</li>
</ul>
<p>Chúc bạn có thời gian nghỉ ngơi hiệu quả.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>',
    UpdateAt = GETDATE()
WHERE Code = 'LEAVE_APPROVE' AND ISNULL(Deleted, 0) = 0;
GO

UPDATE EmailTemplates
SET
    Name = N'Đơn nghỉ phép bị từ chối',
    Subject = N'Đơn nghỉ phép của bạn đã bị từ chối',
    Body = N'<p>Xin chào {{EmployeeName}},</p>
<p>Rất tiếc, đơn xin nghỉ phép của bạn đã bị từ chối.</p>
<p>Thông tin đơn:</p>
<ul>
    <li>Loại nghỉ: {{LeaveType}}</li>
    <li>Từ ngày: {{FromDate}}</li>
    <li>Đến ngày: {{ToDate}}</li>
</ul>
<p>Lý do từ chối:<br />{{RejectReason}}</p>
<p>Vui lòng liên hệ quản lý để biết thêm chi tiết.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>',
    UpdateAt = GETDATE()
WHERE Code = 'LEAVE_REJECT' AND ISNULL(Deleted, 0) = 0;
GO

PRINT 'Migration V078 completed successfully.';
