function selectAttendanceEmployee(button) {
    var row = button.closest('tr');
    if (!row) return;

    var employeeId = parseInt(row.getAttribute('data-employee-id'), 10);
    var fingerprint = row.getAttribute('data-fingerprint') || '';
    syncAttendanceEmployeeSelect(employeeId, fingerprint);
    setAttendanceFingerprint(fingerprint);
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
    var fingerprintInput = document.getElementById('attendanceFingerprintInput');
    var tokenInput = document.querySelector('input[name="token"]');

    var exportMonth = document.getElementById('exportMonth');
    var exportEmployee = document.getElementById('exportEmployeeId');
    var exportFingerprint = document.getElementById('exportFingerprintCode');
    var exportToken = document.getElementById('exportToken');

    if (exportMonth) exportMonth.value = monthInput ? monthInput.value : '';
    if (exportEmployee) exportEmployee.value = employeeInput ? employeeInput.value : '';
    if (exportFingerprint) exportFingerprint.value = fingerprintInput ? fingerprintInput.value : '';
    if (exportToken) exportToken.value = tokenInput ? tokenInput.value : '';

    exportForm.submit();
}

function setAttendanceFingerprint(fingerprint) {
    var fingerprintInput = document.getElementById('attendanceFingerprintInput');
    if (!fingerprintInput) return;
    fingerprintInput.value = fingerprint || '';
}

function findAttendanceRow(employeeId, fingerprint) {
    var rows = Array.prototype.slice.call(document.querySelectorAll('.attendance-summary-row'));
    var normalizedEmployeeId = Number.isNaN(employeeId) ? 0 : employeeId;
    var normalizedFingerprint = (fingerprint || '').trim().toLowerCase();

    return rows.find(function (row) {
        var rowEmployeeId = parseInt(row.getAttribute('data-employee-id'), 10);
        rowEmployeeId = Number.isNaN(rowEmployeeId) ? 0 : rowEmployeeId;
        var rowFingerprint = (row.getAttribute('data-fingerprint') || '').trim().toLowerCase();

        if (normalizedEmployeeId > 0 && rowEmployeeId !== normalizedEmployeeId) {
            return false;
        }

        if (normalizedFingerprint) {
            return rowFingerprint === normalizedFingerprint;
        }

        return normalizedEmployeeId > 0 ? rowEmployeeId === normalizedEmployeeId : rowEmployeeId === 0;
    }) || null;
}

function syncAttendanceEmployeeSelect(employeeId, fingerprint) {
    var employeeSelect = document.getElementById('attendanceEmployeeSelect');
    if (!employeeSelect) return;

    var normalizedEmployeeId = Number.isNaN(employeeId) ? 0 : employeeId;
    var normalizedFingerprint = (fingerprint || '').trim().toLowerCase();
    var matchedOption = Array.prototype.slice.call(employeeSelect.options).find(function (option) {
        var optionEmployeeId = parseInt(option.value, 10);
        optionEmployeeId = Number.isNaN(optionEmployeeId) ? 0 : optionEmployeeId;
        var optionFingerprint = (option.getAttribute('data-fingerprint') || '').trim().toLowerCase();

        if (normalizedEmployeeId > 0 && optionEmployeeId !== normalizedEmployeeId) {
            return false;
        }

        if (normalizedFingerprint) {
            return optionFingerprint === normalizedFingerprint;
        }

        return normalizedEmployeeId > 0 ? optionEmployeeId === normalizedEmployeeId : option.value === '';
    });

    Array.prototype.forEach.call(employeeSelect.options, function (option) {
        option.selected = false;
    });

    if (matchedOption) {
        matchedOption.selected = true;
    } else if (employeeSelect.options.length > 0) {
        employeeSelect.options[0].selected = true;
    }
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

function getAttendanceDate(value) {
    if (!value) return null;

    var date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) return null;

    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function getAttendanceMonthInfo(month) {
    if (!month) return null;

    var parts = month.split('-');
    var year = parseInt(parts[0], 10);
    var monthIndex = parseInt(parts[1], 10) - 1;
    if (Number.isNaN(year) || Number.isNaN(monthIndex)) return null;

    return {
        year: year,
        monthIndex: monthIndex
    };
}

function getAttendanceDayName(date) {
    switch (date.getDay()) {
        case 1: return 'Thứ 2';
        case 2: return 'Thứ 3';
        case 3: return 'Thứ 4';
        case 4: return 'Thứ 5';
        case 5: return 'Thứ 6';
        case 6: return 'Thứ 7';
        default: return 'CN';
    }
}

function isScheduledOffDate(date) {
    if (!(date instanceof Date) || Number.isNaN(date.getTime())) {
        return false;
    }

    if (date.getDay() === 0) {
        return true;
    }

    if (date.getDay() !== 6) {
        return false;
    }

    return Math.floor((date.getDate() - 1) / 7) + 1 > 2;
}

function getScheduledOffLabel(date) {
    if (!(date instanceof Date) || Number.isNaN(date.getTime())) {
        return 'Nghỉ';
    }

    if (date.getDay() === 0) {
        return 'Nghỉ Chủ nhật';
    }

    if (date.getDay() === 6) {
        return 'Nghỉ thứ 7';
    }

    return 'Nghỉ';
}

function getScheduledOffCount(month) {
    var monthInfo = getAttendanceMonthInfo(month);
    if (!monthInfo) return 0;

    var total = 0;
    var lastDay = new Date(monthInfo.year, monthInfo.monthIndex + 1, 0);

    for (var day = 1; day <= lastDay.getDate(); day++) {
        if (isScheduledOffDate(new Date(monthInfo.year, monthInfo.monthIndex, day))) {
            total++;
        }
    }

    return total;
}

function updateAttendanceOffCounts(month) {
    var offCount = getScheduledOffCount(month);
    var rows = document.querySelectorAll('.attendance-summary-row');

    rows.forEach(function (row) {
        row.setAttribute('data-off-count', offCount);
        if (row.children.length > 6) {
            row.children[6].textContent = offCount;
        }
    });

    var activeRow = document.querySelector('.attendance-summary-row.active') || document.querySelector('.attendance-summary-row');
    if (activeRow) {
        setText('summaryOffCount', activeRow.getAttribute('data-off-count'));
    } else {
        setText('summaryOffCount', offCount);
    }
}

function normalizeAttendanceItem(item) {
    var date = getAttendanceDate(item.workDate || item.WorkDate);
    var symbol = item.symbol || item.Symbol || '';

    return {
        workDate: date,
        dayName: item.dayName || item.DayName || (date ? getAttendanceDayName(date) : ''),
        checkIn: item.checkIn || item.CheckIn || '',
        checkOut: item.checkOut || item.CheckOut || '',
        workDay: item.workDay || item.WorkDay || 0,
        workHours: item.workHours || item.WorkHours || 0,
        lateMinutes: item.lateMinutes || item.LateMinutes || 0,
        earlyMinutes: item.earlyMinutes || item.EarlyMinutes || 0,
        shiftName: item.shiftName || item.ShiftName || '',
        symbol: symbol,
        isScheduledOff: Boolean(item.isScheduledOff || item.IsScheduledOff)
    };
}

function buildAttendanceDisplayData(data, month) {
    var monthInfo = getAttendanceMonthInfo(month);
    if (!monthInfo) return [];

    var recordMap = {};

    if (Array.isArray(data)) {
        data.forEach(function (item) {
            var normalized = normalizeAttendanceItem(item);
            if (!normalized.workDate) {
                return;
            }

            var key = toDateKey(normalized.workDate);
            if (!key) {
                return;
            }

            normalized.isScheduledOff = normalized.isScheduledOff || isScheduledOffDate(normalized.workDate);
            if (normalized.isScheduledOff && !normalized.symbol) {
                normalized.symbol = getScheduledOffLabel(normalized.workDate);
            }

            recordMap[key] = normalized;
        });
    }

    var result = [];
    var lastDay = new Date(monthInfo.year, monthInfo.monthIndex + 1, 0);

    for (var day = 1; day <= lastDay.getDate(); day++) {
        var date = new Date(monthInfo.year, monthInfo.monthIndex, day);
        var key = toDateKey(date);
        var record = recordMap[key];

        if (record) {
            record.isScheduledOff = record.isScheduledOff || isScheduledOffDate(date);
            if (record.isScheduledOff && !record.symbol) {
                record.symbol = getScheduledOffLabel(date);
            }

            result.push(record);
            continue;
        }

        if (!isScheduledOffDate(date)) {
            continue;
        }

        result.push({
            workDate: date,
            dayName: getAttendanceDayName(date),
            checkIn: '',
            checkOut: '',
            workDay: 0,
            workHours: 0,
            lateMinutes: 0,
            earlyMinutes: 0,
            shiftName: '',
            symbol: getScheduledOffLabel(date),
            isScheduledOff: true
        });
    }

    return result.sort(function (a, b) {
        return a.workDate.getTime() - b.workDate.getTime();
    });
}

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
        var displayData = buildAttendanceDisplayData(Array.isArray(data) ? data : [], month);

        if (displayData.length === 0) {
            renderAttendanceTable([]);
            renderAttendanceCalendar([], month);
            hint.textContent = 'Không có dữ liệu chi tiết.';
            tableWrapper.classList.remove('d-none');
            return;
        }

        renderAttendanceTable(displayData);
        renderAttendanceCalendar(displayData, month);
        hint.textContent = Array.isArray(data) && data.length === 0
            ? 'Hiển thị ngày nghỉ theo lịch.'
            : '';

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
        var normalized = normalizeAttendanceItem(item);
        if (!normalized.workDate) {
            return;
        }

        var dateText = formatDate(normalized.workDate);
        var dayName = normalized.dayName;
        var checkIn = normalized.checkIn;
        var checkOut = normalized.checkOut;
        var workDay = normalized.workDay;
        var workHours = normalized.workHours;
        var lateMinutes = normalized.lateMinutes;
        var earlyMinutes = normalized.earlyMinutes;
        var shiftName = normalized.shiftName;
        var isScheduledOff = normalized.isScheduledOff || isScheduledOffDate(normalized.workDate);
        var symbol = normalized.symbol || (isScheduledOff ? getScheduledOffLabel(normalized.workDate) : '');

        var rowClasses = [];
        if (symbol) {
            rowClasses.push('attendance-row-absent');
        }
        if (isScheduledOff) {
            rowClasses.push('attendance-row-scheduled-off');
        }

        var lateClass = lateMinutes > 0 ? 'attendance-cell-late' : '';
        var earlyClass = earlyMinutes > 0 ? 'attendance-cell-early' : '';
        var symbolClass = symbol ? 'attendance-cell-absent' : '';
        if (isScheduledOff) {
            symbolClass += (symbolClass ? ' ' : '') + 'attendance-cell-off';
        }

        html += '<tr class="' + rowClasses.join(' ') + '">'
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

    var monthInfo = getAttendanceMonthInfo(month);
    if (!monthInfo) {
        wrapper.innerHTML = '';
        return;
    }

    var firstDay = new Date(monthInfo.year, monthInfo.monthIndex, 1);
    var lastDay = new Date(monthInfo.year, monthInfo.monthIndex + 1, 0);
    var startWeekDay = firstDay.getDay();

    var recordMap = {};
    if (Array.isArray(data)) {
        data.forEach(function (item) {
            var normalized = normalizeAttendanceItem(item);
            var key = toDateKey(normalized.workDate);
            if (key) {
                recordMap[key] = normalized;
            }
        });
    }

    var html = '';
    for (var i = 0; i < startWeekDay; i++) {
        html += '<div class="attendance-day-cell"></div>';
    }

    for (var day = 1; day <= lastDay.getDate(); day++) {
        var date = new Date(monthInfo.year, monthInfo.monthIndex, day);
        var key = toDateKey(date);
        var record = recordMap[key];
        var isScheduledOff = isScheduledOffDate(date);
        var cellClass = 'attendance-day-cell' + (isScheduledOff ? ' attendance-day-off' : '');
        var cellHtml = '<div class="' + cellClass + '">';
        cellHtml += '<div class="attendance-day-number">' + day + '</div>';

        if (record) {
            var checkIn = record.checkIn || '';
            var checkOut = record.checkOut || '';
            var workDay = record.workDay || 0;
            var lateMinutes = record.lateMinutes || 0;
            var earlyMinutes = record.earlyMinutes || 0;
            var symbol = record.symbol || '';

            if (isScheduledOff) {
                cellHtml += '<div class="attendance-badge text-bg-danger mt-1">Ngày nghỉ</div>';
            }

            if (symbol) {
                cellHtml += '<div class="attendance-day-meta">' + escapeHtml(symbol) + '</div>';
            }

            if (checkIn || checkOut) {
                cellHtml += '<div class="attendance-day-meta">Vào: ' + escapeHtml(checkIn) + '</div>';
                cellHtml += '<div class="attendance-day-meta">Ra: ' + escapeHtml(checkOut) + '</div>';
            } else if (!symbol) {
                cellHtml += '<div class="attendance-day-meta">Nghỉ</div>';
            }

            cellHtml += '<div class="attendance-day-meta">Công: ' + workDay + '</div>';

            if (lateMinutes > 0) {
                cellHtml += '<div class="attendance-badge bg-warning text-dark mt-1">Trễ ' + lateMinutes + 'p</div>';
            }

            if (earlyMinutes > 0) {
                cellHtml += '<div class="attendance-badge bg-info text-dark mt-1">Sớm ' + earlyMinutes + 'p</div>';
            }
        } else if (isScheduledOff) {
            cellHtml += '<div class="attendance-badge text-bg-danger mt-1">Ngày nghỉ</div>';
            cellHtml += '<div class="attendance-day-meta">' + escapeHtml(getScheduledOffLabel(date)) + '</div>';
        }

        cellHtml += '</div>';
        html += cellHtml;
    }

    wrapper.innerHTML = html;
}

function formatDate(value) {
    var date = getAttendanceDate(value);
    if (!date) return '';

    var day = String(date.getDate()).padStart(2, '0');
    var month = String(date.getMonth() + 1).padStart(2, '0');
    return day + '/' + month + '/' + date.getFullYear();
}

function toDateKey(value) {
    var date = getAttendanceDate(value);
    if (!date) return '';

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
        alert('Vui lòng chọn file .xlsx');
        return;
    }

    try {
        var response = await fetch('?handler=ImportAttendance', {
            method: 'POST',
            headers: {
                RequestVerificationToken: document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: formData
        });

        var result = await response.json();
        if (response.ok && result.success) {
            alert('Import thành công. Tổng: ' + (result.total || 0) + ', thành công: ' + (result.totalSuccess || 0));
            location.reload();
            return;
        }

        if (Array.isArray(result)) {
            var message = result.map(function (item) { return item.Content || item.content; }).join('\n');
            alert(message || 'Import thất bại');
        } else {
            alert(result.message || 'Import thất bại');
        }
    } catch (error) {
        console.error(error);
        alert('Lỗi hệ thống khi import');
    }
}

document.addEventListener('DOMContentLoaded', function () {
    var meta = document.getElementById('attendanceMeta');
    if (!meta) return;

    var selectedId = parseInt(meta.getAttribute('data-selected-id'), 10);
    var fingerprint = meta.getAttribute('data-selected-fingerprint') || '';
    var month = meta.getAttribute('data-month') || '';

    updateAttendanceOffCounts(month);

    if (selectedId > 0 || fingerprint) {
        var row = findAttendanceRow(selectedId, fingerprint);
        if (row) {
            syncAttendanceEmployeeSelect(selectedId, fingerprint);
            setAttendanceFingerprint(fingerprint);
            updateSummaryFromRow(row);
            highlightSummaryRow(row);
        }
        loadAttendanceDetails(selectedId, fingerprint);
    }

    var employeeSelect = document.getElementById('attendanceEmployeeSelect');
    if (employeeSelect) {
        employeeSelect.addEventListener('change', function (event) {
            var value = parseInt(event.target.value, 10);
            var selectedOption = event.target.options[event.target.selectedIndex];
            var optionFingerprint = selectedOption ? (selectedOption.getAttribute('data-fingerprint') || '') : '';
            setAttendanceFingerprint(optionFingerprint);

            var row = findAttendanceRow(value, optionFingerprint);
            if (row) {
                updateSummaryFromRow(row);
                highlightSummaryRow(row);
                loadAttendanceDetails(value, row.getAttribute('data-fingerprint') || optionFingerprint);
            } else {
                loadAttendanceDetails(value, optionFingerprint);
            }
        });
    }

    setInterval(function () {
        if (document.hidden) return;
        if (document.querySelector('.modal.show')) return;
        location.reload();
    }, 30000);
});
