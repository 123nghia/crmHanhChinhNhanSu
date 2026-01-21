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

var employeeFilterTimer = null;

function normalizeFilterValue(value) {
    return String(value || "").toLowerCase().trim();
}

function getCellFilterValue(cell) {
    if (!cell) {
        return "";
    }

    var text = (cell.textContent || "").replace(/\s+/g, " ").trim();
    if (text) {
        return text;
    }

    if (cell.dataset && cell.dataset.value !== undefined && cell.dataset.value !== null) {
        return String(cell.dataset.value);
    }

    return "";
}

function getEmployeeFilterControls() {
    var table = document.getElementById("employeeGrid");
    if (!table) {
        return [];
    }

    return table.querySelectorAll("thead .table-filter-input, thead .table-filter-select");
}

function populateEmployeeFilterDropdowns() {
    var table = document.getElementById("employeeGrid");
    if (!table) {
        return;
    }

    var selects = table.querySelectorAll("thead .table-filter-select[data-col-index]");
    if (!selects.length) {
        return;
    }

    var rows = table.querySelectorAll("tbody tr");

    selects.forEach(function (select) {
        var index = parseInt(select.dataset.colIndex, 10);
        if (isNaN(index)) {
            return;
        }

        var selectedValue = select.dataset.selectedValue || select.value || "";

        var options = {};

        rows.forEach(function (row) {
            var cell = row.children[index];
            if (!cell) {
                return;
            }

            var label = getCellFilterValue(cell);
            if (!label || label === "--") {
                return;
            }

            var value = "";
            if (cell.dataset && cell.dataset.value !== undefined && cell.dataset.value !== null && cell.dataset.value !== "") {
                value = String(cell.dataset.value).trim();
            } else {
                value = label.trim();
            }

            var key = normalizeFilterValue(value);
            if (!key) {
                return;
            }

            if (!options[key]) {
                options[key] = { value: value, label: label.trim() };
            }
        });

        var items = Object.keys(options).map(function (key) {
            return options[key];
        });

        items.sort(function (a, b) {
            return a.label.localeCompare(b.label);
        });

        var optionsHtml = '<option value="-1">Tat ca</option>';
        items.forEach(function (item) {
            var safeValue = escapeHtml(item.value);
            var safeLabel = escapeHtml(item.label);
            optionsHtml += '<option value="' + safeValue + '">' + safeLabel + '</option>';
        });

        select.innerHTML = optionsHtml;

        if (selectedValue) {
            var normalizedSelected = normalizeFilterValue(selectedValue);
            var matchingOption = Array.from(select.options).find(function (option) {
                return normalizeFilterValue(option.value) === normalizedSelected;
            });
            if (matchingOption) {
                select.value = matchingOption.value;
            }
        }
    });
}

function applyEmployeeHeaderFilters() {
    var url = new URL(window.location.href);
    var form = document.querySelector('form[action="/Employee"]');
    if (form) {
        var formData = new FormData(form);
        formData.forEach(function (value, key) {
            var trimmed = String(value || "").trim();
            if (!trimmed || trimmed === "-1") {
                url.searchParams.delete(key);
            } else {
                url.searchParams.set(key, trimmed);
            }
        });
    }

    var controls = getEmployeeFilterControls();
    controls.forEach(function (control) {
        var key = control.dataset.filterKey;
        if (!key) {
            return;
        }

        var value = String(control.value || "").trim();
        if (control.tagName === "SELECT") {
            if (!value) {
                url.searchParams.delete(key);
            } else {
                url.searchParams.set(key, value);
            }
            return;
        }

        if (!value) {
            url.searchParams.delete(key);
        } else {
            url.searchParams.set(key, value);
        }
    });

    url.searchParams.set("page", "1");
    window.location.href = url.toString();
}

function scheduleEmployeeHeaderFilters() {
    if (employeeFilterTimer) {
        clearTimeout(employeeFilterTimer);
    }
    employeeFilterTimer = setTimeout(applyEmployeeHeaderFilters, 400);
}

function clearEmployeeHeaderFilters() {
    var controls = getEmployeeFilterControls();

    controls.forEach(function (control) {
        control.value = control.tagName === "SELECT" ? "-1" : "";
    });

    applyEmployeeHeaderFilters();
}

function bindEmployeeFilterRefreshHooks() {
    if (window.employeeFilterHooksBound) {
        return;
    }
    window.employeeFilterHooksBound = true;

    var previousCellUpdated = window.onEditableGridCellUpdated;
    var previousRowUpdated = window.onEditableGridRowUpdated;

    window.onEditableGridCellUpdated = function (cell) {
        if (typeof previousCellUpdated === "function") {
            previousCellUpdated(cell);
        }
        if (cell && cell.closest && cell.closest("#employeeGrid")) {
            populateEmployeeFilterDropdowns();
        }
    };

    window.onEditableGridRowUpdated = function (row) {
        if (typeof previousRowUpdated === "function") {
            previousRowUpdated(row);
        }
        if (row && row.closest && row.closest("#employeeGrid")) {
            populateEmployeeFilterDropdowns();
        }
    };
}

function bindEmployeeHeaderFilters() {
    var controls = getEmployeeFilterControls();
    if (!controls.length) {
        return;
    }

    controls.forEach(function (control) {
        if (control.tagName === "SELECT") {
            control.addEventListener("change", applyEmployeeHeaderFilters);
            return;
        }

        control.addEventListener("input", scheduleEmployeeHeaderFilters);
        control.addEventListener("keydown", function (event) {
            if (event.key === "Enter") {
                event.preventDefault();
                applyEmployeeHeaderFilters();
            }
        });
    });

    var clearButton = document.querySelector("#employeeGrid .js-clear-header-filters");
    if (clearButton) {
        clearButton.addEventListener("click", function (event) {
            event.preventDefault();
            clearEmployeeHeaderFilters();
        });
    }
}

document.addEventListener("DOMContentLoaded", function () {
    populateEmployeeFilterDropdowns();
    bindEmployeeHeaderFilters();
    bindEmployeeFilterRefreshHooks();
});

