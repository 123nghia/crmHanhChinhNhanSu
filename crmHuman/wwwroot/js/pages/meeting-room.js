(function () {
    var data = window.meetingRoomData || {};
    var state = {
        rooms: data.rooms || [],
        bookings: normalizeBookings(data.bookings || []),
        weekStart: parseDate(data.weekStart) || new Date(),
        weekEnd: parseDate(data.weekEnd) || new Date(),
        currentUserId: data.currentUserId || 0,
        canManage: data.canManage === true
    };

    var weekGrid = document.getElementById('meetingWeekGrid');
    var roomFilter = document.getElementById('roomFilter');
    var rangeText = document.getElementById('meetingRangeText');
    var tokenInput = document.getElementsByName('__RequestVerificationToken')[0];

    var bookingIdInput = document.getElementById('bookingId');
    var bookingTitleInput = document.getElementById('bookingTitle');
    var bookingRoomInput = document.getElementById('bookingRoom');
    var bookingDateInput = document.getElementById('bookingDate');
    var bookingStartInput = document.getElementById('bookingStart');
    var bookingEndInput = document.getElementById('bookingEnd');
    var bookingNoteInput = document.getElementById('bookingNote');
    var outsideToggle = document.getElementById('outsideHoursToggle');
    var toast = document.getElementById('meetingToast');
    var meetingSaveButton = document.getElementById('meetingSaveButton');
    var isSavingMeetingBooking = false;

    function init() {
        bindToggle();
        loadRooms();
        resetMeetingForm();
        renderRange();
        renderWeek();
        loadBookings();
    }

    function parseDate(value) {
        if (!value) return null;
        var d = new Date(value);
        return isNaN(d.getTime()) ? null : d;
    }

    function normalizeBookings(list) {
        return (list || []).map(function (item) {
            return {
                id: item.Id || item.id,
                roomId: item.RoomId || item.roomId,
                roomName: item.RoomName || item.roomName || '',
                title: item.Title || item.title || '',
                note: item.Note || item.note || '',
                startTime: item.StartTime || item.startTime,
                endTime: item.EndTime || item.endTime,
                createdBy: item.CreatedBy || item.createdBy || 0,
                createdByName: item.CreatedByName || item.createdByName || ''
            };
        });
    }

    function formatDate(date) {
        return date.toISOString().slice(0, 10);
    }

    function formatDisplayDate(date) {
        return date.toLocaleDateString('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit' });
    }

    function formatRange(start, end) {
        var startText = start.toLocaleDateString('vi-VN', { day: '2-digit', month: 'short' });
        var endText = end.toLocaleDateString('vi-VN', { day: '2-digit', month: 'short', year: 'numeric' });
        return startText + ' - ' + endText;
    }

    function formatTime(date) {
        return date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false });
    }

    function addDays(date, days) {
        var next = new Date(date);
        next.setDate(next.getDate() + days);
        return next;
    }

    function loadRooms() {
        var options = '<option value="">Tất cả phòng</option>';
        var bookingOptions = '';
        state.rooms.forEach(function (room) {
            var label = room.Name || room.name || 'Phòng';
            options += '<option value="' + room.Id + '">' + escapeHtml(label) + '</option>';
            bookingOptions += '<option value="' + room.Id + '">' + escapeHtml(label) + '</option>';
        });
        roomFilter.innerHTML = options;
        bookingRoomInput.innerHTML = bookingOptions;
        if (state.rooms.length > 0) {
            bookingRoomInput.value = state.rooms[0].Id;
        }
        roomFilter.addEventListener('change', loadBookings);
    }

    function renderRange() {
        rangeText.textContent = formatRange(state.weekStart, state.weekEnd);
    }

    function renderWeek() {
        var html = '';
        for (var i = 0; i < 7; i++) {
            var day = addDays(state.weekStart, i);
            html += '<div class="day-column" style="--index:' + i + '" data-date="' + formatDate(day) + '">'
                + '<div class="day-header">'
                + '<span class="day-name">' + escapeHtml(formatDisplayDate(day)) + '</span>'
                + '<span class="day-date">' + escapeHtml(day.toLocaleDateString('vi-VN')) + '</span>'
                + '</div>'
                + '<div class="day-body" id="day-' + formatDate(day) + '"></div>'
                + '</div>';
        }
        weekGrid.innerHTML = html;
        renderBookings();
    }

    function renderBookings() {
        var map = buildBookingMap();
        for (var i = 0; i < 7; i++) {
            var day = addDays(state.weekStart, i);
            var dayKey = formatDate(day);
            var container = document.getElementById('day-' + dayKey);
            if (!container) continue;
            var dayBookings = map[dayKey] || [];
            if (dayBookings.length === 0) {
                container.innerHTML = '<div class="text-muted small">Chưa có lịch</div>';
                continue;
            }
            var content = '';
            dayBookings.forEach(function (booking) {
                var start = new Date(booking.startTime);
                var end = new Date(booking.endTime);
                var outside = isOutsideHours(start, end);
                var canEdit = state.canManage || booking.createdBy === state.currentUserId;
                content += '<div class="booking-card ' + (outside ? 'outside' : '') + '" data-id="' + booking.id + '">' 
                    + (canEdit ? '<div class="booking-actions-inline">'
                        + '<button type="button" onclick="editMeetingBooking(' + booking.id + ')"><i class="bi bi-pencil"></i></button>'
                        + '<button type="button" class="delete" onclick="deleteMeetingBooking(' + booking.id + ')"><i class="bi bi-trash"></i></button>'
                        + '</div>' : '')
                    + '<div class="booking-time">' + formatTime(start) + ' - ' + formatTime(end) + '</div>'
                    + '<div class="booking-title">' + escapeHtml(booking.title) + '</div>'
                    + '<div class="booking-room"><i class="bi bi-door-open"></i> ' + escapeHtml(booking.roomName || '') + '</div>'
                    + '<div class="booking-meta">' + escapeHtml(booking.createdByName || '') + '</div>'
                    + (outside ? '<div class="booking-meta outside-label">Ngoài giờ</div>' : '')
                    + (booking.note ? '<div class="booking-meta">' + escapeHtml(booking.note) + '</div>' : '')
                    + '</div>';
            });
            container.innerHTML = content;
        }
    }

    function buildBookingMap() {
        var map = {};
        state.bookings.forEach(function (booking) {
            var start = new Date(booking.startTime);
            var key = formatDate(start);
            if (!map[key]) {
                map[key] = [];
            }
            map[key].push(booking);
        });

        Object.keys(map).forEach(function (key) {
            map[key].sort(function (a, b) {
                return new Date(a.startTime) - new Date(b.startTime);
            });
        });

        return map;
    }

    function isOutsideHours(start, end) {
        var startHour = start.getHours() + start.getMinutes() / 60;
        var endHour = end.getHours() + end.getMinutes() / 60;
        return startHour < 8 || endHour > 18;
    }

    function bindToggle() {
        outsideToggle.addEventListener('change', function () {
            var isOutside = outsideToggle.checked;
            if (isOutside) {
                bookingStartInput.removeAttribute('min');
                bookingStartInput.removeAttribute('max');
                bookingEndInput.removeAttribute('min');
                bookingEndInput.removeAttribute('max');
            } else {
                bookingStartInput.setAttribute('min', '08:00');
                bookingStartInput.setAttribute('max', '18:00');
                bookingEndInput.setAttribute('min', '08:00');
                bookingEndInput.setAttribute('max', '18:00');
            }
        });
    }

    function loadBookings() {
        var from = formatDate(state.weekStart);
        var to = formatDate(state.weekEnd);
        var roomId = roomFilter.value || '';

        fetch('?handler=Data&from=' + encodeURIComponent(from) + '&to=' + encodeURIComponent(to) + '&roomId=' + roomId)
            .then(function (res) { return res.json(); })
            .then(function (data) {
                state.bookings = normalizeBookings(data);
                renderBookings();
            })
            .catch(function () {
                showToast('Không thể tải lịch phòng họp', false);
            });
    }

    function showToast(message, success) {
        if (!toast) return;
        toast.textContent = message;
        toast.className = 'meeting-toast ' + (success ? 'success' : 'error');
        setTimeout(function () {
            toast.className = 'meeting-toast';
        }, 4000);
    }

    function setMeetingSaveState(isSaving) {
        isSavingMeetingBooking = isSaving;
        if (!meetingSaveButton) return;
        meetingSaveButton.disabled = isSaving;
        meetingSaveButton.textContent = isSaving ? 'Đang lưu...' : 'Lưu lịch';
    }

    function escapeHtml(value) {
        return (value || '').toString()
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function toLocalIsoString(date) {
        var pad = function (value) { return value < 10 ? '0' + value : value; };
        return date.getFullYear() + '-' + pad(date.getMonth() + 1) + '-' + pad(date.getDate()) + 'T' + pad(date.getHours()) + ':' + pad(date.getMinutes()) + ':00';
    }

    window.changeWeek = function (delta) {
        state.weekStart = addDays(state.weekStart, delta * 7);
        state.weekEnd = addDays(state.weekStart, 6);
        renderRange();
        renderWeek();
        loadBookings();
    };

    window.goToToday = function () {
        var today = new Date();
        var diff = (7 + today.getDay() - 1) % 7;
        state.weekStart = addDays(today, -diff);
        state.weekEnd = addDays(state.weekStart, 6);
        renderRange();
        renderWeek();
        loadBookings();
    };

    window.saveMeetingBooking = function () {
        if (isSavingMeetingBooking) {
            return;
        }

        var title = bookingTitleInput.value.trim();
        var roomId = parseInt(bookingRoomInput.value, 10) || 0;
        var date = bookingDateInput.value;
        var startTime = bookingStartInput.value;
        var endTime = bookingEndInput.value;

        if (!title) {
            showToast('Vui lòng nhập tiêu đề', false);
            return;
        }
        if (!roomId) {
            showToast('Vui lòng chọn phòng', false);
            return;
        }
        if (!date || !startTime || !endTime) {
            showToast('Vui lòng chọn ngày và giờ', false);
            return;
        }

        var start = new Date(date + 'T' + startTime);
        var end = new Date(date + 'T' + endTime);
        if (end <= start) {
            showToast('Giờ kết thúc phải sau giờ bắt đầu', false);
            return;
        }

        var payload = {
            Id: parseInt(bookingIdInput.value, 10) || 0,
            RoomId: roomId,
            Title: title,
            Note: bookingNoteInput.value || '',
            StartTime: toLocalIsoString(start),
            EndTime: toLocalIsoString(end)
        };

        setMeetingSaveState(true);

        fetch('?handler=Save', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': tokenInput ? tokenInput.value : ''
            },
            body: JSON.stringify(payload)
        })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (data.success) {
                    showToast('Lưu lịch thành công', true);
                    resetMeetingForm();
                    loadBookings();
                } else {
                    showToast(data.message || 'Không thể lưu lịch', false);
                }
            })
            .catch(function () {
                showToast('Không thể lưu lịch', false);
            })
            .finally(function () {
                setMeetingSaveState(false);
            });
    };

    window.resetMeetingForm = function () {
        setMeetingSaveState(false);
        bookingIdInput.value = '0';
        bookingTitleInput.value = '';
        bookingNoteInput.value = '';
        bookingDateInput.value = formatDate(new Date());
        bookingStartInput.value = '08:00';
        bookingEndInput.value = '09:00';
    };

    window.editMeetingBooking = function (id) {
        var booking = state.bookings.find(function (b) { return b.id === id; });
        if (!booking) return;
        var start = new Date(booking.startTime);
        var end = new Date(booking.endTime);

        bookingIdInput.value = booking.id;
        bookingTitleInput.value = booking.title;
        bookingRoomInput.value = booking.roomId;
        bookingDateInput.value = formatDate(start);
        bookingStartInput.value = formatTime(start);
        bookingEndInput.value = formatTime(end);
        bookingNoteInput.value = booking.note || '';
        outsideToggle.checked = isOutsideHours(start, end);
        outsideToggle.dispatchEvent(new Event('change'));
        window.scrollTo({ top: 0, behavior: 'smooth' });
    };

    window.deleteMeetingBooking = function (id) {
        if (!confirm('Xóa lịch đặt phòng này?')) return;
        fetch('?handler=Delete&id=' + id, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': tokenInput ? tokenInput.value : ''
            }
        })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (data.success) {
                    showToast('Đã xóa lịch', true);
                    loadBookings();
                } else {
                    showToast(data.message || 'Không thể xóa lịch', false);
                }
            })
            .catch(function () {
                showToast('Không thể xóa lịch', false);
            });
    };

    init();
})();
