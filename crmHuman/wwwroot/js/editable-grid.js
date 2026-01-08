/**
 * Editable Grid - JavaScript thuần cho chỉnh sửa grid như Excel
 * Hỗ trợ: inline edit, copy/paste row, add/delete row, context menu
 * Cập nhật: Explicit Save/Cancel, Toggle Edit Mode
 */
var EditableGrid = (function () {
    'use strict';

    // Configuration
    var config = {
        tableSelector: '.editable-grid',
        editableCellClass: 'editable-cell',
        editingClass: 'editing',
        selectedRowClass: 'selected-row',
        contextMenuId: 'grid-context-menu',
        copiedRowData: null,
        masterData: {},
        fieldTypes: {
            'FullName': 'text',
            'RoleCode': 'dropdown',
            'PositionCode': 'dropdown',
            'DepartmentCode': 'dropdown',
            'GroupId': 'dropdown',
            'Status': 'dropdown',
            'StatusWork': 'dropdown',
            'DocumentStatus': 'dropdown',
            'Onboard': 'date'
        },
        masterDataTypes: {
            'RoleCode': 'role',
            'PositionCode': 2,
            'DepartmentCode': 1,
            'GroupId': 'group',
            'Status': 3,
            'StatusWork': 8,
            'DocumentStatus': 9
        }
    };

    // State
    var state = {
        currentEditingCell: null,
        selectedRow: null,
        isEditing: false,
        isEditModeEnabled: false, // Trạng thái bật/tắt chế độ sửa
        dirtyRows: {} // Lưu trạng thái các dòng đã chỉnh sửa: { rowId: { originalValues: {} } }
    };

    /**
     * Initialize the editable grid
     */
    function init(options) {
        if (options) {
            Object.assign(config, options);
        }

        createContextMenu();
        bindEvents();
        loadMasterData();

        // Mặc định tắt nút Thêm mới
        var btnAdd = document.getElementById('btnAddRow');
        if (btnAdd) btnAdd.style.display = 'none';

        console.log('EditableGrid initialized with Explicit Save/Cancel');
    }

    /**
     * Toggle Edit Mode
     */
    function toggleEditMode() {
        state.isEditModeEnabled = !state.isEditModeEnabled;

        var btn = document.getElementById('toggleEditMode');
        var btnAdd = document.getElementById('btnAddRow');

        if (state.isEditModeEnabled) {
            if (btn) {
                btn.classList.remove('btn-outline-secondary');
                btn.classList.add('btn-warning');
                btn.innerHTML = '<i class="bi bi-pencil-square"></i> Tắt chỉnh sửa';
            }
            if (btnAdd) btnAdd.style.display = 'inline-block';
            showToast('Đã BẬT chế độ chỉnh sửa. Click vào ô để sửa.', 'info');
            document.querySelector(config.tableSelector).classList.add('edit-mode-active');
        } else {
            if (btn) {
                btn.classList.remove('btn-warning');
                btn.classList.add('btn-outline-secondary');
                btn.innerHTML = '<i class="bi bi-pencil-square"></i> Bật chỉnh sửa';
            }
            if (btnAdd) btnAdd.style.display = 'none';

            // Hủy các thay đổi chưa lưu nếu tắt chế độ edit? 
            // Hoặc chỉ đơn giản là không cho edit tiếp. Ở đây ta chọn không cho edit tiếp.
            finishEditing();
            document.querySelector(config.tableSelector).classList.remove('edit-mode-active');
            showToast('Đã TẮT chế độ chỉnh sửa.', 'info');
        }
    }

    /**
     * Load master data for dropdowns
     */
    async function loadMasterData() {
        try {
            // Load các loại masterdata cần thiết
            var types = [1, 2, 3, 8, 9]; // Department, Position, Status, StatusWork, DocumentStatus

            for (var i = 0; i < types.length; i++) {
                var type = types[i];
                var data = await fetchMasterDataByType(type);
                config.masterData[type] = data;
            }

            // Load roles (hardcoded theo hệ thống hiện tại)
            config.masterData['role'] = [
                { Code: '1', Name: 'Admin' },
                { Code: '2', Name: 'TC' },
                { Code: '3', Name: 'TL' },
                { Code: '4', Name: 'Marketting' },
                { Code: '6', Name: 'Trưởng CTV' },
                { Code: '7', Name: 'CTV' }
            ];

            // Load groups
            var groupData = await fetchGroups();
            config.masterData['group'] = groupData;

            console.log('Master data loaded:', config.masterData);
        } catch (error) {
            console.error('Error loading master data:', error);
        }
    }

    /**
     * Fetch master data by type from API
     */
    async function fetchMasterDataByType(type) {
        try {
            var response = await fetch('/MasterDataPage?handler=AllData&Type=' + type);
            if (response.ok) {
                var result = await response.json();
                return result.Data || result.data || result || [];
            }
        } catch (error) {
            console.error('Error fetching master data type ' + type + ':', error);
        }
        return [];
    }

    /**
     * Fetch groups from API
     */
    async function fetchGroups() {
        try {
            var response = await fetch('/Group?handler=AllData');
            if (response.ok) {
                var result = await response.json();
                return result.Data || result.data || result || [];
            }
        } catch (error) {
            console.error('Error fetching groups:', error);
        }
        return [];
    }

    /**
     * Create context menu HTML
     */
    function createContextMenu() {
        if (document.getElementById(config.contextMenuId)) return;

        var menu = document.createElement('div');
        menu.id = config.contextMenuId;
        menu.className = 'grid-context-menu';
        menu.innerHTML = `
            <ul>
                <li data-action="add-above"><i class="bi bi-plus-circle"></i> Thêm dòng phía trên</li>
                <li data-action="add-below"><i class="bi bi-plus-circle"></i> Thêm dòng phía dưới</li>
                <li class="separator"></li>
                <li data-action="copy"><i class="bi bi-clipboard"></i> Copy dòng (Ctrl+C)</li>
                <li data-action="paste"><i class="bi bi-clipboard-check"></i> Paste dòng (Ctrl+V)</li>
                <li class="separator"></li>
                <li data-action="delete" class="danger"><i class="bi bi-trash"></i> Xóa dòng</li>
            </ul>
        `;
        document.body.appendChild(menu);

        // Bind menu item clicks
        menu.querySelectorAll('li[data-action]').forEach(function (item) {
            item.addEventListener('click', function () {
                handleContextMenuAction(this.dataset.action);
                hideContextMenu();
            });
        });
    }

    /**
     * Bind all events
     */
    function bindEvents() {
        // Cell click for editing
        document.addEventListener('click', function (e) {
            // Chỉ cho phép edit khi đã bật Edit Mode
            if (!state.isEditModeEnabled) return;

            // Nếu click vào thẻ A (link), cho phép chuyển hướng bình thường
            if (e.target.tagName === 'A' || e.target.closest('a')) return;

            var cell = e.target.closest('.' + config.editableCellClass);
            if (cell) {
                // Ngăn chặn các hành vi mặc định khác của cell (nếu có) nhưng không chặn link
                e.preventDefault();

                // Nếu đang click vào cell khác cell đang edit -> finish edit cell cũ
                if (state.currentEditingCell && state.currentEditingCell !== cell) {
                    finishEditing();
                }

                // Tránh event click kích hoạt lại khi click vào input
                if (e.target.tagName === 'INPUT' || e.target.tagName === 'SELECT') return;

                startEditing(cell);
            } else {
                // Click ra ngoài -> finish edit
                // Kiểm tra xem click có nằm trong nút Save/Cancel không
                if (!e.target.closest('.row-actions')) {
                    finishEditing();
                }
                hideContextMenu();
            }
        });

        // Row selection
        document.addEventListener('click', function (e) {
            var row = e.target.closest('.editable-grid tbody tr');
            if (row) {
                selectRow(row);
            }
        });

        // Context menu
        document.addEventListener('contextmenu', function (e) {
            if (!state.isEditModeEnabled) return; // Chỉ hiện menu khi Edit Mode bật

            var row = e.target.closest('.editable-grid tbody tr');
            if (row) {
                e.preventDefault();
                selectRow(row);
                showContextMenu(e.clientX, e.clientY);
            }
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', function (e) {
            if (state.isEditing) {
                handleEditingKeydown(e);
            } else {
                handleGridKeydown(e);
            }
        });

        // Hide context menu on scroll or resize
        window.addEventListener('scroll', hideContextMenu);
        window.addEventListener('resize', hideContextMenu);
    }

    /**
     * Start editing a cell
     */
    function startEditing(cell) {
        if (state.currentEditingCell === cell) return;

        finishEditing();

        var row = cell.closest('tr');
        var rowId = row.dataset.id;

        // Lưu giá trị gốc nếu chưa có
        if (!cell.dataset.originalValue) {
            cell.dataset.originalValue = cell.dataset.value || '';
        }

        var field = cell.dataset.field;
        var fieldType = config.fieldTypes[field] || 'text';
        var currentValue = cell.dataset.value || cell.textContent.trim();

        state.currentEditingCell = cell;
        state.isEditing = true;
        cell.classList.add(config.editingClass);

        var input;
        if (fieldType === 'dropdown') {
            input = createDropdown(field, currentValue);
        } else if (fieldType === 'date') {
            input = createDateInput(currentValue);
        } else {
            input = createTextInput(currentValue);
        }

        cell.innerHTML = '';
        cell.appendChild(input);
        input.focus();

        if (input.select && fieldType !== 'dropdown') input.select();
    }

    /**
     * Create text input for editing
     */
    function createTextInput(value) {
        var input = document.createElement('input');
        input.type = 'text';
        input.className = 'grid-cell-input';
        input.value = value;
        return input;
    }

    /**
     * Create date input for editing
     */
    function createDateInput(value) {
        var input = document.createElement('input');
        input.type = 'date';
        input.className = 'grid-cell-input';
        // Convert dd/MM/yyyy to yyyy-MM-dd for input
        if (value && value.includes('/')) {
            var parts = value.split('/');
            if (parts.length === 3) {
                input.value = parts[2] + '-' + parts[1] + '-' + parts[0];
            }
        } else {
            input.value = value;
        }
        return input;
    }

    /**
     * Create dropdown for editing
     */
    function createDropdown(field, currentValue) {
        var select = document.createElement('select');
        select.className = 'grid-cell-select';

        var dataType = config.masterDataTypes[field];
        var options = config.masterData[dataType] || [];

        select.innerHTML = '<option value="">-- Chọn --</option>';
        options.forEach(function (opt) {
            var value = opt.Code || opt.Id || opt.code || opt.id;
            var name = opt.Name || opt.FullName || opt.name || opt.fullName;
            var selected = (value == currentValue) ? 'selected' : '';
            select.innerHTML += '<option value="' + value + '" ' + selected + '>' + name + '</option>';
        });

        return select;
    }

    /**
     * Finish editing - update UI only, NO AUTO SAVE
     */
    function finishEditing() {
        if (!state.currentEditingCell) return;

        var cell = state.currentEditingCell;
        var input = cell.querySelector('input, select');

        if (input) {
            var newValue = input.value;
            var displayValue = newValue;

            // Get display text for dropdowns
            if (input.tagName === 'SELECT' && input.selectedIndex >= 0) {
                displayValue = input.options[input.selectedIndex].text || newValue;
            }

            // Format date for display
            if (input.type === 'date' && newValue) {
                var dateParts = newValue.split('-');
                if (dateParts.length === 3) {
                    displayValue = dateParts[2] + '/' + dateParts[1] + '/' + dateParts[0];
                }
            }

            var oldValue = cell.dataset.value;
            cell.dataset.value = newValue;
            cell.textContent = displayValue || '--';

            // Check if value changed
            if (newValue !== oldValue) {
                markRowAsDirty(cell.closest('tr'));
            }
        }

        cell.classList.remove(config.editingClass);
        state.currentEditingCell = null;
        state.isEditing = false;
    }

    /**
     * Mark row as dirty and show save/cancel buttons
     */
    function markRowAsDirty(row) {
        row.classList.add('row-dirty');

        // Find action cell (last cell)
        var actionCell = row.querySelector('td:last-child');

        // Check if actions buttons already exist
        if (!actionCell.querySelector('.row-actions-edit')) {
            // Backup original content
            if (!actionCell.dataset.originalContent) {
                actionCell.dataset.originalContent = actionCell.innerHTML;
            }

            // Show Save/Cancel buttons
            actionCell.innerHTML = `
                <div class="row-actions-edit">
                    <button class="btn btn-sm btn-success me-1" onclick="EditableGrid.saveRow(this)" title="Lưu"><i class="bi bi-check-lg"></i></button>
                    <button class="btn btn-sm btn-danger" onclick="EditableGrid.cancelRow(this)" title="Hủy"><i class="bi bi-x-lg"></i></button>
                </div>
            `;
        }
    }

    /**
     * Cancel row changes
     */
    function cancelRow(button) {
        var row = button.closest('tr');

        // Revert all cells
        row.querySelectorAll('.' + config.editableCellClass).forEach(function (cell) {
            if (cell.dataset.originalValue !== undefined) {
                cell.dataset.value = cell.dataset.originalValue;
                // Re-render text (cần logic mapping lại text từ value cho đúng nếu là dropdown, nhưng tạm thời text cũ chắc vẫn còn nếu ta không reload lại trang? Không, ta đã thay đổi textContent rồi)
                // Dễ nhất: nếu cancel thì buộc reload lại cell? 
                // Ta có thể lưu originalText content nữa.
                // Thôi đơn giản: reload lại trang hoặc chỉ revert value. 
                // Cải tiến: load lại text hiển thị.

                // Reset về text hiển thị
                // Với Dropdown, cần tìm text theo value
                var field = cell.dataset.field;
                var fieldType = config.fieldTypes[field];

                if (fieldType === 'dropdown') {
                    var dataType = config.masterDataTypes[field];
                    var options = config.masterData[dataType] || [];
                    var matched = options.find(o => (o.Code || o.Id) == cell.dataset.value);
                    cell.textContent = matched ? (matched.Name || matched.FullName) : '--';
                } else if (fieldType === 'date') {
                    // Reformat date
                    if (cell.dataset.value) {
                        var parts = cell.dataset.value.split('-');
                        if (parts.length === 3) cell.textContent = parts[2] + '/' + parts[1] + '/' + parts[0];
                        else cell.textContent = cell.dataset.value;
                    } else {
                        cell.textContent = '--';
                    }
                } else {
                    cell.textContent = cell.dataset.value || '--';
                }
            }
        });

        // Revert action cell
        var actionCell = row.querySelector('td:last-child');
        if (actionCell.dataset.originalContent) {
            actionCell.innerHTML = actionCell.dataset.originalContent;
        }

        row.classList.remove('row-dirty');

        // Nếu là dòng mới thêm (-1) thì xóa luôn
        if (row.dataset.id === '-1') {
            row.remove();
            renumberRows();
        }
    }

    /**
     * Save row data to server
     */
    async function saveRow(button) {
        var row = button.closest('tr');
        var employeeId = row.dataset.id;

        // Disable buttons
        var btns = row.querySelectorAll('button');
        btns.forEach(b => b.disabled = true);

        try {
            var formData = new FormData();

            // Collect updated fields
            if (employeeId !== '-1') {
                formData.append('Id', employeeId);
            }

            // Add all fields
            row.querySelectorAll('.' + config.editableCellClass).forEach(function (cell) {
                var field = cell.dataset.field;
                var value = cell.dataset.value;
                if (value !== undefined && value !== null) {
                    formData.append(field, value);
                }
            });

            var token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (!token) throw new Error('CSRF token not found');

            var url = employeeId === '-1' ? '/Employee?handler=QuickAdd' : '/Employee?handler=QuickUpdate';

            // Nếu update, QuickUpdate hiện tại chỉ update single field. 
            // Ta cần update API QuickUpdate để nhận full object hoặc gọi loop?
            // API hiện tại dùng EmployeeQuickUpdate map vào itemInfoUpdate.
            // EmployeeQuickUpdate có các trường nullable, nên gửi lên cũng OK.

            // Tuy nhiên QuickUpdate đang thiết kế hơi hướng nhận 1 field. 
            // Xem lại EmployeeQuickUpdate: nó chứa FullName, GroupId... => Nó là object. 
            // Vậy gửi FormData full object lên là OK.

            var response = await fetch(url, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token.value
                },
                body: formData
            });

            if (response.ok) {
                var result = await response.json();

                showToast('Đã lưu thành công', 'success');

                // Update row ID if new
                if (employeeId === '-1' && (result.id || result.Id)) {
                    row.dataset.id = result.id || result.Id;
                }

                // Revert action cell logic
                var actionCell = row.querySelector('td:last-child');
                // Restore original buttons (Delete/Detail) but keep row clean
                // Thực ra nên refresh lại action button từ server hoặc giữ nguyên logic cũ
                // Cho đơn giản: ta restore lại view/delete buttons của dòng đó
                // Tuy nhiên ta cần cập nhật view detail link với ID mới nếu là add new

                if (employeeId === '-1') {
                    // Nếu là dòng mới, cần tạo action buttons mới đúng chuẩn
                    var newId = row.dataset.id;
                    actionCell.innerHTML = `
                        <a href="/EmployeeInfo?id=${newId}" title="Xem chi tiết"><i class="bi bi-eye-fill text-primary"></i></a>
                        <a href="javascript:void(0)" onclick="confirmDelete(${newId})" title="Xóa"><i class="bi bi-trash-fill"></i></a>
                        <a href="javascript:void(0)" onclick="OpenChangPassword(${newId})" title="Đổi mật khẩu"><i class="bi bi-compass-fill"></i></a>
                     `;
                    actionCell.dataset.originalContent = actionCell.innerHTML; // Update backup
                } else {
                    if (actionCell.dataset.originalContent) {
                        actionCell.innerHTML = actionCell.dataset.originalContent;
                    }
                }

                row.classList.remove('row-dirty');

                // Update original values
                row.querySelectorAll('.' + config.editableCellClass).forEach(function (cell) {
                    cell.dataset.originalValue = cell.dataset.value;
                });

            } else {
                var error = await response.json();
                showToast('Lỗi: ' + (error.message || 'Không thể lưu'), 'error');
                btns.forEach(b => b.disabled = false);
            }
        } catch (error) {
            console.error('Save error:', error);
            showToast('Lỗi kết nối server', 'error');
            btns.forEach(b => b.disabled = false);
        }
    }

    /**
     * Handle keydown while editing
     */
    function handleEditingKeydown(e) {
        if (e.key === 'Enter') {
            finishEditing();
            e.preventDefault();
        } else if (e.key === 'Escape') {
            // Cancel current cell editing (not row)
            var cell = state.currentEditingCell;
            if (cell) {
                cell.textContent = cell.dataset.originalValue || cell.dataset.value || '--'; // Revert visual
                // Nhưng thực ra nếu đang edit và bấm Esc, chỉ nên thoát edit mode của cell đó mà không lưu giá trị thay đổi vào dataset.value
                // Logic finishEditing ở trên đang lưu dataset.value = input.value
                // Nên ta cần revert tay ở đây trước khi finish
                // Tuy nhiên logic này phức tạp, tạm thời Enter để confirm, click ra ngoài confirm.
            }
            state.currentEditingCell = null;
            cell.classList.remove(config.editingClass);
            state.isEditing = false;
            e.preventDefault();
        } else if (e.key === 'Tab') {
            finishEditing();
            moveToNextCell(e.shiftKey);
            e.preventDefault();
        }
    }

    /**
     * Handle keydown on grid (not editing)
     */
    function handleGridKeydown(e) {
        if (!state.isEditModeEnabled) return;
        if (!state.selectedRow) return;

        if (e.ctrlKey && e.key === 'c') {
            copyRow();
            e.preventDefault();
        } else if (e.ctrlKey && e.key === 'v') {
            pasteRow();
            e.preventDefault();
        } else if (e.key === 'Delete') {
            deleteRow();
            e.preventDefault();
        }
    }

    /**
     * Move to next editable cell
     */
    function moveToNextCell(reverse) {
        var cells = document.querySelectorAll('.' + config.editableCellClass);
        var currentIndex = Array.from(cells).indexOf(state.currentEditingCell);
        var nextIndex = reverse ? currentIndex - 1 : currentIndex + 1;

        if (nextIndex >= 0 && nextIndex < cells.length) {
            startEditing(cells[nextIndex]);
        }
    }

    /**
     * Select a row
     */
    function selectRow(row) {
        if (state.selectedRow) {
            state.selectedRow.classList.remove(config.selectedRowClass);
        }
        state.selectedRow = row;
        row.classList.add(config.selectedRowClass);
    }

    /**
     * Show context menu
     */
    function showContextMenu(x, y) {
        var menu = document.getElementById(config.contextMenuId);
        if (!menu) return;

        menu.style.display = 'block';
        menu.style.left = x + 'px';
        menu.style.top = y + 'px';

        // Adjust if menu goes off screen
        var rect = menu.getBoundingClientRect();
        if (rect.right > window.innerWidth) {
            menu.style.left = (x - rect.width) + 'px';
        }
        if (rect.bottom > window.innerHeight) {
            menu.style.top = (y - rect.height) + 'px';
        }
    }

    /**
     * Hide context menu
     */
    function hideContextMenu() {
        var menu = document.getElementById(config.contextMenuId);
        if (menu) {
            menu.style.display = 'none';
        }
    }

    /**
     * Handle context menu actions
     */
    function handleContextMenuAction(action) {
        switch (action) {
            case 'add-above':
                addNewRow('above');
                break;
            case 'add-below':
                addNewRow('below');
                break;
            case 'copy':
                copyRow();
                break;
            case 'paste':
                pasteRow();
                break;
            case 'delete':
                deleteRow();
                break;
        }
    }

    /**
     * Add new row
     */
    function addNewRow(position) {
        if (!state.isEditModeEnabled) return;

        var table = document.querySelector(config.tableSelector);
        var tbody = table.querySelector('tbody');
        var template = createEmptyRow();

        if (state.selectedRow) {
            if (position === 'above') {
                state.selectedRow.parentNode.insertBefore(template, state.selectedRow);
            } else {
                state.selectedRow.parentNode.insertBefore(template, state.selectedRow.nextSibling);
            }
        } else {
            tbody.appendChild(template);
        }

        selectRow(template);
        renumberRows();

        // Mark new row as dirty appropriately is handled by createEmptyRow's HTML
        // But better trigger edit on first cell
        var firstEditable = template.querySelector('.' + config.editableCellClass);
        if (firstEditable) startEditing(firstEditable);
    }

    // Add row at end
    function addRowAtEnd() {
        if (!state.isEditModeEnabled) {
            showToast('Vui lòng bật chế độ chỉnh sửa trước', 'warning');
            return;
        }
        addNewRow('below'); // Or just append
        // scroll handled by user focus
    }

    /**
     * Create empty row template
     */
    function createEmptyRow() {
        var row = document.createElement('tr');
        row.dataset.id = '-1'; // New row
        row.classList.add('row-dirty');

        row.innerHTML = `
            <td>--</td>
            <td>--</td>
            <td class="editable-cell" data-field="FullName" data-value=""></td>
            <td class="editable-cell" data-field="RoleCode" data-value=""></td>
            <td class="editable-cell" data-field="PositionCode" data-value=""></td>
            <td class="editable-cell" data-field="DepartmentCode" data-value=""></td>
            <td>--</td>
            <td class="editable-cell" data-field="Status" data-value=""></td>
            <td class="editable-cell" data-field="StatusWork" data-value=""></td>
            <td class="editable-cell" data-field="DocumentStatus" data-value=""></td>
            <td class="editable-cell" data-field="Onboard" data-value=""></td>
            <td>--</td>
            <td>
                <div class="row-actions-edit">
                    <button class="btn btn-sm btn-success me-1" onclick="EditableGrid.saveRow(this)" title="Lưu"><i class="bi bi-check-lg"></i></button>
                    <button class="btn btn-sm btn-danger" onclick="EditableGrid.cancelRow(this)" title="Hủy"><i class="bi bi-x-lg"></i></button>
                </div>
            </td>
        `;

        return row;
    }

    /**
     * Copy row data
     */
    function copyRow() {
        if (!state.selectedRow) {
            showToast('Vui lòng chọn một dòng để copy', 'warning');
            return;
        }

        config.copiedRowData = {
            id: state.selectedRow.dataset.id,
            cells: {}
        };

        state.selectedRow.querySelectorAll('.' + config.editableCellClass).forEach(function (cell) {
            config.copiedRowData.cells[cell.dataset.field] = {
                value: cell.dataset.value,
                text: cell.textContent
            };
        });

        showToast('Đã copy dòng', 'info');
    }

    /**
     * Paste row data
     */
    async function pasteRow() {
        if (!config.copiedRowData) {
            showToast('Chưa có dữ liệu để paste', 'warning');
            return;
        }

        // Khi paste, thay vì gọi API ngay, ta fill data vào row mới và để user save
        addNewRow('below');
        var newRow = state.selectedRow; // addNewRow selects the new row

        // Fill data
        var copiedCells = config.copiedRowData.cells;
        newRow.querySelectorAll('.' + config.editableCellClass).forEach(function (cell) {
            var field = cell.dataset.field;
            if (copiedCells[field]) {
                cell.dataset.value = copiedCells[field].value;
                cell.textContent = copiedCells[field].text || '--';
            }
        });

        showToast('Đã paste dữ liệu. Vui lòng bấm Lưu để xác nhận.', 'info');
    }

    /**
     * Delete row
     */
    function deleteRow() {
        if (!state.selectedRow) {
            showToast('Vui lòng chọn một dòng để xóa', 'warning');
            return;
        }

        var employeeId = state.selectedRow.dataset.id;

        if (employeeId === '-1') {
            state.selectedRow.remove();
            state.selectedRow = null;
            renumberRows();
            return;
        }

        // Confirm delete
        Swal.fire({
            title: 'Xác nhận xóa?',
            text: 'Bạn có chắc muốn xóa nhân viên này?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            confirmButtonText: 'Xóa',
            cancelButtonText: 'Hủy'
        }).then(async function (result) {
            if (result.isConfirmed) {
                await performDelete(employeeId);
            }
        });
    }

    /**
     * Perform delete API call
     */
    async function performDelete(employeeId) {
        try {
            var formData = new FormData();
            formData.append('Id', employeeId);

            var token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (!token) return;

            var response = await fetch('/Employee?handler=Delete', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token.value
                },
                body: formData
            });

            if (response.ok) {
                state.selectedRow.remove();
                state.selectedRow = null;
                renumberRows();
                showToast('Đã xóa nhân viên', 'success');
            } else {
                showToast('Không thể xóa nhân viên', 'error');
            }
        } catch (error) {
            console.error('Delete error:', error);
            showToast('Lỗi kết nối server', 'error');
        }
    }

    /**
     * Renumber rows
     */
    function renumberRows() {
        var rows = document.querySelectorAll(config.tableSelector + ' tbody tr');
        rows.forEach(function (row, index) {
            var firstCell = row.querySelector('td:first-child');
            if (firstCell && row.dataset.id !== '-1') {
                firstCell.textContent = index + 1;
            }
        });
    }

    /**
     * Show toast notification
     */
    function showToast(message, type) {
        if (typeof Swal !== 'undefined') {
            var icon = type === 'success' ? 'success' :
                type === 'error' ? 'error' :
                    type === 'warning' ? 'warning' : 'info';

            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: icon,
                title: message,
                showConfirmButton: false,
                timer: 2000,
                timerProgressBar: true
            });
        } else {
            console.log('[' + type + '] ' + message);
        }
    }

    // Public API
    return {
        init: init,
        toggleEditMode: toggleEditMode,
        saveRow: saveRow,
        cancelRow: cancelRow,
        addRowAtEnd: addRowAtEnd,
        copyRow: copyRow,
        pasteRow: pasteRow,
        deleteRow: deleteRow
    };
})();

// Auto-initialize when DOM ready
document.addEventListener('DOMContentLoaded', function () {
    if (document.querySelector('.editable-grid')) {
        EditableGrid.init();
    }
});
