let isSubmittingLeaveApproval = false;

function approveLeave(id, action) {
    $('#approveId').val(id);
    $('#approveAction').val(action);
    $('#approveComment').val('');
    $('#commentModal').modal('show');
}

async function submitApproval() {
    if (isSubmittingLeaveApproval) {
        return;
    }

    const data = {
        Id: parseInt($('#approveId').val(), 10),
        Action: $('#approveAction').val(),
        Comment: $('#approveComment').val()
    };

    isSubmittingLeaveApproval = true;

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
    } finally {
        isSubmittingLeaveApproval = false;
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

    const actions = data.canAct ? `
        <div class="d-flex flex-wrap gap-2 align-items-center">
            <strong class="me-2">Action required</strong>
            <button class="btn btn-success btn-sm" onclick="approveLeave(${leave.id}, 'Agree')"><i class="bi bi-check2"></i> Approve</button>
            <button class="btn btn-danger btn-sm" onclick="approveLeave(${leave.id}, 'Reject')"><i class="bi bi-x-lg"></i> Reject</button>
            <button class="btn btn-outline-primary btn-sm" onclick="approveLeave(${leave.id}, 'Acting')"><i class="bi bi-shield-check"></i> Acting approve</button>
        </div>`
        : '<span class="text-muted">No direct action is required from you.</span>';
    $('#workflowActionArea').html(actions);

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
    const leaveId = parseInt(new URLSearchParams(window.location.search).get('id') || '0', 10);
    if (leaveId > 0) {
        viewWorkflowDetail(leaveId);
    }
});
