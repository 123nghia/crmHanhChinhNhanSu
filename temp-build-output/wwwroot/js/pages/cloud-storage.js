function showFolderModal() {
    $('#folderName').val('');
    $('#folderModal').modal('show');
}

function showUploadModal() {
    $('#fileInput').val('');
    $('#selectedFileName').text('');
    $('#btnDoUpload').attr('disabled', true);
    $('#uploadModal').modal('show');
}

function handleFileSelect(input) {
    if (input.files && input.files[0]) {
        $('#selectedFileName').text('Đã chọn: ' + input.files[0].name);
        $('#btnDoUpload').attr('disabled', false);
    }
}

async function createFolder() {
    const name = $('#folderName').val();
    if (!name) return alert('Vui lòng nhập tên thư mục');

    const params = new URLSearchParams(window.location.search);
    const parentId = params.get('parentId');

    try {
        const response = await fetch('?handler=CreateFolder', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: `name=${encodeURIComponent(name)}&parentId=${parentId || ''}`
        });
        const result = await response.json();
        if (result.success) {
            location.reload();
        } else {
            alert('Có lỗi xảy ra khi tạo thư mục');
        }
    } catch (e) {
        console.error(e);
        alert('Có lỗi kết nối');
    }
}

async function uploadFile() {
    const fileInput = document.getElementById('fileInput');
    const file = fileInput.files[0];
    if (!file) return;

    const params = new URLSearchParams(window.location.search);
    const parentId = params.get('parentId');

    const formData = new FormData();
    formData.append('file', file);
    if (parentId) formData.append('parentId', parentId);

    $('#btnDoUpload').attr('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span> Đang tải...');

    try {
        const response = await fetch('?handler=UploadFile', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: formData
        });
        const result = await response.json();
        if (result.success) {
            location.reload();
        } else {
            alert('Lỗi tải file: ' + (result.message || ''));
            $('#btnDoUpload').attr('disabled', false).text('Bắt đầu tải');
        }
    } catch (e) {
        console.error(e);
        alert('Có lỗi kết nối');
        $('#btnDoUpload').attr('disabled', false).text('Bắt đầu tải');
    }
}

async function confirmDelete(id) {
    if (!confirm('Bạn có chắc chắn muốn xóa tài liệu này? Thao tác này không thể hoàn tác.')) return;

    try {
        const response = await fetch('?handler=Delete', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: `id=${id}`
        });
        const result = await response.json();
        if (result.success) {
            location.reload();
        } else {
            alert('Bạn không có quyền xóa hoặc lỗi hệ thống');
        }
    } catch (e) {
        console.error(e);
    }
}

let currentAccessLevel = 0;
async function showShareModal(id, accessLevel) {
    $('#shareDocId').val(id);
    selectAccess(accessLevel, false);

    // Set share link
    const baseUrl = window.location.origin;
    $('#shareLinkInput').val(`${baseUrl}/Cloud/Storage?parentId=${id}`);

    // Load existing shares
    const response = await fetch(`?handler=Shares&id=${id}`);
    const userIds = await response.json();

    $('#shareList').empty();
    userIds.forEach(uid => {
        const userName = $(`#userSelect option[value="${uid}"]`).text() || 'User ' + uid;
        addBadge(uid, userName);
    });

    $('#shareModal').modal('show');
}

function selectAccess(level, triggerUpdate = true) {
    currentAccessLevel = level;
    $('.access-option').removeClass('active border-primary');
    $('.access-option').eq(level).addClass('active border-primary');

    if (level === 2) {
        $('#restrictedSection').removeClass('d-none');
    } else {
        $('#restrictedSection').addClass('d-none');
    }

    if (triggerUpdate) {
        updateAccessLevel(level);
    }
}

async function updateAccessLevel(level) {
    const id = $('#shareDocId').val();
    try {
        await fetch('?handler=UpdateAccessLevel', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: `id=${id}&accessLevel=${level}`
        });
    } catch (e) {
        console.error(e);
    }
}

function addBadge(uid, name) {
    const badge = $(`
        <span class="badge bg-light text-primary border border-primary p-2 d-flex align-items-center" data-id="${uid}">
            ${name}
            <i class="bi bi-x-circle ms-2 cursor-pointer text-danger" onclick="removeShareUser(${uid})"></i>
        </span>
    `);
    $('#shareList').append(badge);
}

async function addShareUser() {
    const userId = $('#userSelect').val();
    if (!userId) return;

    const userName = $('#userSelect option:selected').text();
    if ($(`#shareList span[data-id="${userId}"]`).length > 0) return;

    addBadge(userId, userName);
    saveShares();
}

function removeShareUser(uid) {
    $(`#shareList span[data-id="${uid}"]`).remove();
    saveShares();
}

async function saveShares() {
    const id = $('#shareDocId').val();
    const userIds = [];
    $('#shareList span').each(function () {
        userIds.push(parseInt($(this).data('id')));
    });

    try {
        await fetch('?handler=UpdateShares&id=' + id, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify(userIds)
        });
    } catch (e) {
        console.error(e);
    }
}

function copyShareLink() {
    const copyText = document.getElementById("shareLinkInput");
    copyText.select();
    copyText.setSelectionRange(0, 99999);
    navigator.clipboard.writeText(copyText.value);

    const btn = event.target.closest('button');
    const originalText = btn.innerHTML;
    btn.innerHTML = '<i class="bi bi-check2 me-1"></i> Copied!';
    setTimeout(() => { btn.innerHTML = originalText; }, 2000);
}

$(document).ready(function () {
    if ($.fn.select2) {
        $('.select2').select2({
            dropdownParent: $('#shareModal')
        });
    }
});
