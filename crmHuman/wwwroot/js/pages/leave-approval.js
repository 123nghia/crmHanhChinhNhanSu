let isSubmittingLeaveApproval = false;
let pendingLeaveQueryAction = null;

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
            alert('Thao tac thanh cong.');
            window.location.href = window.location.pathname;
        } else {
            alert(result.message || 'Co loi xay ra.');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Loi he thong.');
    } finally {
        isSubmittingLeaveApproval = false;
    }
}

async function viewHistory(id) {
    try {
        const res = await fetch(`?handler=LeaveHistory&id=${id}`);
        const data = await res.json();
        if (!Array.isArray(data)) {
            alert(data.message || 'Forbidden');
            return;
        }

        const html = data.map(item => `
            <tr>
                <td>${new Date(item.actionTime).toLocaleString()}</td>
                <td>${escapeWorkflowHtml(item.actionByName)}</td>
                <td><span class="badge ${getActionBadge(item.action)}">${getActionText(item.action)}</span></td>
                <td>${escapeWorkflowHtml(item.comment || '')}</td>
            </tr>
        `).join('');

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

function normalizeApprovalAction(action) {
    if (!action) {
        return null;
    }

    const normalized = String(action).trim().toLowerCase();
    if (normalized === 'agree') {
        return 'Agree';
    }

    if (normalized === 'reject') {
        return 'Reject';
    }

    if (normalized === 'acting') {
        return 'Acting';
    }

    return null;
}

function isRequestedActionAllowed(action, data) {
    if (action === 'Agree') {
        return !!data.canApprove;
    }

    if (action === 'Reject') {
        return !!data.canReject;
    }

    if (action === 'Acting') {
        return !!data.canActingApprove;
    }

    return false;
}

function buildWorkflowActionArea(leaveId, data) {
    const buttons = [];

    if (data.canApprove) {
        buttons.push(`<button class="btn btn-success btn-sm" onclick="approveLeave(${leaveId}, 'Agree')"><i class="bi bi-check2"></i> Approve</button>`);
    }

    if (data.canReject) {
        buttons.push(`<button class="btn btn-danger btn-sm" onclick="approveLeave(${leaveId}, 'Reject')"><i class="bi bi-x-lg"></i> Reject</button>`);
    }

    if (data.canActingApprove) {
        buttons.push(`<button class="btn btn-outline-primary btn-sm" onclick="approveLeave(${leaveId}, 'Acting')"><i class="bi bi-shield-check"></i> Acting approve</button>`);
    }

    if (buttons.length === 0) {
        return '<span class="text-muted">No direct action is required from you.</span>';
    }

    return `
        <div class="d-flex flex-wrap gap-2 align-items-center">
            <strong class="me-2">Action required</strong>
            ${buttons.join('')}
        </div>
    `;
}

function tryOpenRequestedAction(leave, data) {
    if (!pendingLeaveQueryAction || !leave || !leave.id) {
        return;
    }

    const requestedAction = pendingLeaveQueryAction;
    pendingLeaveQueryAction = null;

    if (!isRequestedActionAllowed(requestedAction, data)) {
        return;
    }

    approveLeave(leave.id, requestedAction);
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
        tryOpenRequestedAction(data.leave || {}, data);
    } catch (error) {
        console.error('Error fetching workflow detail:', error);
        alert('Loi he thong.');
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

    $('#workflowActionArea').html(buildWorkflowActionArea(leave.id, data));

    $('#workflowBusinessInfo').html(`
        <div><strong>Employee:</strong> ${escapeWorkflowHtml(leave.employeeName)}</div>
        <div><strong>Range:</strong> ${formatWorkflowDate(leave.fromDate)} - ${formatWorkflowDate(leave.toDate)}</div>
        <div><strong>Days:</strong> ${escapeWorkflowHtml(leave.numDays)}</div>
        <div><strong>Reason:</strong> ${escapeWorkflowHtml(leave.reason)}</div>
        <div><strong>Handover:</strong> ${escapeWorkflowHtml(leave.handoverEmployeeName || '--')}</div>
        <div><strong>Lead approver:</strong> ${escapeWorkflowHtml(leave.leadApproverName || '--')}</div>
        <div><strong>HCNS approver:</strong> ${escapeWorkflowHtml(leave.hcnsApproverName || '--')}</div>
        <div><strong>BGD approver:</strong> ${escapeWorkflowHtml(leave.bgdApproverName || '--')}</div>
    `);

    $('#workflowAuditInfo').html(`
        <div><strong>Lead note:</strong> ${escapeWorkflowHtml(leave.leadComment || '--')}</div>
        <div><strong>HCNS note:</strong> ${escapeWorkflowHtml(leave.hcnsComment || '--')}</div>
        <div><strong>BGD note:</strong> ${escapeWorkflowHtml(leave.bgdComment || '--')}</div>
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
        case 'Create':
            return 'bg-primary';
        case 'Update':
            return 'bg-info';
        case 'Agree':
            return 'bg-success';
        case 'Reject':
            return 'bg-danger';
        case 'Acting':
            return 'bg-warning text-dark';
        case 'Cancel':
            return 'bg-secondary';
        default:
            return 'bg-light text-dark';
    }
}

function getActionText(action) {
    switch (action) {
        case 'Create':
            return 'Tao moi';
        case 'Update':
            return 'Cap nhat';
        case 'Agree':
            return 'Phe duyet';
        case 'Reject':
            return 'Tu choi';
        case 'Acting':
            return 'Duyet thay';
        case 'Cancel':
            return 'Huy';
        default:
            return action || '';
    }
}

$(document).ready(function () {
    const params = new URLSearchParams(window.location.search);
    const leaveId = parseInt(params.get('id') || '0', 10);
    pendingLeaveQueryAction = normalizeApprovalAction(params.get('action'));

    if (leaveId > 0) {
        viewWorkflowDetail(leaveId);
    }
});
