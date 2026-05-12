let isSubmittingLeaveApproval = false;
let pendingLeaveQueryAction = null;
let currentWorkflowContext = {
    leaveId: 0,
    viewMode: 'modal'
};

function approveLeave(id, action) {
    currentWorkflowContext.leaveId = id;
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
        if (!result.success) {
            alert(result.message || 'Có lỗi xảy ra.');
            return;
        }

        $('#commentModal').modal('hide');
        alert('Thao tác thành công.');

        const deepLinkLeaveId = getDeepLinkLeaveId();
        if (deepLinkLeaveId > 0) {
            updateDeepLinkUrl(data.Id);
            await viewWorkflowDetail(data.Id, {
                viewMode: hasInlineWorkflowView() ? 'inline' : currentWorkflowContext.viewMode,
                skipRequestedAction: true
            });
            return;
        }

        window.location.href = window.location.pathname;
    } catch (error) {
        console.error('Error:', error);
        alert('Lỗi hệ thống.');
    } finally {
        isSubmittingLeaveApproval = false;
    }
}

function getDeepLinkLeaveId() {
    const params = new URLSearchParams(window.location.search);
    return parseInt(params.get('id') || '0', 10);
}

function updateDeepLinkUrl(leaveId) {
    const nextUrl = new URL(window.location.href);
    nextUrl.searchParams.set('id', leaveId);
    nextUrl.searchParams.delete('action');
    window.history.replaceState({}, '', nextUrl.toString());
}

async function viewHistory(id) {
    try {
        const res = await fetch(`?handler=LeaveHistory&id=${id}`);
        const data = await res.json();
        if (!Array.isArray(data)) {
            alert(data.message || 'Bạn không có quyền xem lịch sử xử lý.');
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

function getWorkflowAlertClass(tone) {
    switch (tone) {
        case 'success':
            return 'alert-success';
        case 'danger':
            return 'alert-danger';
        case 'warning':
            return 'alert-warning';
        case 'secondary':
            return 'alert-secondary';
        case 'info':
        default:
            return 'alert-info';
    }
}

function buildWorkflowStateAlert(data) {
    const message = data.stateMessage || 'Đang hiển thị trạng thái hiện tại của đơn nghỉ phép.';
    return `<div class="alert ${getWorkflowAlertClass(data.stateTone)} mb-3">${escapeWorkflowHtml(message)}</div>`;
}

function buildWorkflowStatusBadge(data) {
    if (!data.dueAt) {
        return '';
    }

    return data.isOverdue
        ? '<span class="badge bg-danger ms-1">Quá hạn</span>'
        : '<span class="badge bg-success ms-1">Đúng hạn</span>';
}

function buildWorkflowActionArea(leaveId, data) {
    const buttons = [];

    if (data.canApprove) {
        buttons.push(`<button class="btn btn-success btn-sm" onclick="approveLeave(${leaveId}, 'Agree')"><i class="bi bi-check2"></i> Phê duyệt</button>`);
    }

    if (data.canReject) {
        buttons.push(`<button class="btn btn-danger btn-sm" onclick="approveLeave(${leaveId}, 'Reject')"><i class="bi bi-x-lg"></i> Từ chối</button>`);
    }

    if (data.canActingApprove) {
        buttons.push(`<button class="btn btn-outline-primary btn-sm" onclick="approveLeave(${leaveId}, 'Acting')"><i class="bi bi-shield-check"></i> Duyệt thay BGĐ</button>`);
    }

    if (buttons.length === 0) {
        return `
            ${buildWorkflowStateAlert(data)}
            <span class="text-muted">Đơn này hiện không có thao tác xử lý trực tiếp dành cho bạn.</span>
        `;
    }

    return `
        ${buildWorkflowStateAlert(data)}
        <div class="d-flex flex-wrap gap-2 align-items-center">
            <strong class="me-2">Thao tác khả dụng</strong>
            ${buttons.join('')}
        </div>
    `;
}

function buildWorkflowTimelineHtml(data) {
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

    if (events.length === 0) {
        return '<div class="text-muted">Chưa có dữ liệu tiến trình xử lý.</div>';
    }

    return events.map(item => `
        <div class="workflow-timeline-item">
            <div class="fw-bold">${escapeWorkflowHtml(item.eventTitle || item.eventCode || '')}</div>
            <div class="small text-muted">${formatWorkflowDate(item.occurredAt)} ${item.triggeredByName ? '- ' + escapeWorkflowHtml(item.triggeredByName) : ''}</div>
            <div class="small">${escapeWorkflowHtml(item.currentStatusText || '')}</div>
            ${item.emailSent ? '<span class="badge bg-success me-1">Đã gửi mail</span>' : ''}
            ${item.emailFailed ? '<span class="badge bg-danger me-1">Lỗi gửi mail</span>' : ''}
            ${item.notificationCreated ? '<span class="badge bg-info text-dark me-1">Đã tạo thông báo</span>' : ''}
            ${item.attendanceSyncStatus ? `<span class="badge bg-secondary me-1">${escapeWorkflowHtml(item.attendanceSyncStatus)}</span>` : ''}
            ${item.errorMessage ? `<div class="text-danger small">${escapeWorkflowHtml(item.errorMessage)}</div>` : ''}
        </div>
    `).join('');
}

function getWorkflowTargets(viewMode) {
    if (viewMode === 'inline') {
        return {
            requestCode: '#inlineWorkflowRequestCode',
            title: '#inlineWorkflowTitle',
            header: '#inlineWorkflowHeader',
            actionArea: '#inlineWorkflowActionArea',
            businessInfo: '#inlineWorkflowBusinessInfo',
            auditInfo: '#inlineWorkflowAuditInfo',
            timeline: '#inlineWorkflowTimeline'
        };
    }

    return {
        requestCode: '#workflowRequestCode',
        title: '#workflowTitle',
        header: '#workflowHeader',
        actionArea: '#workflowActionArea',
        businessInfo: '#workflowBusinessInfo',
        auditInfo: '#workflowAuditInfo',
        timeline: '#workflowTimeline'
    };
}

function renderWorkflowDetail(data, viewMode = 'modal') {
    const leave = data.leave || {};
    const targets = getWorkflowTargets(viewMode);
    const statusBadge = buildWorkflowStatusBadge(data);
    const ownerLabel = leave.status === 0 || leave.status === 1 || leave.status === 2
        ? 'Người xử lý hiện tại'
        : 'Người xử lý cuối';

    $(targets.requestCode).text(`LEAVE-${leave.id}`);
    $(targets.title).text(`${leave.employeeName || ''} - ${leave.leaveTypeName || leave.leaveTypeCode || ''}`);
    $(targets.header).html(`
        <div class="workflow-kpi"><div class="workflow-kpi-label">Trạng thái</div><div class="workflow-kpi-value">${escapeWorkflowHtml(data.currentStep)} ${statusBadge}</div></div>
        <div class="workflow-kpi"><div class="workflow-kpi-label">${ownerLabel}</div><div class="workflow-kpi-value">${escapeWorkflowHtml(data.currentOwner || '--')}</div></div>
        <div class="workflow-kpi"><div class="workflow-kpi-label">Hạn xử lý</div><div class="workflow-kpi-value">${formatWorkflowDate(data.dueAt)}</div></div>
        <div class="workflow-kpi"><div class="workflow-kpi-label">Đồng bộ chấm công</div><div class="workflow-kpi-value">${escapeWorkflowHtml(leave.attendanceSyncStatus || '--')}</div></div>
    `);

    $(targets.actionArea).html(buildWorkflowActionArea(leave.id, data));
    $(targets.businessInfo).html(`
        <div><strong>Nhân viên:</strong> ${escapeWorkflowHtml(leave.employeeName)}</div>
        <div><strong>Thời gian nghỉ:</strong> ${formatWorkflowDate(leave.fromDate)} - ${formatWorkflowDate(leave.toDate)}</div>
        <div><strong>Số ngày:</strong> ${escapeWorkflowHtml(leave.numDays)}</div>
        <div><strong>Lý do:</strong> ${escapeWorkflowHtml(leave.reason)}</div>
        <div><strong>Người bàn giao:</strong> ${escapeWorkflowHtml(leave.handoverEmployeeName || '--')}</div>
        <div><strong>Người duyệt cấp quản lý:</strong> ${escapeWorkflowHtml(leave.leadApproverName || '--')}</div>
        <div><strong>Người duyệt HCNS:</strong> ${escapeWorkflowHtml(leave.hcnsApproverName || '--')}</div>
        <div><strong>Người duyệt BGD:</strong> ${escapeWorkflowHtml(leave.bgdApproverName || '--')}</div>
    `);

    $(targets.auditInfo).html(`
        <div><strong>Ghi chú quản lý:</strong> ${escapeWorkflowHtml(leave.leadComment || '--')}</div>
        <div><strong>Ghi chú HCNS:</strong> ${escapeWorkflowHtml(leave.hcnsComment || '--')}</div>
        <div><strong>Ghi chú BGD:</strong> ${escapeWorkflowHtml(leave.bgdComment || '--')}</div>
        <div><strong>Lần đồng bộ gần nhất:</strong> ${formatWorkflowDate(leave.lastAttendanceSyncAt)}</div>
        <div><strong>Lỗi đồng bộ:</strong> ${escapeWorkflowHtml(leave.lastAttendanceSyncError || '--')}</div>
        <div><strong>Số lần thử đồng bộ:</strong> ${escapeWorkflowHtml(leave.attendanceSyncAttemptCount ?? '--')}</div>
    `);

    $(targets.timeline).html(buildWorkflowTimelineHtml(data));
}

function hasInlineWorkflowView() {
    return $('#inlineWorkflowContent').length > 0;
}

function startInlineWorkflowLoading() {
    if (!hasInlineWorkflowView()) {
        return;
    }

    $('#inlineWorkflowLoading').removeClass('d-none');
    $('#inlineWorkflowError').addClass('d-none').text('');
    $('#inlineWorkflowContent').addClass('d-none');
}

function showInlineWorkflowContent() {
    if (!hasInlineWorkflowView()) {
        return;
    }

    $('#inlineWorkflowLoading').addClass('d-none');
    $('#inlineWorkflowError').addClass('d-none').text('');
    $('#inlineWorkflowContent').removeClass('d-none');
}

function showInlineWorkflowError(message) {
    if (!hasInlineWorkflowView()) {
        return;
    }

    $('#inlineWorkflowLoading').addClass('d-none');
    $('#inlineWorkflowContent').addClass('d-none');
    $('#inlineWorkflowError').removeClass('d-none').text(message || 'Không thể tải chi tiết đơn nghỉ phép.');
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

async function viewWorkflowDetail(id, options = {}) {
    const viewMode = options.viewMode || 'modal';
    currentWorkflowContext = {
        leaveId: id,
        viewMode
    };

    if (viewMode === 'inline') {
        startInlineWorkflowLoading();
    }

    try {
        const res = await fetch(`?handler=WorkflowDetail&id=${id}`, {
            credentials: 'same-origin',
            headers: {
                'Accept': 'application/json'
            }
        });
        const contentType = res.headers.get('content-type') || '';
        if (!contentType.includes('application/json')) {
            throw new Error('Phiên đăng nhập không còn hợp lệ hoặc máy chủ không trả về dữ liệu JSON.');
        }

        const data = await res.json();
        if (!data.success) {
            if (viewMode === 'inline') {
                showInlineWorkflowError(data.message || 'Không tìm thấy đơn nghỉ phép.');
                return;
            }

            alert(data.message || 'Bạn không có quyền xem đơn nghỉ phép này.');
            return;
        }

        renderWorkflowDetail(data, viewMode);

        if (viewMode === 'inline') {
            showInlineWorkflowContent();
        } else {
            $('#workflowModal').modal('show');
        }

        if (!options.skipRequestedAction) {
            tryOpenRequestedAction(data.leave || {}, data);
        }
    } catch (error) {
        console.error('Error fetching workflow detail:', error);
        const errorMessage = error && error.message
            ? error.message
            : 'Lỗi hệ thống.';
        if (viewMode === 'inline') {
            showInlineWorkflowError(errorMessage);
            return;
        }

        alert(errorMessage);
    }
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
            return 'Tạo mới';
        case 'Update':
            return 'Cập nhật';
        case 'Agree':
            return 'Phê duyệt';
        case 'Reject':
            return 'Từ chối';
        case 'Acting':
            return 'Duyệt thay';
        case 'Cancel':
            return 'Hủy';
        default:
            return action || '';
    }
}

$(document).ready(function () {
    const params = new URLSearchParams(window.location.search);
    const leaveId = parseInt(params.get('id') || '0', 10);
    pendingLeaveQueryAction = normalizeApprovalAction(params.get('action'));

    if (leaveId > 0) {
        viewWorkflowDetail(leaveId, {
            viewMode: hasInlineWorkflowView() ? 'inline' : 'modal'
        });
    }
});
