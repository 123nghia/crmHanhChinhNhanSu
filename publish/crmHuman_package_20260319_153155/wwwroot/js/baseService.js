(function (global, $) {
    'use strict';

    if (!$) {
        console.error('BaseService requires jQuery to be loaded beforehand.');
        return;
    }

    const getToken = () => $('input[name="__RequestVerificationToken"]').val();

    const buildHeaders = (headers = {}) => {
        const token = getToken();
        return token
            ? { 'RequestVerificationToken': token, ...headers }
            : { ...headers };
    };

    const request = (options = {}) => {
        const { headers: customHeaders = {}, ...rest } = options;
        const ajaxConfig = {
            dataType: 'json',
            ...rest,
            headers: buildHeaders(customHeaders)
        };

        return new Promise((resolve, reject) => {
            $.ajax({
                ...ajaxConfig,
                success: resolve,
                error: (jqXHR, textStatus, errorThrown) => {
                    reject({ jqXHR, textStatus, error: errorThrown });
                }
            });
        });
    };

    const getJSON = (url, data = {}, options = {}) =>
        request({
            url,
            type: 'GET',
            data,
            ...options
        });

    const postJSON = (url, data = {}, options = {}) =>
        request({
            url,
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json; charset=UTF-8',
            processData: false,
            ...options
        });

    const postFormData = (url, formData, options = {}) =>
        request({
            url,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            ...options
        });

    global.BaseService = {
        request,
        getJSON,
        postJSON,
        postFormData
    };
})(window, window.jQuery);

