(function (global, $, BaseService, DomUtils, CrmApi) {
    'use strict';

    if (!$ || !BaseService || !DomUtils || !CrmApi) {
        console.error('main.js requires jQuery, BaseService, DomUtils and CrmApi.');
        return;
    }

    const SELECTORS = {
        partnerPrimary: 'cbpartnerId2',
        partnerSecondary: 'cbpartnerId3',
        partnerFallback: 'cbpartnerId4',
        group: 'cbgroup',
        member: 'cbmember',
        jobPrimary: 'cbJobId',
        jobSecondary: 'cbJobId1',
        project: 'cbProject',
        status: 'cbstatus',
        limit: 'cbLimit',
        address: 'cbAddress'
    };

    const DEFAULT_OPTIONS = {
        all: { text: 'Tất cả', value: '-1' },
        address: { text: 'Chọn địa chỉ', value: '-1' }
    };

    const safeNumber = (value) => Number(value) || 0;
    const isOrderDetailPage = () => global.location?.pathname?.includes('OrderDetail');

    const ErrorHandler = {
        notify(error) {
            if (error?.jqXHR && typeof global.showError === 'function') {
                global.showError(error.jqXHR);
                return;
            }

            if (error?.jqXHR) {
                console.error('Request failed', error.jqXHR);
            } else {
                console.error('Request failed', error);
            }
        }
    };

    const AddressSection = {
        toggle(useDefault) {
            const defaultCheckbox = DomUtils.get('checkDefaultAddress');
            const otherCheckbox = DomUtils.get('checkOtherAddress');

            if (defaultCheckbox) {
                defaultCheckbox.checked = useDefault;
            }

            if (otherCheckbox) {
                otherCheckbox.checked = !useDefault;
            }

            DomUtils.setDisplay('divAdddressDefault', useDefault);
            DomUtils.setDisplay('divAdddressOther', !useDefault);
        }
    };

    const Dropdowns = {
        async partners(selectId = SELECTORS.partnerPrimary) {
            const select = DomUtils.get(selectId);
            if (!select) {
                return [];
            }

            DomUtils.populateSelect(select, [], { defaultOption: DEFAULT_OPTIONS.all });

            try {
                const response = await CrmApi.partners();
                const partners = CrmApi.toArray(response);

                DomUtils.populateSelect(select, partners, {
                    textKey: 'shortName',
                    valueKey: 'id',
                    defaultOption: DEFAULT_OPTIONS.all
                });

                return partners;
            } catch (error) {
                ErrorHandler.notify(error);
                return [];
            }
        },

        async groups() {
            const select = DomUtils.get(SELECTORS.group);
            if (!select) {
                return [];
            }

            DomUtils.populateSelect(select, [], { defaultOption: DEFAULT_OPTIONS.all });

            try {
                const response = await CrmApi.groups();
                const groups = CrmApi.toArray(response);

                DomUtils.populateSelect(select, groups, {
                    textKey: 'name',
                    valueKey: 'id',
                    defaultOption: DEFAULT_OPTIONS.all,
                    onOption: (option) => {
                        const request = global.extraSearchRequest;
                        const match = request && safeNumber(request.groupSearch) === safeNumber(option.value);
                        if (match) {
                            option.selected = true;
                            request.groupSearch = -2;
                        }
                    }
                });

                select.onchange = (event) => {
                    const value = event?.target?.value ?? select.value;
                    Dropdowns.groupMembers(value);
                };

                DomUtils.dispatchChange(select);
                return groups;
            } catch (error) {
                ErrorHandler.notify(error);
                return [];
            }
        },

        async groupMembers(groupId) {
            const select = DomUtils.get(SELECTORS.member);
            if (!select) {
                return [];
            }

            DomUtils.populateSelect(select, [], { defaultOption: DEFAULT_OPTIONS.all });

            if (!groupId || safeNumber(groupId) < 0) {
                return [];
            }

            try {
                const response = await CrmApi.groupMembers(groupId);
                const members = CrmApi.toArray(response);

                DomUtils.populateSelect(select, members, {
                    textKey: 'memberName',
                    valueKey: 'memberId',
                    defaultOption: DEFAULT_OPTIONS.all,
                    onOption: (option) => {
                        const request = global.extraSearchRequest;
                        const match = request && safeNumber(request.memberSearch) === safeNumber(option.value);
                        if (match) {
                            option.selected = true;
                            request.memberSearch = -2;
                        }
                    }
                });

                DomUtils.dispatchChange(select);
                return members;
            } catch (error) {
                ErrorHandler.notify(error);
                return [];
            }
        },

        async jobs() {
            const select = DomUtils.get(SELECTORS.jobPrimary);
            if (!select) {
                return [];
            }

            DomUtils.populateSelect(select, [], { defaultOption: DEFAULT_OPTIONS.all });

            try {
                const response = await CrmApi.jobs();
                const jobs = CrmApi.toArray(response);

                DomUtils.populateSelect(select, jobs, {
                    textKey: 'name',
                    valueKey: 'id',
                    defaultOption: DEFAULT_OPTIONS.all
                });

                select.onchange = () => {
                    const groupSelect = DomUtils.get(SELECTORS.group);
                    if (groupSelect) {
                        Dropdowns.groupMembers(groupSelect.value);
                    }
                };

                return jobs;
            } catch (error) {
                ErrorHandler.notify(error);
                return [];
            }
        },

        async jobFilter() {
            const select = DomUtils.get(SELECTORS.jobPrimary);
            if (!select) {
                return [];
            }

            const params = {
                PartnerId: DomUtils.get(SELECTORS.partnerPrimary)?.value ?? '',
                ProjectId: DomUtils.get(SELECTORS.project)?.value ?? '',
                LinhvucId: DomUtils.get('cbField')?.value ?? ''
            };

            try {
                const response = await CrmApi.jobFilter(params);
                const jobs = CrmApi.toArray(response);

                DomUtils.populateSelect(select, jobs, {
                    textKey: 'name',
                    valueKey: 'id',
                    defaultOption: DEFAULT_OPTIONS.all
                });

                return jobs;
            } catch (error) {
                ErrorHandler.notify(error);
                return [];
            }
        },

        async projects(relValue, targetId = SELECTORS.project) {
            const select = DomUtils.get(targetId);
            if (!select) {
                return [];
            }

            DomUtils.populateSelect(select, [], { defaultOption: DEFAULT_OPTIONS.all });

            if (!relValue) {
                return [];
            }

            try {
                const response = await CrmApi.projects(relValue);
                const projects = CrmApi.toArray(response);

                DomUtils.populateSelect(select, projects, {
                    textKey: 'text',
                    valueKey: 'id',
                    defaultOption: DEFAULT_OPTIONS.all
                });

                return projects;
            } catch (error) {
                ErrorHandler.notify(error);
                return [];
            }
        },

        async projectDetail(relValue) {
            await Dropdowns.projects(relValue, SELECTORS.project);
            if (relValue) {
                await Dropdowns.addresses(relValue);
            }
        },

        async addresses(relValue, { autoToggle = true, autoDispatch = true } = {}) {
            const select = DomUtils.get(SELECTORS.address);
            if (!select) {
                return [];
            }

            DomUtils.populateSelect(select, [], { defaultOption: DEFAULT_OPTIONS.address });

            if (!relValue) {
                if (autoToggle) {
                    AddressSection.toggle(false);
                }
                return [];
            }

            try {
                const response = await CrmApi.addresses(relValue);
                const addresses = CrmApi.toArray(response);

                DomUtils.populateSelect(select, addresses, {
                    textKey: 'text',
                    valueKey: 'id',
                    defaultOption: DEFAULT_OPTIONS.address
                });

                if (autoToggle) {
                    AddressSection.toggle(addresses.length > 0);
                }

                if (autoDispatch) {
                    DomUtils.dispatchChange(select);
                }

                return addresses;
            } catch (error) {
                ErrorHandler.notify(error);
                return [];
            }
        },

        async addressesByPartner(partnerId) {
            return Dropdowns.addresses(partnerId, {
                autoToggle: true,
                autoDispatch: false
            });
        },

        async statuses() {
            const select = DomUtils.get(SELECTORS.status);
            if (!select) {
                return [];
            }

            DomUtils.populateSelect(select, [], { defaultOption: DEFAULT_OPTIONS.all });

            try {
                const response = await CrmApi.statuses();
                const statuses = CrmApi.toArray(response);

                DomUtils.populateSelect(select, statuses, {
                    textKey: 'name',
                    valueKey: 'id',
                    defaultOption: DEFAULT_OPTIONS.all
                });

                DomUtils.dispatchChange(select);
                return statuses;
            } catch (error) {
                ErrorHandler.notify(error);
                return [];
            }
        }
    };

    const Navigation = {
        setActiveMenu() {
            const path = global.location?.pathname || '';
            if (!path) {
                return;
            }

            const matches = document.querySelectorAll(`.sidebar-nav a[href*="${path}"]`);
            if (!matches.length) {
                return;
            }

            const activeLink = matches[0];
            activeLink.classList.add('active');

            const parentList = activeLink.closest('ul');
            if (parentList) {
                parentList.classList.add('show');
            }
        }
    };

    const Uploader = {
        async upload(fileInput, type = 'candidate') {
            if (!fileInput?.files?.length) {
                return;
            }

            const formData = new FormData();
            formData.append('FileRequest', fileInput.files[0]);
            formData.append('Type', type);

            try {
                const response = await CrmApi.uploadFile(formData);
                Uploader.handleSuccess(response, fileInput);
            } catch (error) {
                ErrorHandler.notify(error);
            }
        },

        handleSuccess(dataResponse, fileInput) {
            if (!fileInput) {
                return;
            }

            const linkResult = dataResponse?.linkResult;
            if (!linkResult) {
                return;
            }

            const group = fileInput.closest('.form-group');
            if (!group) {
                return;
            }

            const fileResult = group.querySelector('.fileResult');
            if (fileResult) {
                fileResult.innerHTML = '';
                const link = document.createElement('a');
                link.href = linkResult;
                link.target = '_blank';
                link.textContent = 'Thông tin file';
                fileResult.appendChild(link);
            }

            const fileValueInput = group.querySelector('.valuefile');
            if (fileValueInput) {
                fileValueInput.value = linkResult;
            }
        }
    };

    const CallCenter = {
        showAlert(message, footer) {
            if (global.Swal) {
                Swal.fire({
                    icon: 'error',
                    title: message,
                    text: message,
                    footer
                });
            } else {
                alert(message);
            }
        },

        async call(typeCall, idRel, elementClick) {
            if (!idRel || idRel === '-1') {
                CallCenter.showAlert('Không có đối tượng để gọi, vui lòng tạo thông tin trước', '<a>Vi phạm gọi</a>');
                return;
            }

            if (!typeCall) {
                CallCenter.showAlert('Không có đối tượng để gọi', '<a>Có lỗi trong thao tác gọi</a>');
                return;
            }

            const phoneInput = elementClick?.closest('.input-group')?.querySelector('.phonecall');
            const phoneNumber = phoneInput?.value || '';

            if (phoneNumber.length < 10) {
                CallCenter.showAlert('Số điện thoại không chính xác hoặc không có', '<a>Có lỗi số điện thoại</a>');
                return;
            }

            if (global.Swal) {
                Swal.fire({
                    title: 'Đang thực hiện cuộc gọi, vui lòng chờ đợi',
                    width: 800,
                    padding: '3em',
                    color: '#716add',
                    background: '#fff url(/assets/trees.png)',
                    backdrop: 'rgba(0,0,123,0.4) left top no-repeat'
                });
            }

            try {
                await CrmApi.makeCall({
                    typecall: typeCall,
                    phonecall: phoneNumber,
                    idrel: idRel
                });

                if (global.Swal) {
                    Swal.fire({
                        title: 'Đang chuyển tiếp đến microsip, thao tác thành công',
                        width: 800,
                        padding: '3em',
                        color: '#716add',
                        timer: 4000,
                        background: '#fff url(/assets/trees.png)',
                        backdrop: 'rgba(0,0,123,0.4) left top no-repeat'
                    });
                }
            } catch (error) {
                if (global.Swal) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Có lỗi trong quá trình gọi',
                        text: 'Vui lòng liên hệ IT!',
                        footer: 'Gọi thất bại'
                    });
                } else {
                    ErrorHandler.notify(error);
                }
            }
        }
    };

    const FileActions = {
        openInNewTab(element) {
            if (!element) {
                return;
            }

            const container = element.closest('.form-group');
            const input = container?.querySelector('input');
            const link = input?.value;

            if (link) {
                const win = global.open(link, '_blank');
                if (win) {
                    win.focus();
                }
            }
        },

        downloadRecord(filePath = '') {
            if (!filePath) {
                return;
            }

            const anchor = document.createElement('a');
            anchor.href = `http://192.168.1.3:7224/api/file/getaudio9?filePath=${filePath}`;
            anchor.target = '_blank';
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
        }
    };

    const ComboBox = {
        init() {
            const candidateInput = DomUtils.get('cbcandidateId');
            const input = DomUtils.get('input');
            const datalist = DomUtils.get('browsers');

            if (!candidateInput || !input || !datalist) {
                return;
            }

            const options = Array.from(datalist.options);
            let currentFocus = -1;

            input.onfocus = () => {
                datalist.style.display = 'block';
                input.style.borderRadius = '5px 5px 0 0';
            };

            options.forEach((option) => {
                option.onclick = () => {
                    input.value = option.text;
                    candidateInput.value = option.value;
                    datalist.style.display = 'none';
                    input.style.borderRadius = '5px';
                };
            });

            input.oninput = () => {
                currentFocus = -1;
                const text = input.value.toUpperCase();
                options.forEach((option) => {
                    const matches = option.value.toUpperCase().includes(text) ||
                        option.text.toUpperCase().includes(text);
                    option.style.display = matches ? 'block' : 'none';
                });
            };

            input.onkeydown = (event) => {
                if (event.keyCode === 40) {
                    currentFocus += 1;
                    addActive(options);
                } else if (event.keyCode === 38) {
                    currentFocus -= 1;
                    addActive(options);
                } else if (event.keyCode === 13) {
                    event.preventDefault();
                    if (currentFocus > -1 && options[currentFocus]) {
                        options[currentFocus].click();
                    }
                }
            };

            const addActive = (list) => {
                if (!list.length) {
                    return;
                }

                removeActive(list);

                if (currentFocus >= list.length) {
                    currentFocus = 0;
                }

                if (currentFocus < 0) {
                    currentFocus = list.length - 1;
                }

                list[currentFocus].classList.add('active');
            };

            const removeActive = (list) => {
                list.forEach((option) => option.classList.remove('active'));
            };
        }
    };

    const CRMApp = {
        async init() {
            Navigation.setActiveMenu();

            const loads = [
                Dropdowns.partners(SELECTORS.partnerPrimary),
                Dropdowns.partners(SELECTORS.partnerSecondary),
                Dropdowns.statuses()
            ];

            if (typeof global.extraSearchRequest !== 'undefined') {
                loads.push(Dropdowns.groups());
            }

            await Promise.all(loads);

            if (CRMApp.isDetailPage()) {
                await CRMApp.loadDataPartner();
            }

            if (isOrderDetailPage()) {
                global.setTimeout(() => {
                    DomUtils.triggerChange(`#${SELECTORS.jobSecondary}`);
                }, 300);
            }

            const limitSelect = DomUtils.get(SELECTORS.limit);
            if (limitSelect) {
                DomUtils.dispatchChange(limitSelect);
            }
        },

        isDetailPage() {
            const href = global.location?.href?.toLowerCase() || '';
            return href.includes('detail') || href.includes('job');
        },

        async loadDataPartner() {
            if (isOrderDetailPage()) {
                return;
            }

            const partnerSelect =
                DomUtils.get(SELECTORS.partnerPrimary) || DomUtils.get(SELECTORS.partnerFallback);

            if (!partnerSelect) {
                return;
            }

            DomUtils.dispatchChange(partnerSelect);

            const projectSelect = DomUtils.get(SELECTORS.project);
            const projectValueInput = DomUtils.get('txtProjectId');

            if (!projectSelect || !projectValueInput) {
                return;
            }

            const projectValue = safeNumber(projectValueInput.value);
            if (projectValue > 0) {
                projectSelect.value = String(projectValue);
                DomUtils.triggerChange(`#${SELECTORS.project}`);
            }
        },

        logout() {
            $('#logoutSubmit').trigger('click');
        },

        addNewAddress(elementClick, labelText = 'Địa chỉ') {
            if (!elementClick) {
                return;
            }

            const container = elementClick.closest('div');
            if (!container) {
                return;
            }

            const template = `
                <div class="col-12 form-group">
                    <label class="form-label">${labelText}</label>
                    <input type="text" customvalue="-1" class="form-control" placeholder="" />
                </div>
            `;

            container.insertAdjacentHTML('beforeend', template.trim());
        },

        async getAllPartner(id = SELECTORS.partnerPrimary) {
            return Dropdowns.partners(id);
        },

        async getAllGroup() {
            return Dropdowns.groups();
        },

        async getAllMemberOfGroup(groupId) {
            return Dropdowns.groupMembers(groupId);
        },

        async getAllJob() {
            return Dropdowns.jobs();
        },

        async getProject(elementChange, targetId = SELECTORS.project) {
            if (!elementChange) {
                return [];
            }

            return Dropdowns.projects(elementChange.value, targetId);
        },

        async getProjectDetailOrder(elementChange) {
            if (!elementChange) {
                return [];
            }

            return Dropdowns.projectDetail(elementChange.value);
        },

        async getAllAddress(elementChange) {
            if (!elementChange) {
                return [];
            }

            return Dropdowns.addresses(elementChange.value);
        },

        async getAllAddress2(value) {
            return Dropdowns.addressesByPartner(value);
        },

        changeSelect(_, valueSelect) {
            AddressSection.toggle(Number(valueSelect) === 1);
        },

        async getAllJobFilter() {
            return Dropdowns.jobFilter();
        },

        async getAllStatus() {
            return Dropdowns.statuses();
        },

        callFrom(typeCall, idRel, elementClick) {
            CallCenter.call(typeCall, idRel, elementClick);
        },

        UploadImage(fileInput, type = 'candidate') {
            Uploader.upload(fileInput, type);
        },

        successUpload(dataResponse, fileInput) {
            Uploader.handleSuccess(dataResponse, fileInput);
        },

        openFileNewTag(element) {
            FileActions.openInNewTab(element);
        },

        async getFullPathJob(elementChange) {
            if (!elementChange) {
                return;
            }

            try {
                const response = await CrmApi.jobFullText(elementChange.value);
                if (!response) {
                    return;
                }

                const fullPathJobDiv = DomUtils.get('fullPathJob');
                if (fullPathJobDiv) {
                    fullPathJobDiv.innerHTML = '';
                    const paragraph = document.createElement('p');
                    paragraph.innerHTML = response.pathFullText || '';
                    paragraph.style.display = 'none';
                    fullPathJobDiv.appendChild(paragraph);
                }

                if (response.partnerId) {
                    const partnerSelect = DomUtils.get(SELECTORS.partnerSecondary);
                    if (partnerSelect) {
                        partnerSelect.value = response.partnerId;
                    }

                    await Dropdowns.addressesByPartner(response.partnerId);
                }
            } catch (error) {
                ErrorHandler.notify(error);
            }
        },

        showOrderList(statusId) {
            const href = `/order?status=${statusId}`;
            const win = global.open(href, '_blank');
            if (win) {
                win.focus();
            }
        },

        isBlank(value) {
            return !value || /^\s*$/.test(value);
        },

        changeStatusQuick(selectElement) {
            if (!selectElement) {
                return;
            }

            const value = safeNumber(selectElement.value);
            const shouldShow = value === 12 || value === 9;
            DomUtils.setDisplay('div_Status', shouldShow);
        },

        loadCombobx() {
            ComboBox.init();
        },

        formatDateTime(value) {
            return value;
        },

        dowloadfileRecord(filePath = '') {
            FileActions.downloadRecord(filePath);
        }
    };

    $(document).ready(() => {
        CRMApp.init();
    });

    Object.assign(global, {
        loadDataPartner: CRMApp.loadDataPartner.bind(CRMApp),
        logout: CRMApp.logout,
        activeMenu: Navigation.setActiveMenu,
        getAllMemberOfGroup: CRMApp.getAllMemberOfGroup,
        getAllGroup: CRMApp.getAllGroup,
        UploadImage: CRMApp.UploadImage,
        successUpload: CRMApp.successUpload,
        callFrom: CRMApp.callFrom,
        addNewAddress: CRMApp.addNewAddress,
        getAllPartner: CRMApp.getAllPartner,
        getAllJob: CRMApp.getAllJob,
        getProject: CRMApp.getProject,
        getProjectDetailOrder: CRMApp.getProjectDetailOrder,
        getAllAddress: CRMApp.getAllAddress,
        getAllAddress2: CRMApp.getAllAddress2,
        changeSelect: CRMApp.changeSelect,
        getAllJobFilter: CRMApp.getAllJobFilter,
        getAllStatus: CRMApp.getAllStatus,
        openFileNewTag: CRMApp.openFileNewTag,
        getFullPathJob: CRMApp.getFullPathJob,
        showOrderList: CRMApp.showOrderList,
        isBlank: CRMApp.isBlank,
        changeStatusQuick: CRMApp.changeStatusQuick,
        loadCombobx: CRMApp.loadCombobx,
        formatDateTime: CRMApp.formatDateTime,
        dowloadfileRecord: CRMApp.dowloadfileRecord
    });
})(window, window.jQuery, window.BaseService, window.DomUtils, window.CrmApi);

