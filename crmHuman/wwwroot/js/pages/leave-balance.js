function openLeaveBalanceModal(button) {
    var row = button.closest('tr');
    if (!row) return;

    var employeeId = row.getAttribute('data-id');
    var name = row.getAttribute('data-name') || '';
    var allowed = row.getAttribute('data-allowed') || '0';
    var carryOver = row.getAttribute('data-carryover') || '0';
    var used = row.getAttribute('data-used') || '0';
    var expired = row.getAttribute('data-expired') || '0';

    $('#leaveBalanceEmployeeId').val(employeeId);
    $('#leaveBalanceEmployeeName').val(name);
    $('#allowedLeaveDays').val(allowed);
    $('#carryOverLeaveDays').val(carryOver);
    $('#usedLeaveDays').val(used);
    $('#expiredLeaveDays').val(expired);
    $('#leaveBalanceModal').modal('show');
}

function openLeaveDetailsModal(button) {
    var row = button.closest('tr');
    if (!row) return;

    var employeeId = parseInt(row.getAttribute('data-id'), 10);
    var name = row.getAttribute('data-name') || '';

    if (!employeeId || employeeId <= 0) {
        alert('Mã nhân viên không hợp lệ.');
        return;
    }

    $('#leaveDetailEmployeeName').val(name);
    $('#leaveDetailContent').html('<tr><td colspan="8" class="text-center">Đang tải...</td></tr>');
    $('#leaveDetailModal').modal('show');

    fetch('?handler=LeaveDetails&employeeId=' + employeeId)
        .then(function (res) { return res.json(); })
        .then(function (data) {
            if (!Array.isArray(data) || data.length === 0) {
                $('#leaveDetailContent').html('<tr><td colspan="8" class="text-center">Không có dữ liệu</td></tr>');
                return;
            }

            var html = '';
            data.forEach(function (item) {
                var fromDate = formatDate(item.fromDate || item.FromDate);
                var toDate = formatDate(item.toDate || item.ToDate);
                var numDays = item.numDays || item.NumDays || 0;
                var reason = item.reason || item.Reason || '';
                var currentApproverName = item.currentApproverName || item.CurrentApproverName || '';
                var createdAt = formatDateTime(item.createdAt || item.CreatedAt || item.createAt || item.CreateAt);
                var statusInfo = getLeaveStatusInfo(item.status != null ? item.status : item.Status);

                html += '<tr>'
                    + '<td>' + escapeHtml(item.leaveTypeName || item.LeaveTypeName || '') + '</td>'
                    + '<td>' + fromDate + '</td>'
                    + '<td>' + toDate + '</td>'
                    + '<td class="text-end">' + numDays + '</td>'
                    + '<td>' + escapeHtml(reason) + '</td>'
                    + '<td>' + escapeHtml(currentApproverName) + '</td>'
                    + '<td>' + createdAt + '</td>'
                    + '<td class="text-center"><span class="badge ' + statusInfo.className + '">' + statusInfo.text + '</span></td>'
                    + '</tr>';
            });

            $('#leaveDetailContent').html(html);
        })
        .catch(function (error) {
            console.error('Error:', error);
            $('#leaveDetailContent').html('<tr><td colspan="8" class="text-center">Lỗi hệ thống</td></tr>');
        });
}

function getLeaveStatusInfo(status) {
    switch (status) {
        case 0:
            return { text: 'Chờ quản lý trực tiếp phê duyệt', className: 'bg-warning text-dark' };
        case 1:
            return { text: 'Chờ HCNS phê duyệt', className: 'bg-info text-dark' };
        case 2:
            return { text: 'Chờ BGĐ phê duyệt', className: 'bg-primary' };
        case 3:
            return { text: 'Đã phê duyệt', className: 'bg-success' };
        case 4:
            return { text: 'Đã phê duyệt (HCNS duyệt thay)', className: 'bg-success' };
        case 5:
            return { text: 'Từ chối', className: 'bg-danger' };
        case 6:
            return { text: 'Đã hủy', className: 'bg-secondary' };
        default:
            return { text: 'Không xác định', className: 'bg-light text-dark' };
    }
}

function formatDate(value) {
    if (!value) return '';
    var date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    var day = String(date.getDate()).padStart(2, '0');
    var month = String(date.getMonth() + 1).padStart(2, '0');
    return day + '/' + month + '/' + date.getFullYear();
}

function formatDateTime(value) {
    if (!value) return '';
    var date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    var day = String(date.getDate()).padStart(2, '0');
    var month = String(date.getMonth() + 1).padStart(2, '0');
    var hours = String(date.getHours()).padStart(2, '0');
    var minutes = String(date.getMinutes()).padStart(2, '0');
    return day + '/' + month + '/' + date.getFullYear() + ' ' + hours + ':' + minutes;
}

function escapeHtml(value) {
    if (value == null) return '';
    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function initLeaveBalanceTooltips() {
    if (typeof bootstrap === 'undefined' || !bootstrap.Tooltip) {
        return;
    }

    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (element) {
        bootstrap.Tooltip.getOrCreateInstance(element);
    });
}

async function saveLeaveBalance() {
    var employeeId = parseInt($('#leaveBalanceEmployeeId').val(), 10);
    var allowedRaw = $('#allowedLeaveDays').val();
    var carryOverRaw = $('#carryOverLeaveDays').val();
    var expiredRaw = $('#expiredLeaveDays').val();

    var allowedLeaveDays = allowedRaw === '' ? null : parseFloat(allowedRaw);
    var carryOverLeaveDays = carryOverRaw === '' ? null : parseFloat(carryOverRaw);
    var expiredLeaveDays = expiredRaw === '' ? null : parseFloat(expiredRaw);

    if (!employeeId || employeeId <= 0) {
        alert('Mã nhân viên không hợp lệ.');
        return;
    }

    if (allowedLeaveDays !== null && Number.isNaN(allowedLeaveDays)) {
        alert('Số ngày phép năm không hợp lệ.');
        return;
    }

    if (carryOverLeaveDays !== null && Number.isNaN(carryOverLeaveDays)) {
        alert('Số ngày phép tồn năm cũ không hợp lệ.');
        return;
    }

    if (expiredLeaveDays !== null && Number.isNaN(expiredLeaveDays)) {
        alert('Số ngày phép hết hạn không hợp lệ.');
        return;
    }

    var payload = {
        EmployeeId: employeeId,
        AllowedLeaveDays: allowedLeaveDays,
        CarryOverLeaveDays: carryOverLeaveDays,
        ExpiredLeaveDays: expiredLeaveDays
    };

    try {
        var response = await fetch('?handler=UpdateLeaveBalance', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify(payload)
        });

        var result = await response.json();
        if (response.ok && result.success) {
            alert('Cập nhật thành công.');
            location.reload();
            return;
        }

        if (Array.isArray(result)) {
            var message = result.map(function (item) { return item.Content; }).join('\n');
            alert(message || 'Có lỗi xảy ra.');
        } else {
            alert(result.message || 'Có lỗi xảy ra.');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Lỗi hệ thống.');
    }
}

document.addEventListener('DOMContentLoaded', initLeaveBalanceTooltips);
