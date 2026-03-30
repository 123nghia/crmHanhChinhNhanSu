let isSavingLateEarly = false;

function setLateEarlySaveState(isSaving) {
    isSavingLateEarly = isSaving;

    const saveButton = $('#lateEarlySaveButton');
    if (saveButton.length === 0) {
        return;
    }

    saveButton.prop('disabled', isSaving);
    saveButton.text(isSaving ? 'Đang lưu...' : 'Lưu');
}

function resetLateEarlySaveState() {
    setLateEarlySaveState(false);
}

function openAddLateEarly() {
    $('#lateEarlyId').val(0);
    $('#lateEarlyForm')[0].reset();
    $('#lateEarlyModalTitle').text('Tạo yêu cầu');
    resetLateEarlySaveState();
    $('#lateEarlyModal').modal('show');
}

async function openEditLateEarly(id) {
    try {
        const response = await fetch(`?handler=ById&id=${id}`);
        const data = await response.json();
        if (!data) {
            alert('Không tìm thấy dữ liệu');
            return;
        }

        $('#lateEarlyId').val(data.id || data.Id);
        $('#requestType').val(data.requestType || data.RequestType || 'LATE');

        const dateText = (data.requestDate || data.RequestDate) ? new Date(data.requestDate || data.RequestDate) : null;
        const startText = (data.startTime || data.StartTime) ? new Date(data.startTime || data.StartTime) : null;
        const endText = (data.endTime || data.EndTime) ? new Date(data.endTime || data.EndTime) : null;

        if (dateText) $('#requestDate').val(dateText.toISOString().slice(0, 10));
        if (startText) $('#startTime').val(formatTime(startText));
        if (endText) $('#endTime').val(formatTime(endText));

        $('#reason').val(data.reason || data.Reason || '');
        $('#lateEarlyModalTitle').text('Chỉnh sửa yêu cầu');
        resetLateEarlySaveState();
        $('#lateEarlyModal').modal('show');
    } catch (error) {
        console.error(error);
        alert('Lỗi hệ thống');
    }
}

function formatTime(date) {
    return date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false });
}

function buildDateTime(dateValue, timeValue) {
    return `${dateValue}T${timeValue}:00`;
}

async function saveLateEarly() {
    if (isSavingLateEarly) {
        return;
    }

    const id = parseInt($('#lateEarlyId').val()) || 0;
    const requestType = $('#requestType').val();
    const requestDate = $('#requestDate').val();
    const startTime = $('#startTime').val();
    const endTime = $('#endTime').val();
    const reason = $('#reason').val();

    if (!requestType || !requestDate || !startTime || !endTime || !reason) {
        alert('Vui lòng nhập đầy đủ thông tin');
        return;
    }

    const data = {
        Id: id,
        RequestType: requestType,
        RequestDate: requestDate,
        StartTime: buildDateTime(requestDate, startTime),
        EndTime: buildDateTime(requestDate, endTime),
        Reason: reason
    };

    setLateEarlySaveState(true);

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
        console.error(error);
        alert('Lỗi hệ thống');
    } finally {
        resetLateEarlySaveState();
    }
}

async function deleteLateEarly(id) {
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
            alert(result.message || 'Không thể xóa');
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

$(document).ready(function () {
    $('#lateEarlyModal').on('hidden.bs.modal', resetLateEarlySaveState);
});
