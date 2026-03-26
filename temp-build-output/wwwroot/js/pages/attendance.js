function selectAttendanceEmployee(button) {
    var row = button.closest('tr');
    if (!row) return;
    var employeeId = parseInt(row.getAttribute('data-employee-id'), 10);
    var fingerprint = row.getAttribute('data-fingerprint') || '';
    updateSummaryFromRow(row);
    highlightSummaryRow(row);
    loadAttendanceDetails(employeeId, fingerprint);
}

function highlightSummaryRow(row) {
    document.querySelectorAll('.attendance-summary-row').forEach(function (item) {
        item.classList.remove('active');
    });
    if (row) {
        row.classList.add('active');
    }
}

function updateSummaryFromRow(row) {
    if (!row) return;
    setText('summaryWorkDays', row.getAttribute('data-work-days'));
    setText('summaryOffCount', row.getAttribute('data-off-count'));
    setText('summaryLateCount', row.getAttribute('data-late-count'));
    setText('summaryEarlyCount', row.getAttribute('data-early-count'));
    setText('summaryLateMinutes', row.getAttribute('data-late-minutes'));
    setText('summaryEarlyMinutes', row.getAttribute('data-early-minutes'));
    setText('summaryTotalHours', row.getAttribute('data-total-hours'));
    setText('summaryWorkHours', row.getAttribute('data-work-hours'));
}

function setText(id, value) {
    var el = document.getElementById(id);
    if (!el) return;
    el.textContent = value == null || value === '' ? '0' : value;
}

function submitAttendanceExport() {
    var exportForm = document.getElementById('attendanceExportForm');
    if (!exportForm) return;

    var monthInput = document.querySelector('input[name="month"]');
    var employeeInput = document.querySelector('[name="employeeId"]');
    var tokenInput = document.querySelector('input[name="token"]');

    var exportMonth = document.getElementById('exportMonth');
    var exportEmployee = document.getElementById('exportEmployeeId');
    var exportToken = document.getElementById('exportToken');

    if (exportMonth) exportMonth.value = monthInput ? monthInput.value : '';
    if (exportEmployee) exportEmployee.value = employeeInput ? employeeInput.value : '';
    if (exportToken) exportToken.value = tokenInput ? tokenInput.value : '';

    exportForm.submit();
}

function initMonthPicker() {
    var hiddenInput = document.getElementById('attendanceMonthInput');
    var displayInput = document.getElementById('attendanceMonthDisplay');
    if (!hiddenInput || !displayInput) return;

    var toDisplay = function (value) {
        if (!value) return '';
        var parts = value.split('-');
        if (parts.length < 2) return '';
        return parts[1].padStart(2, '0') + '/' + parts[0];
    };

    var toHidden = function (value) {
        if (!value) return '';
        var match = value.trim().match(/^(\d{1,2})[\/\-](\d{4})$/);
        if (!match) return '';
        var mm = match[1].padStart(2, '0');
        var yyyy = match[2];
        var month = parseInt(mm, 10);
        if (Number.isNaN(month) || month < 1 || month > 12) return '';
        return yyyy + '-' + mm;
    };

    displayInput.value = toDisplay(hiddenInput.value);

    displayInput.addEventListener('change', function () {
        var converted = toHidden(displayInput.value);
        if (converted) {
            hiddenInput.value = converted;
            displayInput.value = toDisplay(converted);
        }
    });

    displayInput.addEventListener('blur', function () {
        var converted = toHidden(displayInput.value);
        if (converted) {
            hiddenInput.value = converted;
            displayInput.value = toDisplay(converted);
        } else {
            displayInput.value = toDisplay(hiddenInput.value);
        }
    });

    var form = displayInput.closest('form');
    if (form) {
        form.addEventListener('submit', function () {
            var converted = toHidden(displayInput.value);
            if (converted) {
                hiddenInput.value = converted;
                displayInput.value = toDisplay(converted);
            }
        });
    }
}

document.addEventListener('DOMContentLoaded', initMonthPicker);

function toggleAttendanceView(view) {
    var tableWrapper = document.getElementById('attendanceDetailTableWrapper');
    var calendarWrapper = document.getElementById('attendanceCalendarWrapper');
    var tableBtn = document.getElementById('attendanceTableViewBtn');
    var calendarBtn = document.getElementById('attendanceCalendarViewBtn');

    if (view === 'calendar') {
        tableWrapper.classList.add('d-none');
        calendarWrapper.classList.remove('d-none');
        calendarBtn.classList.add('active');
        tableBtn.classList.remove('active');
    } else {
        calendarWrapper.classList.add('d-none');
        tableWrapper.classList.remove('d-none');
        tableBtn.classList.add('active');
        calendarBtn.classList.remove('active');
    }
}

async function loadAttendanceDetails(employeeId, fingerprint) {
    var meta = document.getElementById('attendanceMeta');
    if (!meta) return;

    var month = meta.getAttribute('data-month') || '';
    var hint = document.getElementById('attendanceDetailHint');
    var tableWrapper = document.getElementById('attendanceDetailTableWrapper');
    var calendarWrapper = document.getElementById('attendanceCalendarWrapper');

    if ((!employeeId || employeeId <= 0) && !fingerprint) {
        hint.textContent = 'Chọn nhân viên để xem chi tiết.';
        tableWrapper.classList.add('d-none');
        calendarWrapper.classList.add('d-none');
        return;
    }

    hint.textContent = 'Đang tải dữ liệu...';

    var url = '?handler=AttendanceDetails&month=' + encodeURIComponent(month);
    if (employeeId && employeeId > 0) {
        url += '&employeeId=' + employeeId;
    }
    if (fingerprint) {
        url += '&fingerprintCode=' + encodeURIComponent(fingerprint);
    }

    try {
        var response = await fetch(url);
        var data = await response.json();

        if (!Array.isArray(data) || data.length === 0) {
            renderAttendanceTable([]);
            renderAttendanceCalendar([], month);
            hint.textContent = 'Không có dữ liệu chi tiết.';
            tableWrapper.classList.remove('d-none');
            return;
        }

        renderAttendanceTable(data);
        renderAttendanceCalendar(data, month);
        hint.textContent = '';

        var calendarBtn = document.getElementById('attendanceCalendarViewBtn');
        if (calendarBtn && calendarBtn.classList.contains('active')) {
            calendarWrapper.classList.remove('d-none');
            tableWrapper.classList.add('d-none');
        } else {
            tableWrapper.classList.remove('d-none');
            calendarWrapper.classList.add('d-none');
        }
    } catch (error) {
        console.error(error);
        hint.textContent = 'Lỗi hệ thống khi tải dữ liệu.';
    }
}

function renderAttendanceTable(data) {
    var body = document.getElementById('attendanceDetailBody');
    if (!body) return;

    if (!Array.isArray(data) || data.length === 0) {
        body.innerHTML = '<tr><td colspan="10" class="text-center">Không có dữ liệu</td></tr>';
        return;
    }

    var html = '';
    data.forEach(function (item) {
        var dateText = formatDate(item.workDate || item.WorkDate);
        var dayName = item.dayName || item.DayName || '';
        var checkIn = item.checkIn || item.CheckIn || '';
        var checkOut = item.checkOut || item.CheckOut || '';
        var workDay = item.workDay || item.WorkDay || 0;
        var workHours = item.workHours || item.WorkHours || 0;
        var lateMinutes = item.lateMinutes || item.LateMinutes || 0;
        var earlyMinutes = item.earlyMinutes || item.EarlyMinutes || 0;
        var shiftName = item.shiftName || item.ShiftName || '';
        var symbol = item.symbol || item.Symbol || '';

        var rowClass = symbol ? 'attendance-row-absent' : '';
        var lateClass = lateMinutes > 0 ? 'attendance-cell-late' : '';
        var earlyClass = earlyMinutes > 0 ? 'attendance-cell-early' : '';
        var symbolClass = symbol ? 'attendance-cell-absent' : '';

        html += '<tr class="' + rowClass + '">'
            + '<td>' + dateText + '</td>'
            + '<td>' + escapeHtml(dayName) + '</td>'
            + '<td>' + escapeHtml(checkIn) + '</td>'
            + '<td>' + escapeHtml(checkOut) + '</td>'
            + '<td class="text-end">' + workDay + '</td>'
            + '<td class="text-end">' + workHours + '</td>'
            + '<td class="text-end ' + lateClass + '">' + lateMinutes + '</td>'
            + '<td class="text-end ' + earlyClass + '">' + earlyMinutes + '</td>'
            + '<td>' + escapeHtml(shiftName) + '</td>'
            + '<td class="' + symbolClass + '">' + escapeHtml(symbol) + '</td>'
            + '</tr>';
    });

    body.innerHTML = html;
}

function renderAttendanceCalendar(data, month) {
    var wrapper = document.getElementById('attendanceCalendar');
    if (!wrapper) return;

    if (!month) {
        wrapper.innerHTML = '';
        return;
    }

    var parts = month.split('-');
    var year = parseInt(parts[0], 10);
    var monthIndex = parseInt(parts[1], 10) - 1;
    if (Number.isNaN(year) || Number.isNaN(monthIndex)) {
        wrapper.innerHTML = '';
        return;
    }

    var firstDay = new Date(year, monthIndex, 1);
    var lastDay = new Date(year, monthIndex + 1, 0);
    var startWeekDay = firstDay.getDay();

    var recordMap = {};
    if (Array.isArray(data)) {
        data.forEach(function (item) {
            var key = toDateKey(item.workDate || item.WorkDate);
            if (key) {
                recordMap[key] = item;
            }
        });
    }

    var html = '';
    for (var i = 0; i < startWeekDay; i++) {
        html += '<div class="attendance-day-cell"></div>';
    }

    for (var day = 1; day <= lastDay.getDate(); day++) {
        var date = new Date(year, monthIndex, day);
        var key = toDateKey(date);
        var record = recordMap[key];
        var cellHtml = '<div class="attendance-day-cell">';
        cellHtml += '<div class="attendance-day-number">' + day + '</div>';

        if (record) {
            var checkIn = record.checkIn || record.CheckIn || '';
            var checkOut = record.checkOut || record.CheckOut || '';
            var workDay = record.workDay || record.WorkDay || 0;
            var lateMinutes = record.lateMinutes || record.LateMinutes || 0;
            var earlyMinutes = record.earlyMinutes || record.EarlyMinutes || 0;
            var symbol = record.symbol || record.Symbol || '';

            if (symbol) {
                cellHtml += '<div class="attendance-day-meta">Nghỉ: ' + escapeHtml(symbol) + '</div>';
            } else if (checkIn || checkOut) {
                cellHtml += '<div class="attendance-day-meta">Vào: ' + escapeHtml(checkIn) + '</div>';
                cellHtml += '<div class="attendance-day-meta">Ra: ' + escapeHtml(checkOut) + '</div>';
            } else {
                cellHtml += '<div class="attendance-day-meta">Nghỉ</div>';
            }

            cellHtml += '<div class="attendance-day-meta">Công: ' + workDay + '</div>';

            if (lateMinutes > 0) {
                cellHtml += '<div class="attendance-badge bg-warning text-dark mt-1">Trễ ' + lateMinutes + 'p</div>';
            }

            if (earlyMinutes > 0) {
                cellHtml += '<div class="attendance-badge bg-info text-dark mt-1">Sớm ' + earlyMinutes + 'p</div>';
            }
        }

        cellHtml += '</div>';
        html += cellHtml;
    }

    wrapper.innerHTML = html;
}

function formatDate(value) {
    if (!value) return '';
    var date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    var day = String(date.getDate()).padStart(2, '0');
    var month = String(date.getMonth() + 1).padStart(2, '0');
    return day + '/' + month + '/' + date.getFullYear();
}

function toDateKey(value) {
    var date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    var day = String(date.getDate()).padStart(2, '0');
    var month = String(date.getMonth() + 1).padStart(2, '0');
    return date.getFullYear() + '-' + month + '-' + day;
}

function escapeHtml(value) {
    if (value == null) return '';
    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

async function submitAttendanceImport() {
    var form = document.getElementById('attendanceImportForm');
    if (!form) return;

    var formData = new FormData(form);
    if (!formData.get('FileRequest')) {
        alert('Vui long chon file .xlsx');
        return;
    }

    try {
        var response = await fetch('?handler=ImportAttendance', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: formData
        });

        var result = await response.json();
        if (response.ok && result.success) {
            alert('Import thanh cong. Tong: ' + (result.total || 0) + ', thanh cong: ' + (result.totalSuccess || 0));
            location.reload();
            return;
        }

        if (Array.isArray(result)) {
            var message = result.map(function (item) { return item.Content || item.content; }).join('\n');
            alert(message || 'Import that bai');
        } else {
            alert(result.message || 'Import that bai');
        }
    } catch (error) {
        console.error(error);
        alert('Loi he thong khi import');
    }
}

document.addEventListener('DOMContentLoaded', function () {
    var meta = document.getElementById('attendanceMeta');
    if (!meta) return;

    var selectedId = parseInt(meta.getAttribute('data-selected-id'), 10);
    var fingerprint = meta.getAttribute('data-selected-fingerprint') || '';

    if (selectedId > 0 || fingerprint) {
        var row = document.querySelector('.attendance-summary-row[data-employee-id="' + selectedId + '"]');
        if (row) {
            updateSummaryFromRow(row);
            highlightSummaryRow(row);
        }
        loadAttendanceDetails(selectedId, fingerprint);
    }

    var employeeSelect = document.getElementById('attendanceEmployeeSelect');
    if (employeeSelect) {
        employeeSelect.addEventListener('change', function (event) {
            var value = parseInt(event.target.value, 10);
            var row = document.querySelector('.attendance-summary-row[data-employee-id="' + value + '"]');
            if (row) {
                updateSummaryFromRow(row);
                highlightSummaryRow(row);
                loadAttendanceDetails(value, row.getAttribute('data-fingerprint') || '');
            } else {
                loadAttendanceDetails(value, '');
            }
        });
    }

    setInterval(function () {
        if (document.hidden) return;
        if (document.querySelector('.modal.show')) return;
        location.reload();
    }, 30000);
});
