function approveLateEarly(id, action) {
    $('#approveId').val(id);
    $('#approveAction').val(action);
    $('#approveComment').val('');
    $('#commentModal').modal('show');
}

async function submitLateEarlyApproval() {
    const id = parseInt($('#approveId').val()) || 0;
    const action = $('#approveAction').val();
    const comment = $('#approveComment').val();

    if (!id || !action) {
        alert('Dữ liệu không hợp lệ');
        return;
    }

    try {
        const response = await fetch('?handler=Approve', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify({ Id: id, Action: action, Comment: comment })
        });
        const result = await response.json();
        if (result.success) {
            alert('Cập nhật thành công');
            location.reload();
        } else {
            alert(result.message || 'Có lỗi xảy ra');
        }
    } catch (error) {
        console.error(error);
        alert('Lỗi hệ thống');
    }
}

async function viewLateEarlyHistory(id) {
    try {
        const response = await fetch(`?handler=History&id=${id}`);
        const data = await response.json();
        if (!data || data.length === 0) {
            $('#historyContent').html('<tr><td colspan="4" class="text-center">Không có dữ liệu</td></tr>');
        } else {
            let html = '';
            data.forEach(item => {
                const timeText = item.actionTime ? new Date(item.actionTime).toLocaleString('vi-VN') : '';
                html += `<tr>
                    <td>${timeText}</td>
                    <td>${item.actionByName || ''}</td>
                    <td>${item.action || ''}</td>
                    <td>${item.comment || ''}</td>
                </tr>`;
            });
            $('#historyContent').html(html);
        }
        $('#historyModal').modal('show');
    } catch (error) {
        console.error(error);
        alert('Lỗi hệ thống');
    }
}
