function openLeaveBalanceModal(button) {
    var row = button.closest('tr');
    if (!row) return;

    var employeeId = row.getAttribute('data-id');
    var name = row.getAttribute('data-name') || '';
    var allowed = row.getAttribute('data-allowed') || '0';
    var carryOver = row.getAttribute('data-carryover') || '0';
    var used = row.getAttribute('data-used') || '0';

    $('#leaveBalanceEmployeeId').val(employeeId);
    $('#leaveBalanceEmployeeName').val(name);
    $('#allowedLeaveDays').val(allowed);
    $('#carryOverLeaveDays').val(carryOver);
    $('#usedLeaveDays').val(used);
    $('#leaveBalanceModal').modal('show');
}

function openLeaveDetailsModal(button) {
    var row = button.closest('tr');
    if (!row) return;

    var employeeId = parseInt(row.getAttribute('data-id'), 10);
    var name = row.getAttribute('data-name') || '';

    if (!employeeId || employeeId <= 0) {
        alert('EmployeeId khong hop le');
        return;
    }

    $('#leaveDetailEmployeeName').val(name);
    $('#leaveDetailContent').html('<tr><td colspan="8" class="text-center">Dang tai...</td></tr>');
    $('#leaveDetailModal').modal('show');

    fetch('?handler=LeaveDetails&employeeId=' + employeeId)
        .then(function (res) { return res.json(); })
        .then(function (data) {
            if (!Array.isArray(data) || data.length === 0) {
                $('#leaveDetailContent').html('<tr><td colspan="8" class="text-center">Khong co du lieu</td></tr>');
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
            $('#leaveDetailContent').html('<tr><td colspan="8" class="text-center">Loi he thong</td></tr>');
        });
}

function getLeaveStatusInfo(status) {
    switch (status) {
        case 0:
            return { text: 'Cho Lead duyet', className: 'bg-warning text-dark' };
        case 1:
            return { text: 'Cho HCNS duyet', className: 'bg-info text-dark' };
        case 2:
            return { text: 'Cho BGD duyet', className: 'bg-primary' };
        case 3:
            return { text: 'Da duyet', className: 'bg-success' };
        case 4:
            return { text: 'Da duyet (HCNS)', className: 'bg-success' };
        case 5:
            return { text: 'Tu choi', className: 'bg-danger' };
        case 6:
            return { text: 'Da huy', className: 'bg-secondary' };
        default:
            return { text: 'Khong ro', className: 'bg-light text-dark' };
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

async function saveLeaveBalance() {
    var employeeId = parseInt($('#leaveBalanceEmployeeId').val(), 10);
    var allowedRaw = $('#allowedLeaveDays').val();
    var carryOverRaw = $('#carryOverLeaveDays').val();
    var usedRaw = $('#usedLeaveDays').val();

    var allowedLeaveDays = allowedRaw === '' ? null : parseFloat(allowedRaw);
    var carryOverLeaveDays = carryOverRaw === '' ? null : parseFloat(carryOverRaw);
    var usedLeaveDays = usedRaw === '' ? null : parseFloat(usedRaw);

    if (!employeeId || employeeId <= 0) {
        alert('EmployeeId khong hop le');
        return;
    }

    if (allowedLeaveDays !== null && Number.isNaN(allowedLeaveDays)) {
        alert('So ngay duoc huong khong hop le');
        return;
    }

    if (carryOverLeaveDays !== null && Number.isNaN(carryOverLeaveDays)) {
        alert('So ngay phep ton nam cu khong hop le');
        return;
    }

    if (usedLeaveDays !== null && Number.isNaN(usedLeaveDays)) {
        alert('So ngay da dung khong hop le');
        return;
    }

    var payload = {
        EmployeeId: employeeId,
        AllowedLeaveDays: allowedLeaveDays,
        CarryOverLeaveDays: carryOverLeaveDays,
        UsedLeaveDays: usedLeaveDays
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
            alert('Cap nhat thanh cong');
            location.reload();
            return;
        }

        if (Array.isArray(result)) {
            var message = result.map(function (item) { return item.Content; }).join('\n');
            alert(message || 'Co loi xay ra');
        } else {
            alert(result.message || 'Co loi xay ra');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Loi he thong');
    }
}
