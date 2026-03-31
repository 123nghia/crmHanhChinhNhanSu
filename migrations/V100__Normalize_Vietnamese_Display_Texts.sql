PRINT 'Running V100__Normalize_Vietnamese_Display_Texts.sql';

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AppPages]') AND type = N'U')
BEGIN
    UPDATE dbo.AppPages SET Name = N'Quản lý chấm công' WHERE Code = 'Attendance';
    UPDATE dbo.AppPages SET Name = N'Danh sách phỏng vấn' WHERE Code = 'ScheduleInterview';
    UPDATE dbo.AppPages SET Name = N'Quản lý hợp đồng' WHERE Code = 'Contract';
    UPDATE dbo.AppPages SET Name = N'Báo cáo và phân tích' WHERE Code = 'ReportAnalytics';
    UPDATE dbo.AppPages SET Name = N'Cấu hình mail' WHERE Code = 'MailSetting';
    UPDATE dbo.AppPages SET Name = N'Đặt phòng họp' WHERE Code = 'MeetingRoom';
    UPDATE dbo.AppPages SET Name = N'Xin phép đi trễ/về sớm' WHERE Code = 'LateEarlyRequest';
    UPDATE dbo.AppPages SET Name = N'Duyệt đi trễ/về sớm' WHERE Code = 'LateEarlyApproval';
    UPDATE dbo.AppPages SET Name = N'Quản lý ngày phép' WHERE Code = 'LeaveBalance';
    UPDATE dbo.AppPages SET Name = N'Tin nội bộ' WHERE Code = 'InternalNews';
    UPDATE dbo.AppPages SET Name = N'Yêu cầu hỗ trợ' WHERE Code = 'SupportRequest';
    UPDATE dbo.AppPages SET Name = N'Xử lý yêu cầu' WHERE Code = 'SupportRequestProcessing';
END;

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmailTemplates]') AND type = N'U')
BEGIN
    UPDATE dbo.EmailTemplates
    SET
        Name = N'Thư mời phỏng vấn',
        Subject = N'Thư mời phỏng vấn - {{CandidateName}}',
        Body = N'
<p>Xin chào {{CandidateName}},</p>
<p>Lịch phỏng vấn của bạn {{InterviewAction}}.</p>
<p>Thông tin chi tiết:</p>
<ul>
    <li>Vị trí ứng tuyển: {{AppliedPosition}}</li>
    <li>Vòng phỏng vấn: {{InterviewRound}}</li>
    <li>Thời gian: {{ScheduleDate}}</li>
    <li>Hình thức: {{InterviewMode}}</li>
    <li>Địa điểm / Link: {{AddressInfo}}</li>
    <li>Người phỏng vấn: {{InterviewerName}}</li>
</ul>
<p>{{Noted}}</p>
<p>Trân trọng,<br/>Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'INTERVIEW_SCHEDULE';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Đơn nghỉ phép mới',
        Subject = N'Đơn xin nghỉ phép mới từ {{EmployeeName}}',
        Body = N'
<p>Xin chào {{ManagerName}},</p>
<p>Nhân viên {{EmployeeName}} vừa tạo một đơn xin nghỉ phép với thông tin như sau:</p>
<ul>
    <li>Loại nghỉ: {{LeaveType}}</li>
    <li>Từ ngày: {{FromDate}}</li>
    <li>Đến ngày: {{ToDate}}</li>
    <li>Số ngày nghỉ: {{TotalDays}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để xem và phê duyệt đơn.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'LEAVE_CREATE';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Đơn nghỉ phép được phê duyệt',
        Subject = N'Đơn nghỉ phép của bạn đã được phê duyệt',
        Body = N'
<p>Xin chào {{EmployeeName}},</p>
<p>Đơn xin nghỉ phép của bạn đã được phê duyệt.</p>
<p>Thông tin chi tiết:</p>
<ul>
    <li>Loại nghỉ: {{LeaveType}}</li>
    <li>Từ ngày: {{FromDate}}</li>
    <li>Đến ngày: {{ToDate}}</li>
    <li>Số ngày nghỉ: {{TotalDays}}</li>
</ul>
<p>Chúc bạn có thời gian nghỉ ngơi hiệu quả.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'LEAVE_APPROVE';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Đơn nghỉ phép bị từ chối',
        Subject = N'Đơn nghỉ phép của bạn đã bị từ chối',
        Body = N'
<p>Xin chào {{EmployeeName}},</p>
<p>Rất tiếc, đơn xin nghỉ phép của bạn đã bị từ chối.</p>
<p>Thông tin đơn:</p>
<ul>
    <li>Loại nghỉ: {{LeaveType}}</li>
    <li>Từ ngày: {{FromDate}}</li>
    <li>Đến ngày: {{ToDate}}</li>
</ul>
<p>Lý do từ chối:<br />{{RejectReason}}</p>
<p>Vui lòng liên hệ quản lý để biết thêm chi tiết.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'LEAVE_REJECT';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Đơn nghỉ phép chờ HCNS xử lý',
        Subject = N'Đơn nghỉ phép của {{EmployeeName}} chờ HCNS xử lý',
        Body = N'
<p>Kính gửi Phòng HCNS,</p>
<p>Đơn nghỉ phép của {{EmployeeName}} đã được Team Lead duyệt và đang chờ HCNS xử lý.</p>
<ul>
    <li>Loại nghỉ: {{LeaveType}}</li>
    <li>Từ ngày: {{FromDate}}</li>
    <li>Đến ngày: {{ToDate}}</li>
    <li>Số ngày nghỉ: {{TotalDays}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để tiếp tục xử lý đơn.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'LEAVE_PENDING_HCNS';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Đơn nghỉ phép chờ BGĐ phê duyệt',
        Subject = N'Đơn nghỉ phép của {{EmployeeName}} chờ BGĐ phê duyệt',
        Body = N'
<p>Kính gửi Ban Giám đốc,</p>
<p>Đơn nghỉ phép của {{EmployeeName}} đã được HCNS kiểm tra và đang chờ BGĐ phê duyệt.</p>
<ul>
    <li>Loại nghỉ: {{LeaveType}}</li>
    <li>Từ ngày: {{FromDate}}</li>
    <li>Đến ngày: {{ToDate}}</li>
    <li>Số ngày nghỉ: {{TotalDays}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để xem và phê duyệt đơn.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'LEAVE_PENDING_BGD';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Đơn đi trễ về sớm mới',
        Subject = N'Đơn {{RequestTypeName}} mới từ {{EmployeeName}}',
        Body = N'
<p>Xin chào {{ManagerName}},</p>
<p>{{EmployeeName}} vừa tạo một yêu cầu {{RequestTypeName}} với thông tin như sau:</p>
<ul>
    <li>Ngày áp dụng: {{RequestDate}}</li>
    <li>Khung giờ: {{TimeRange}}</li>
    <li>Số phút: {{DurationMinutes}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để xem và phê duyệt yêu cầu.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'LATE_EARLY_CREATE';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Đơn đi trễ về sớm được phê duyệt',
        Subject = N'Đơn {{RequestTypeName}} của bạn đã được phê duyệt',
        Body = N'
<p>Xin chào {{EmployeeName}},</p>
<p>Yêu cầu {{RequestTypeName}} của bạn đã được phê duyệt.</p>
<ul>
    <li>Ngày áp dụng: {{RequestDate}}</li>
    <li>Khung giờ: {{TimeRange}}</li>
    <li>Số phút: {{DurationMinutes}}</li>
</ul>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'LATE_EARLY_APPROVE';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Đơn đi trễ về sớm bị từ chối',
        Subject = N'Đơn {{RequestTypeName}} của bạn đã bị từ chối',
        Body = N'
<p>Xin chào {{EmployeeName}},</p>
<p>Rất tiếc, yêu cầu {{RequestTypeName}} của bạn đã bị từ chối.</p>
<ul>
    <li>Ngày áp dụng: {{RequestDate}}</li>
    <li>Khung giờ: {{TimeRange}}</li>
</ul>
<p>Lý do từ chối:<br />{{RejectReason}}</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'LATE_EARLY_REJECT';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Đơn đi trễ về sớm chờ HCNS xử lý',
        Subject = N'Đơn {{RequestTypeName}} của {{EmployeeName}} chờ HCNS xử lý',
        Body = N'
<p>Kính gửi Phòng HCNS,</p>
<p>Yêu cầu {{RequestTypeName}} của {{EmployeeName}} đã được Team Lead duyệt và đang chờ HCNS xử lý.</p>
<ul>
    <li>Ngày áp dụng: {{RequestDate}}</li>
    <li>Khung giờ: {{TimeRange}}</li>
    <li>Số phút: {{DurationMinutes}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để tiếp tục xử lý.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'LATE_EARLY_PENDING_HCNS';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Đơn đi trễ về sớm chờ BGĐ phê duyệt',
        Subject = N'Đơn {{RequestTypeName}} của {{EmployeeName}} chờ BGĐ phê duyệt',
        Body = N'
<p>Kính gửi Ban Giám đốc,</p>
<p>Yêu cầu {{RequestTypeName}} của {{EmployeeName}} đã được HCNS kiểm tra và đang chờ BGĐ phê duyệt.</p>
<ul>
    <li>Ngày áp dụng: {{RequestDate}}</li>
    <li>Khung giờ: {{TimeRange}}</li>
    <li>Số phút: {{DurationMinutes}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để xem và phê duyệt yêu cầu.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'LATE_EARLY_PENDING_BGD';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Đơn đi trễ về sớm chờ Admin phê duyệt',
        Subject = N'Đơn {{RequestTypeName}} của {{EmployeeName}} chờ Admin phê duyệt',
        Body = N'
<p>Kính gửi Admin,</p>
<p>Yêu cầu {{RequestTypeName}} của {{EmployeeName}} đã được BGĐ duyệt và đang chờ Admin xác nhận cuối.</p>
<ul>
    <li>Ngày áp dụng: {{RequestDate}}</li>
    <li>Khung giờ: {{TimeRange}}</li>
    <li>Số phút: {{DurationMinutes}}</li>
    <li>Lý do: {{Reason}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để tiếp tục xử lý.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'LATE_EARLY_PENDING_ADMIN';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Yêu cầu hỗ trợ mới',
        Subject = N'Yêu cầu hỗ trợ mới: {{Title}}',
        Body = N'
<p>Xin chào {{AssignedToName}},</p>
<p>Bạn vừa được giao một yêu cầu hỗ trợ mới.</p>
<ul>
    <li>Người tạo: {{RequesterName}}</li>
    <li>Bộ phận cần xử lý: {{TargetDepartment}}</li>
    <li>Tiêu đề: {{Title}}</li>
    <li>Nội dung: {{Content}}</li>
</ul>
<p>Vui lòng đăng nhập hệ thống để tiếp nhận và cập nhật tiến độ.</p>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'SUPPORT_REQUEST_CREATE';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Yêu cầu hỗ trợ đang được xử lý',
        Subject = N'Yêu cầu hỗ trợ ''{{Title}}'' đang được xử lý',
        Body = N'
<p>Xin chào {{RequesterName}},</p>
<p>Yêu cầu hỗ trợ của bạn đã được tiếp nhận và đang xử lý.</p>
<ul>
    <li>Bộ phận xử lý: {{TargetDepartment}}</li>
    <li>Người phụ trách: {{AssignedToName}}</li>
    <li>Tiêu đề: {{Title}}</li>
    <li>Ghi chú xử lý: {{ProcessorComment}}</li>
</ul>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'SUPPORT_REQUEST_INPROCESS';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Yêu cầu hỗ trợ đã hoàn thành',
        Subject = N'Yêu cầu hỗ trợ ''{{Title}}'' đã hoàn thành',
        Body = N'
<p>Xin chào {{RequesterName}},</p>
<p>Yêu cầu hỗ trợ của bạn đã được xử lý xong.</p>
<ul>
    <li>Bộ phận xử lý: {{TargetDepartment}}</li>
    <li>Người phụ trách: {{AssignedToName}}</li>
    <li>Tiêu đề: {{Title}}</li>
    <li>Kết quả: {{ProcessorComment}}</li>
</ul>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'SUPPORT_REQUEST_DONE';

    UPDATE dbo.EmailTemplates
    SET
        Name = N'Yêu cầu hỗ trợ đã bị hủy',
        Subject = N'Yêu cầu hỗ trợ ''{{Title}}'' đã bị hủy',
        Body = N'
<p>Xin chào {{RequesterName}},</p>
<p>Yêu cầu hỗ trợ của bạn đã được cập nhật sang trạng thái hủy.</p>
<ul>
    <li>Bộ phận xử lý: {{TargetDepartment}}</li>
    <li>Người phụ trách: {{AssignedToName}}</li>
    <li>Tiêu đề: {{Title}}</li>
    <li>Lý do / ghi chú: {{ProcessorComment}}</li>
</ul>
<p>Trân trọng,<br />Hệ thống quản lý nhân sự</p>'
    WHERE Code = 'SUPPORT_REQUEST_CANCEL';
END;
