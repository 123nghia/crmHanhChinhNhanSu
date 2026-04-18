let isSavingLeave = false;

function setLeaveSaveState(isSaving) {
    isSavingLeave = isSaving;

    const saveButton = $('#leaveSaveButton');
    if (saveButton.length === 0) {
        return;
    }

    saveButton.prop('disabled', isSaving);
    saveButton.text(isSaving ? 'Đang lưu...' : 'Lưu lại');
}

function resetLeaveSaveState() {
    setLeaveSaveState(false);
}

function openAddLeave() {
    $('#leaveId').val(0);
    $('#leaveForm')[0].reset();
    $('#leaveModalTitle').text('Đăng ký nghỉ phép');
    resetLeaveSaveState();
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
    if (isSavingLeave) {
        return;
    }

    const data = {
        Id: parseInt($('#leaveId').val(), 10),
        LeaveTypeCode: $('#leaveType').val(),
        FromDate: $('#fromDate').val(),
        ToDate: $('#toDate').val(),
        NumDays: parseFloat($('#numDays').val()),
        Reason: $('#reason').val(),
        HandoverEmployeeId: $('#handoverEmployeeId').val() ? parseInt($('#handoverEmployeeId').val(), 10) : null
    };

    if (!data.FromDate || !data.ToDate || data.NumDays <= 0 || !data.Reason) {
        alert('Vui lòng điền đầy đủ thông tin hợp lệ.');
        return;
    }

    if (data.LeaveTypeCode === 'NP' && data.FromDate) {
        const startDate = new Date(`${data.FromDate}T00:00:00`);
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const minDate = new Date(today);
        minDate.setDate(minDate.getDate() + 1);
        if (startDate < minDate) {
            alert('Nghỉ phép năm phải đăng ký trước ít nhất 1 ngày.');
            return;
        }
    }

    setLeaveSaveState(true);

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
            alert('Lưu thành công.');
            location.reload();
        } else {
            alert(result.message || 'Có lỗi xảy ra.');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Lỗi hệ thống.');
    } finally {
        resetLeaveSaveState();
    }
}

async function deleteLeave(id) {
    if (!confirm('Bạn có chắc chắn muốn xóa yêu cầu này?')) {
        return;
    }

    try {
        const response = await fetch(`?handler=Delete&id=${id}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        });

        const result = await response.json();
        if (result.success) {
            alert('Xóa thành công.');
            location.reload();
        } else {
            alert(result.message || 'Có lỗi xảy ra.');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Lỗi hệ thống.');
    }
}

function openEditLeave(id) {
    fetch(`?handler=LeaveById&id=${id}`)
        .then(res => res.json())
        .then(data => {
            resetLeaveSaveState();
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

function escapeWorkflowHtml(value) {
    return String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

function formatWorkflowDate(value) {
    if (!value) {
        return '--';
    }

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? '--' : date.toLocaleString();
}

async function viewWorkflowDetail(id) {
    try {
        const res = await fetch(`?handler=WorkflowDetail&id=${id}`);
        const data = await res.json();
        if (!data.success) {
            alert(data.message || 'Forbidden');
            return;
        }

        renderWorkflowDetail(data);
        $('#workflowModal').modal('show');
    } catch (error) {
        console.error('Error fetching workflow detail:', error);
        alert('Lá»—i há»‡ thá»‘ng.');
    }
}

function renderWorkflowDetail(data) {
    const leave = data.leave || {};
    const statusBadge = data.isOverdue
        ? '<span class="badge bg-danger">OVERDUE</span>'
        : '<span class="badge bg-success">ON TRACK</span>';

    $('#workflowRequestCode').text(`LEAVE-${leave.id}`);
    $('#workflowTitle').text(`${leave.employeeName || ''} - ${leave.leaveTypeName || leave.leaveTypeCode || ''}`);
    $('#workflowHeader').html(`
        <div class="workflow-kpi"><div class="workflow-kpi-label">Status</div><div class="workflow-kpi-value">${escapeWorkflowHtml(data.currentStep)} ${statusBadge}</div></div>
        <div class="workflow-kpi"><div class="workflow-kpi-label">Current owner</div><div class="workflow-kpi-value">${escapeWorkflowHtml(data.currentOwner || '--')}</div></div>
        <div class="workflow-kpi"><div class="workflow-kpi-label">Deadline</div><div class="workflow-kpi-value">${formatWorkflowDate(data.dueAt)}</div></div>
        <div class="workflow-kpi"><div class="workflow-kpi-label">Attendance sync</div><div class="workflow-kpi-value">${escapeWorkflowHtml(leave.attendanceSyncStatus || '--')}</div></div>
    `);
    $('#workflowActionArea').html('<span class="text-muted">Track status here. Approval action is available in the approval inbox.</span>');
    $('#workflowBusinessInfo').html(`
        <div><strong>Employee:</strong> ${escapeWorkflowHtml(leave.employeeName)}</div>
        <div><strong>Range:</strong> ${formatWorkflowDate(leave.fromDate)} - ${formatWorkflowDate(leave.toDate)}</div>
        <div><strong>Days:</strong> ${escapeWorkflowHtml(leave.numDays)}</div>
        <div><strong>Reason:</strong> ${escapeWorkflowHtml(leave.reason)}</div>
        <div><strong>Handover:</strong> ${escapeWorkflowHtml(leave.handoverEmployeeName || '--')}</div>
    `);
    $('#workflowAuditInfo').html(`
        <div><strong>Last sync:</strong> ${formatWorkflowDate(leave.lastAttendanceSyncAt)}</div>
        <div><strong>Sync error:</strong> ${escapeWorkflowHtml(leave.lastAttendanceSyncError || '--')}</div>
        <div><strong>Sync attempts:</strong> ${escapeWorkflowHtml(leave.attendanceSyncAttemptCount ?? '--')}</div>
    `);

    const events = [...(data.timeline || [])];
    if (events.length === 0) {
        (data.history || []).forEach(item => {
            events.push({
                occurredAt: item.actionTime,
                eventTitle: `${getActionText(item.action)} - ${item.actionByName || ''}`,
                eventCode: item.action,
                currentStatusText: item.statusAfter,
                errorMessage: item.comment
            });
        });
    }

    const timelineHtml = events.length === 0
        ? '<div class="text-muted">No timeline event yet.</div>'
        : events.map(item => `
            <div class="workflow-timeline-item">
                <div class="fw-bold">${escapeWorkflowHtml(item.eventTitle || item.eventCode || '')}</div>
                <div class="small text-muted">${formatWorkflowDate(item.occurredAt)} ${item.triggeredByName ? '- ' + escapeWorkflowHtml(item.triggeredByName) : ''}</div>
                <div class="small">${escapeWorkflowHtml(item.currentStatusText || '')}</div>
                ${item.emailSent ? '<span class="badge bg-success me-1">EMAIL SENT</span>' : ''}
                ${item.emailFailed ? '<span class="badge bg-danger me-1">EMAIL FAIL</span>' : ''}
                ${item.notificationCreated ? '<span class="badge bg-info text-dark me-1">NOTIFY</span>' : ''}
                ${item.attendanceSyncStatus ? `<span class="badge bg-secondary me-1">${escapeWorkflowHtml(item.attendanceSyncStatus)}</span>` : ''}
                ${item.errorMessage ? `<div class="text-danger small">${escapeWorkflowHtml(item.errorMessage)}</div>` : ''}
            </div>
        `).join('');
    $('#workflowTimeline').html(timelineHtml);
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

$(document).ready(function () {
    $('#leaveModal').on('hidden.bs.modal', resetLeaveSaveState);

    const leaveId = parseInt(new URLSearchParams(window.location.search).get('id') || '0', 10);
    if (leaveId > 0) {
        viewWorkflowDetail(leaveId);
    }
});
