function openAddLeave() {
    $('#leaveId').val(0);
    $('#leaveForm')[0].reset();
    $('#leaveModalTitle').text('Đăng ký nghỉ phép');
    $('#leaveModal').modal('show');
}

function calculateDays() {
    const fromDate = $('#fromDate').val();
    const toDate = $('#toDate').val();
    if (fromDate && toDate) {
        const start = new Date(fromDate);
        const end = new Date(toDate);
        if (end >= start) {
            const diffTime = Math.abs(end - start);
            const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;
            $('#numDays').val(diffDays);
        } else {
            $('#numDays').val(0);
        }
    }
}

async function saveLeave() {
    const data = {
        Id: parseInt($('#leaveId').val()),
        LeaveTypeCode: $('#leaveType').val(),
        FromDate: $('#fromDate').val(),
        ToDate: $('#toDate').val(),
        NumDays: parseFloat($('#numDays').val()),
        Reason: $('#reason').val(),
        HandoverEmployeeId: $('#handoverEmployeeId').val() ? parseInt($('#handoverEmployeeId').val()) : null
    };

    if (!data.FromDate || !data.ToDate || data.NumDays <= 0 || !data.Reason) {
        alert('Vui lòng điền đầy đủ thông tin hợp lệ');
        return;
    }
    if (data.LeaveTypeCode === 'NP' && data.FromDate) {
        const startDate = new Date(`${data.FromDate}T00:00:00`);
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const minDate = new Date(today);
        minDate.setDate(minDate.getDate() + 1);
        if (startDate < minDate) {
            alert('Nghi phep nam phai dang ky truoc it nhat 1 ngay.');
            return;
        }
    }

    try {
        const response = await fetch('?handler=Save', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify(data)
        });

        const result = await response.json();
        if (result.success) {
            alert('Lưu thành công');
            location.reload();
        } else {
            alert(result.message || 'Có lỗi xảy ra');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Lỗi hệ thống');
    }
}

async function deleteLeave(id) {
    if (!confirm('Bạn có chắc chắn muốn xóa yêu cầu này?')) return;

    try {
        const response = await fetch(`?handler=Delete&id=${id}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        });

        const result = await response.json();
        if (result.success) {
            alert('Xóa thành công');
            location.reload();
        } else {
            alert(result.message || 'Có lỗi xảy ra');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Lỗi hệ thống');
    }
}

function openEditLeave(id) {
    // In a real app, you might fetch data from the server or use data from the row
    // For now, I'll fetch by Id for accuracy
    fetch(`?handler=LeaveById&id=${id}`)
        .then(res => res.json())
        .then(data => {
            $('#leaveId').val(data.id);
            $('#leaveType').val(data.leaveTypeCode);
            $('#fromDate').val(data.fromDate.split('T')[0]);
            $('#toDate').val(data.toDate.split('T')[0]);
            $('#numDays').val(data.numDays);
            $('#reason').val(data.reason);
            $('#handoverEmployeeId').val(data.handoverEmployeeId || '');
            $('#leaveModalTitle').text('Chỉnh sửa nghỉ phép');
            $('#leaveModal').modal('show');
        });
}

async function viewHistory(id) {
    try {
        const res = await fetch(`?handler=LeaveHistory&id=${id}`);
        const data = await res.json();
        let html = '';
        data.forEach(item => {
            html += `<tr>
                <td>${new Date(item.actionTime).toLocaleString()}</td>
                <td>${item.actionByName}</td>
                <td><span class="badge ${getActionBadge(item.action)}">${item.action}</span></td>
                <td>${item.comment || ''}</td>
            </tr>`;
        });
        $('#historyContent').html(html);
        $('#historyModal').modal('show');
    } catch (error) {
        console.error('Error fetching history:', error);
    }
}

function getActionBadge(action) {
    switch (action) {
        case 'Create': return 'bg-primary';
        case 'Update': return 'bg-info';
        case 'Agree': return 'bg-success';
        case 'Reject': return 'bg-danger';
        case 'Acting': return 'bg-warning text-dark';
        case 'Cancel': return 'bg-secondary';
        default: return 'bg-light text-dark';
    }
}

