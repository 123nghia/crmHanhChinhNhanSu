function approveLeave(id, action) {
    $('#approveId').val(id);
    $('#approveAction').val(action);
    $('#approveComment').val('');
    $('#commentModal').modal('show');
}

async function submitApproval() {
    const data = {
        Id: parseInt($('#approveId').val(), 10),
        Action: $('#approveAction').val(),
        Comment: $('#approveComment').val()
    };

    try {
        const response = await fetch('?handler=Approve', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify(data)
        });

        const result = await response.json();
        if (result.success) {
            alert('Thao tác thành công.');
            location.reload();
        } else {
            alert(result.message || 'Có lỗi xảy ra.');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Lỗi hệ thống.');
    }
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
                <td><span class="badge ${getActionBadge(item.action)}">${getActionText(item.action)}</span></td>
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

function getActionText(action) {
    switch (action) {
        case 'Create': return 'Tạo mới';
        case 'Update': return 'Cập nhật';
        case 'Agree': return 'Phê duyệt';
        case 'Reject': return 'Từ chối';
        case 'Acting': return 'Duyệt thay';
        case 'Cancel': return 'Hủy';
        default: return action || '';
    }
}
