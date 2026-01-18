function openImportEmployee() {
    $("#contentModal").html("");
    $("#formModal").modal("show");
    $.get("/Employee?handler=FormImportEmployee", function (rs) {
        $("#contentModal").html(rs);
    });
}

function escapeHtml(value) {
    return String(value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/\"/g, "&quot;")
        .replace(/'/g, "&#39;");
}

function formatImportErrors(errors) {
    if (!Array.isArray(errors) || errors.length === 0) {
        return "";
    }

    var items = errors.map(function (item) {
        var row = item && (item.Row ?? item.row);
        var content = item && (item.Content ?? item.content);
        var rowLabel = row !== undefined && row !== null && row !== "" ? "Dong " + row + ": " : "";
        return "<li>" + escapeHtml(rowLabel + (content ?? "")) + "</li>";
    }).join("");

    return "<div style=\"text-align:left\"><ul>" + items + "</ul></div>";
}

function showImportModal(options) {
    if (window.Swal && typeof Swal.fire === "function") {
        return Swal.fire(options);
    }
    console.error(options && options.title ? options.title : "Import error");
    return Promise.resolve();
}

function submitImportEmployee() {
    const form = document.getElementById("formImportEmployee");
    if (!form) {
        showImportModal({
            icon: "error",
            title: "Loi",
            text: "Khong tim thay form import"
        });
        return;
    }
    const fileInput = form.querySelector("input[name='FileRequest']");
    if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
        showImportModal({
            icon: "warning",
            title: "Thong bao",
            text: "Vui long chon file .xlsx"
        });
        return;
    }

    const btnSubmit = form.closest(".modal-content")?.querySelector(".btn-primary");
    const originalText = btnSubmit ? btnSubmit.innerHTML : "";
    if (btnSubmit) {
        btnSubmit.disabled = true;
        btnSubmit.innerHTML = "<span class=\"spinner-border spinner-border-sm\" role=\"status\" aria-hidden=\"true\"></span> Dang xu ly...";
    }

    const data = new FormData(form);
    $.ajax({
        url: "/Employee?handler=ImportEmployee",
        type: "POST",
        headers: {
            "RequestVerificationToken": $("input[name=\"__RequestVerificationToken\"]").val()
        },
        data: data,
        cache: false,
        processData: false,
        contentType: false,
        success: function (res) {
            if (btnSubmit) {
                btnSubmit.disabled = false;
                btnSubmit.innerHTML = originalText;
            }

            if (res && res.success) {
                var successHtml = "<div style=\"text-align:left\">";
                successHtml += "<div>- Thanh cong: " + (res.totalSuccess ?? 0) + "</div>";
                successHtml += "<div>- Loi: " + (res.totalError ?? 0) + "</div>";
                successHtml += "</div>";

                showImportModal({
                    icon: "success",
                    title: "Import thanh cong",
                    html: successHtml
                }).then(function () {
                    $("#formModal").modal("hide");
                    location.reload();
                });
            } else {
                showImportModal({
                    icon: "error",
                    title: "Import that bai",
                    text: "Khong the xu ly du lieu import"
                });
            }
        },
        error: function (xhr) {
            if (btnSubmit) {
                btnSubmit.disabled = false;
                btnSubmit.innerHTML = originalText;
            }

            var errors = [];
            if (xhr && xhr.responseJSON) {
                if (Array.isArray(xhr.responseJSON)) {
                    errors = xhr.responseJSON;
                } else if (Array.isArray(xhr.responseJSON.errors)) {
                    errors = xhr.responseJSON.errors;
                }
            }

            var errorHtml = formatImportErrors(errors);
            if (errorHtml) {
                showImportModal({
                    icon: "error",
                    title: "Import that bai",
                    html: errorHtml
                });
                return;
            }

            var fallbackMsg = "Import that bai";
            if (xhr && xhr.responseJSON && xhr.responseJSON.message) {
                fallbackMsg = xhr.responseJSON.message;
            } else if (xhr && xhr.responseText) {
                fallbackMsg = xhr.responseText;
            }
            showImportModal({
                icon: "error",
                title: "Import that bai",
                text: fallbackMsg
            });
        }
    });
}
