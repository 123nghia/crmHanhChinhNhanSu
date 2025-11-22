function openImportEmployee() {
    $("#contentModal").html("");
    $("#formModal").modal("show");
    $.get("/Employee?handler=FormImportEmployee", function (rs) {
        $("#contentModal").html(rs);
    });
}

function submitImportEmployee() {
    const form = document.getElementById("formImportEmployee");
    if (!form) {
        alert("Không tìm thấy form import");
        return;
    }
    const fileInput = form.querySelector("input[name='FileRequest']");
    if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
        alert("Vui lòng chọn file .xlsx");
        return;
    }

    // Show loading
    const btnSubmit = form.closest('.modal-content').querySelector('.btn-primary');
    const originalText = btnSubmit.innerHTML;
    btnSubmit.disabled = true;
    btnSubmit.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...';

    const data = new FormData(form);
    $.ajax({
        url: "/Employee?handler=ImportEmployee",
        type: "POST",
        headers: {
            "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
        },
        data: data,
        cache: false,
        processData: false,
        contentType: false,
        success: function (res) {
            btnSubmit.disabled = false;
            btnSubmit.innerHTML = originalText;

            if (res && res.success) {
                // Success notification
                const msg = `Import hoàn tất!\n- Thành công: ${res.totalSuccess}\n- Lỗi: ${res.totalError}`;
                alert(msg); // You can replace this with SweetAlert or Toastr if available
                
                $("#formModal").modal("hide");
                location.reload();
            } else {
                alert("Import không thành công");
            }
        },
        error: function (xhr) {
            btnSubmit.disabled = false;
            btnSubmit.innerHTML = originalText;

            let msg = "Import thất bại";
            if (xhr && xhr.responseJSON && xhr.responseJSON.length) {
                msg += "\n" + xhr.responseJSON.map(x => `${x?.Row ?? ''}: ${x?.Content ?? ''}`).join("\n");
            }
            alert(msg);
        }
    });
}
