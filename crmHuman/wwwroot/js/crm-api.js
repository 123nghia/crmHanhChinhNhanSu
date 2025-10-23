(function (global, BaseService) {
    'use strict';

    if (!BaseService) {
        console.error('CrmApi requires BaseService. Ensure baseService.js is loaded first.');
        return;
    }

    const toArray = (payload) => Array.isArray(payload?.data) ? payload.data : [];

    const CrmApi = {
        toArray,
        groups: () => BaseService.getJSON('/GroupPage?handler=AllGroup'),
        groupMembers: (groupId) => BaseService.getJSON(`/GroupPage?handler=ListMember&&groupId=${groupId}`),
        partners: () => BaseService.getJSON('/Partner?handler=AllData'),
        jobs: () => BaseService.getJSON('/Job?handler=AllData'),
        jobFilter: (params) => BaseService.getJSON('/job?handler=AllJob', params),
        projects: (relId) => BaseService.getJSON(`/common?handler=GetAllParrentChild&idRel=${relId}`),
        addresses: (relId) => BaseService.getJSON(`/common?handler=GetAllParrentAddress&idRel=${relId}`),
        statuses: () => BaseService.getJSON('/StatusPage?handler=AllData'),
        jobFullText: (jobId) => BaseService.getJSON(`/job?handler=FullText&id=${jobId}`),
        uploadFile: (formData) => BaseService.postFormData('/file?handler=Upload', formData),
        makeCall: (payload) =>
            BaseService.request({
                url: '/Call?handler=MakeCall',
                type: 'POST',
                data: payload
            })
    };

    global.CrmApi = CrmApi;
})(window, window.BaseService);

