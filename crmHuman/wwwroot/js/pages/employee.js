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
        var rowLabel = row !== undefined && row !== null && row !== "" ? "Dòng " + row + ": " : "";
        return "<li>" + escapeHtml(rowLabel + (content ?? "")) + "</li>";
    }).join("");

    return "<div style=\"text-align:left\"><ul>" + items + "</ul></div>";
}

function showImportModal(options) {
    if (window.Swal && typeof Swal.fire === "function") {
        return Swal.fire(options);
    }
    console.error(options && options.title ? options.title : "Lỗi import");
    return Promise.resolve();
}

function submitImportEmployee() {
    var form = document.getElementById("formImportEmployee");
    if (!form) {
        showImportModal({
            icon: "error",
            title: "Lỗi",
            text: "Không tìm thấy form import"
        });
        return;
    }

    var fileInput = form.querySelector("input[name='FileRequest']");
    if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
        showImportModal({
            icon: "warning",
            title: "Thông báo",
            text: "Vui lòng chọn file .xlsx"
        });
        return;
    }

    var btnSubmit = form.closest(".modal-content")?.querySelector(".btn-primary");
    var originalText = btnSubmit ? btnSubmit.innerHTML : "";
    if (btnSubmit) {
        btnSubmit.disabled = true;
        btnSubmit.innerHTML = "<span class=\"spinner-border spinner-border-sm\" role=\"status\" aria-hidden=\"true\"></span> Đang xử lý...";
    }

    var data = new FormData(form);
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
                successHtml += "<div>- Thành công: " + (res.totalSuccess ?? 0) + "</div>";
                successHtml += "<div>- Lỗi: " + (res.totalError ?? 0) + "</div>";
                successHtml += "</div>";

                showImportModal({
                    icon: "success",
                    title: "Import thành công",
                    html: successHtml
                }).then(function () {
                    $("#formModal").modal("hide");
                    location.reload();
                });
            } else {
                showImportModal({
                    icon: "error",
                    title: "Import thất bại",
                    text: "Không thể xử lý dữ liệu import"
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
                    title: "Import thất bại",
                    html: errorHtml
                });
                return;
            }

            var fallbackMsg = "Import thất bại";
            if (xhr && xhr.responseJSON && xhr.responseJSON.message) {
                fallbackMsg = xhr.responseJSON.message;
            } else if (xhr && xhr.responseText) {
                fallbackMsg = xhr.responseText;
            }

            showImportModal({
                icon: "error",
                title: "Import thất bại",
                text: fallbackMsg
            });
        }
    });
}

var employeeFilterTimer = null;
var employeeHeaderFrame = null;

function normalizeFilterValue(value) {
    return String(value || "").toLowerCase().trim();
}

function normalizeEmployeeLabel(value) {
    return String(value || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLowerCase()
        .trim();
}

function formatEmployeeBadgeLabel(value) {
    var text = String(value || "").trim();
    var normalized = normalizeEmployeeLabel(text);

    if (!text) {
        return "--";
    }

    if (normalized.indexOf("onboard") >= 0) {
        return "Đã onboard";
    }

    if (normalized === "active") {
        return "Đang hoạt động";
    }

    if (normalized === "inactive") {
        return "Ngưng hoạt động";
    }

    return text;
}

function getEmployeeBadgeClass(value) {
    var normalized = normalizeEmployeeLabel(value);

    if (!normalized) {
        return "status-neutral";
    }

    if (/(nghi viec|resign|offboard|inactive)/.test(normalized)) {
        return "status-resigned";
    }

    if (/(thu viec|probation|pending|tam dung|pause|hold)/.test(normalized)) {
        return "status-paused";
    }

    if (/(thieu|chua|tre|can bo sung|incomplete)/.test(normalized)) {
        return "status-warning";
    }

    if (/(dang lam|active|chinh thuc|hoan tat|day du|approved|onboard)/.test(normalized)) {
        return "status-working";
    }

    return "status-neutral";
}

function renderEmployeeCellValue(cell, rawValue, displayValue) {
    if (!cell) {
        return;
    }

    var field = cell.dataset ? cell.dataset.field : "";
    var text = formatEmployeeBadgeLabel(displayValue || rawValue);
    var safeText = escapeHtml(text);
    var row = cell.closest("tr");
    var rowId = row && row.dataset ? row.dataset.id : "";

    if (field === "FullName") {
        if (text !== "--" && rowId && rowId !== "-1") {
            cell.innerHTML = '<a class="employee-name-link" href="/EmployeeInfo?id=' + escapeHtml(rowId) + '">' + safeText + '</a>';
            return;
        }
        cell.textContent = text;
        return;
    }

    if (field === "Status" || field === "StatusWork" || field === "DocumentStatus") {
        cell.innerHTML = '<span class="status-badge ' + getEmployeeBadgeClass(text) + '">' + safeText + '</span>';
        return;
    }

    cell.textContent = text;
}

window.renderEditableGridCellValue = renderEmployeeCellValue;

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

function getAdvancedFilterControls() {
    var form = document.querySelector('form[action="/Employee"]');
    if (!form) {
        return [];
    }

    return form.querySelectorAll("input, select");
}

function setBadgeValue(element, count) {
    if (!element) {
        return;
    }

    if (count > 0) {
        element.textContent = String(count);
        element.classList.add("has-value");
    } else {
        element.textContent = "";
        element.classList.remove("has-value");
    }
}

function countActiveControls(controls) {
    var count = 0;

    Array.from(controls || []).forEach(function (control) {
        if (!control || !control.name || control.type === "hidden" || control.disabled) {
            return;
        }

        var value = String(control.value || "").trim();
        if (!value || value === "-1") {
            return;
        }

        count += 1;
    });

    return count;
}

function updateEmployeeFilterBadges() {
    var url = new URL(window.location.href);
    var advancedKeys = ["from", "to", "token", "documentStatus", "statusWork", "groupId", "memberId"];
    var advancedCount = advancedKeys.reduce(function (count, key) {
        var value = url.searchParams.get(key);
        if (!value || value === "-1") {
            return count;
        }
        return count + 1;
    }, 0);

    setBadgeValue(document.getElementById("employeeAdvancedFilterBadge"), advancedCount);
    setBadgeValue(document.getElementById("employeeColumnFilterBadge"), countActiveControls(getEmployeeFilterControls()));
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

        var optionsHtml = '<option value="-1">Tất cả</option>';
        items.forEach(function (item) {
            optionsHtml += '<option value="' + escapeHtml(item.value) + '">' + escapeHtml(item.label) + '</option>';
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

    updateEmployeeFilterBadges();
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
        if (!value || value === "-1") {
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
    Array.from(getEmployeeFilterControls()).forEach(function (control) {
        control.value = control.tagName === "SELECT" ? "-1" : "";
    });

    updateEmployeeFilterBadges();
    applyEmployeeHeaderFilters();
}

function setAdvancedFilterPanel(open) {
    var panel = document.getElementById("employeeFilterPanel");
    var toggle = document.getElementById("toggleEmployeeFilters");

    if (!panel || !toggle) {
        return;
    }

    panel.classList.toggle("is-collapsed", !open);
    toggle.setAttribute("aria-expanded", open ? "true" : "false");
}

function setColumnFilterPanel(open) {
    var shell = document.getElementById("employeeGridShell");
    var toggle = document.getElementById("toggleColumnFilters");

    if (!shell || !toggle) {
        return;
    }

    shell.classList.toggle("show-column-filters", !!open);
    toggle.setAttribute("aria-expanded", open ? "true" : "false");
    queueEmployeeFloatingHeaderSync();
}

function bindEmployeePanels() {
    var toggleAdvanced = document.getElementById("toggleEmployeeFilters");
    var closeAdvanced = document.getElementById("closeEmployeeFilters");
    var toggleColumns = document.getElementById("toggleColumnFilters");
    var panel = document.getElementById("employeeFilterPanel");
    var shell = document.getElementById("employeeGridShell");

    if (toggleAdvanced && panel) {
        toggleAdvanced.addEventListener("click", function () {
            setAdvancedFilterPanel(panel.classList.contains("is-collapsed"));
        });
    }

    if (closeAdvanced && panel) {
        closeAdvanced.addEventListener("click", function () {
            setAdvancedFilterPanel(false);
        });
    }

    if (toggleColumns && shell) {
        toggleColumns.addEventListener("click", function () {
            setColumnFilterPanel(!shell.classList.contains("show-column-filters"));
        });
    }
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
            updateEmployeeFilterBadges();
        }
    };

    window.onEditableGridRowUpdated = function (row) {
        if (typeof previousRowUpdated === "function") {
            previousRowUpdated(row);
        }

        if (row && row.closest && row.closest("#employeeGrid")) {
            populateEmployeeFilterDropdowns();
            updateEmployeeFilterBadges();
        }
    };
}

function bindEmployeeHeaderFilters() {
    var controls = getEmployeeFilterControls();
    if (!controls.length) {
        return;
    }

    Array.from(controls).forEach(function (control) {
        if (control.tagName === "SELECT") {
            control.addEventListener("change", applyEmployeeHeaderFilters);
            return;
        }

        control.addEventListener("input", function () {
            updateEmployeeFilterBadges();
            scheduleEmployeeHeaderFilters();
        });

        control.addEventListener("keydown", function (event) {
            if (event.key === "Enter") {
                event.preventDefault();
                applyEmployeeHeaderFilters();
            }
        });
    });

    var clearButton = document.querySelector(".js-clear-header-filters");
    if (clearButton) {
        clearButton.addEventListener("click", function (event) {
            event.preventDefault();
            clearEmployeeHeaderFilters();
        });
    }
}

function getEmployeeStickyOffset() {
    var stickyOffset = parseInt(
        window.getComputedStyle(document.documentElement).getPropertyValue("--sticky-table-offset"),
        10
    );

    if (!Number.isNaN(stickyOffset) && stickyOffset > 0) {
        return stickyOffset;
    }

    var header = document.getElementById("header");
    var pageTitle = document.querySelector(".pagetitle");
    var headerHeight = header ? Math.ceil(header.getBoundingClientRect().height) : 0;
    var pageTitleHeight = pageTitle ? Math.ceil(pageTitle.getBoundingClientRect().height) : 0;
    return headerHeight + pageTitleHeight;
}

function syncEmployeeFloatingHeader() {
    var table = document.getElementById("employeeGrid");
    var shell = document.getElementById("employeeGridShell");

    if (!table || !table.tHead || !table.tHead.rows.length || !shell) {
        return;
    }

    var headerCells = Array.from(table.tHead.rows[0].cells || []);
    if (!headerCells.length) {
        return;
    }

    if (window.innerWidth < 992) {
        headerCells.forEach(function (cell) {
            cell.style.transform = "";
        });
        shell.classList.remove("is-header-floating");
        return;
    }

    var tableRect = table.getBoundingClientRect();
    var headerHeight = Math.ceil(table.tHead.rows[0].getBoundingClientRect().height) || 0;
    var stickyOffset = getEmployeeStickyOffset();
    var maxTranslate = Math.max(0, table.offsetHeight - headerHeight);
    var translateY = Math.max(0, Math.min(stickyOffset - tableRect.top, maxTranslate));
    var shouldFloat = translateY > 0 && tableRect.bottom > stickyOffset + headerHeight;

    headerCells.forEach(function (cell) {
        cell.style.transform = shouldFloat ? "translateY(" + translateY + "px)" : "";
    });

    shell.classList.toggle("is-header-floating", shouldFloat);
}

function queueEmployeeFloatingHeaderSync() {
    if (employeeHeaderFrame !== null) {
        window.cancelAnimationFrame(employeeHeaderFrame);
    }

    employeeHeaderFrame = window.requestAnimationFrame(function () {
        employeeHeaderFrame = null;
        syncEmployeeFloatingHeader();
    });
}

document.addEventListener("DOMContentLoaded", function () {
    populateEmployeeFilterDropdowns();
    bindEmployeePanels();
    bindEmployeeHeaderFilters();
    bindEmployeeFilterRefreshHooks();
    updateEmployeeFilterBadges();
    queueEmployeeFloatingHeaderSync();
    setTimeout(queueEmployeeFloatingHeaderSync, 120);
    setTimeout(queueEmployeeFloatingHeaderSync, 360);
});

window.addEventListener("scroll", queueEmployeeFloatingHeaderSync, { passive: true });
window.addEventListener("resize", queueEmployeeFloatingHeaderSync);
window.addEventListener("load", queueEmployeeFloatingHeaderSync);
