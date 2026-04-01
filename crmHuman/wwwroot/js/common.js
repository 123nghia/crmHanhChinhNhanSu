function addError(idInput, message) {
    var input = document.getElementById(idInput);
    if (input == null) {
        return;
    }
    var containerdiv = input.closest(".form-group");
    if (containerdiv == null) {
        return;
    }
    containerdiv.classList.add("hasError");
    if (message != "") {
        var childElement = containerdiv.getElementsByClassName("invalid-feedback-cs");
        if (childElement.length > 0) {
            childElement[0].textContent = message;
        }

    }

}

function removeError(idInput) {
    var input = document.getElementById(idInput);
    if (input == null) {
        return;
    }
    var containerdiv = input.closest(".form-group");
    containerdiv
    if (containerdiv == null) {
        return;
    }
    containerdiv.classList.remove("hasError");
}

function removeAllEror(idForm) {
    var formEle = document.getElementById(idForm);
    var allError = formEle.querySelectorAll(".hasError");
    for (var i = 0; i < allError.length; i++) {
        allError[i].classList.remove("hasError");
    }
}

function validateForm() {
    return true;
}

function successAdd(id = -1, deleteRecord = false, isReload=true) {
    var timer1 = 1500;
    var textMess = "Thêm mới thành công";
    if (id > 0 ) {
        textMess = "Cập nhật thành công";
    }
    if (deleteRecord ==true) {
        textMess = "Xoá thành công";
        timer1 = 3000;
    }
    Swal.fire({
        position: "center",
        icon: "success",
        title: textMess,
        showConfirmButton: false,
        timer: timer1
    }).then((result) => {
        if(isReload ==false)
        {

        }
        else 
        {
            window.location.reload();
        }
        
    });
}
function khoiphucXoa() {
    var timer1 = 1500;
    var textMess  = "khôi phục thành công";
 
    Swal.fire({
        position: "center",
        icon: "success",
        title: textMess,
        showConfirmButton: false,
        timer: 3000
    }).then((result) => {
        window.location.reload();
    });
}

function openAlertAndClosePopup(id = -1, deleteRecord = false) {
    var timer1 = 1500;
    var textMess = "Thêm mới thành công";
    if (id > 0) {
        textMess = "Đổi mật khẩu thành công";
    }
    if (deleteRecord == true) {
        textMess = "Xoá thành công";
        timer1 = 3000;
    }
    Swal.fire({
        position: "center",
        icon: "success",
        title: textMess,
        showConfirmButton: false,
        timer: timer1
    }).then((result) => {

        $("#closePopup").click();

    });
}
function successAddMember(id = -1, deleteRecord = false,groupId =-1) {
    var timer1 = 1500;
    var textMess = "Thêm mới thành công";
    if (id > 0) {
        textMess = "Cập nhật thành công";
    }
    if (deleteRecord == true) {
        textMess = "Xoá thành công";
        timer1 = 3000;
    }
    Swal.fire({
        position: "center",
        icon: "success",
        title: textMess,
        showConfirmButton: false,
        timer: timer1
    }).then((result) => {
        loadData(groupId);
    });
}
function successDeleteMember(id = -1, deleteRecord = false, groupId = -1) {
    var timer1 = 1500;
    var textMess = "Thêm mới thành công";
    if (id > 0) {
        textMess = "Cập nhật thành công";
    }
    if (deleteRecord == true) {
        textMess = "Xoá thành công";
        timer1 = 3000;
    }
    Swal.fire({
        position: "center",
        icon: "success",
        title: textMess,
        showConfirmButton: false,
        timer: timer1
    }).then((result) => {
        loadData(groupId);
    });
}
function showError(jqXHRItem) {
    var statusCode = jqXHRItem.status;
    if (statusCode == 400) {
        var dataEror = jqXHRItem.responseJSON;
        for (var i = 0; i < dataEror.length; i++) {
            var itemName = dataEror[i].name;
            addError(itemName, dataEror[i].content);
        }
    }

}

function showErrorDelete(jqXHRItem) {

    Swal.fire({
        icon: "error",
        title: "Không thành công",
        text: "Xoá thất bại!"
        
    });
}

function getValueControl(idControl) {
    var valueCon = document.getElementById(idControl);
    if (valueCon == null || valueCon == undefined || valueCon == "") {

        return "";
    }
    return valueCon.value;
}
function addErorControl(idControl) {
       
}

function submitForm(formId) {
    var form = document.getElementById(formId);
    if (form == null) {
        return;
    }

    if (window.DuplicateSubmitGuard && typeof window.DuplicateSubmitGuard.submitForm === "function") {
        window.DuplicateSubmitGuard.submitForm(form);
        return;
    }

    if (typeof form.requestSubmit === "function") {
        form.requestSubmit();
        return;
    }

    form.submit();
}

function successAddImpact(id = -1, deleteRecord = false) {
    var timer1 = 1500;
    var textMess = "Thêm mới thành công";
    if (id > 0 ) {
        textMess = "Cập nhật thành công";
    }
    if (deleteRecord ==true) {
        textMess = "Xoá thành công";
        timer1 = 3000;
    }
    Swal.fire({
        position: "center",
        icon: "success",
        title: textMess,
        showConfirmButton: false,
        timer: timer1
    }).then((result) => {
        window.location.reload(true);
    });
}

(function (global, $) {
    'use strict';

    var OPT_OUT_ATTR = 'data-allow-duplicate-submit';
    var PENDING_ATTR = 'data-duplicate-submit-pending';
    var BUSY_ATTR = 'data-duplicate-submit-busy';
    var RELEASE_TOKEN_ATTR = 'data-duplicate-submit-release-token';
    var TRIGGER_WINDOW_MS = 1000;
    var FORM_RELEASE_MS = 3000;
    var REQUEST_RELEASE_MS = 800;
    var pendingTrigger = null;

    function isElement(node) {
        return !!node && node.nodeType === 1;
    }

    function hasOptOut(element) {
        if (!isElement(element)) {
            return false;
        }

        return element.getAttribute(OPT_OUT_ATTR) === 'true'
            || !!element.closest('[' + OPT_OUT_ATTR + '="true"]');
    }

    function isMutatingMethod(method) {
        var normalized = (method || 'GET').toString().toUpperCase();
        return normalized === 'POST' || normalized === 'PUT' || normalized === 'PATCH' || normalized === 'DELETE';
    }

    function getActionElement(target) {
        if (!isElement(target)) {
            return null;
        }

        return target.closest('button, input[type="submit"], input[type="button"], a.btn, a[onclick], [data-prevent-duplicate]');
    }

    function isBusy(element) {
        if (!isElement(element)) {
            return false;
        }

        return element.getAttribute(PENDING_ATTR) === 'true'
            || element.getAttribute(BUSY_ATTR) === 'true';
    }

    function clearBusyState(element) {
        if (!isElement(element)) {
            return;
        }

        element.removeAttribute(PENDING_ATTR);
        element.removeAttribute(BUSY_ATTR);
        element.removeAttribute('aria-busy');
        element.removeAttribute(RELEASE_TOKEN_ATTR);
    }

    function setPendingState(element) {
        if (!isElement(element) || hasOptOut(element)) {
            return;
        }

        element.setAttribute(PENDING_ATTR, 'true');
        element.setAttribute('aria-busy', 'true');
    }

    function setBusyState(element) {
        if (!isElement(element) || hasOptOut(element)) {
            return;
        }

        element.removeAttribute(PENDING_ATTR);
        element.setAttribute(BUSY_ATTR, 'true');
        element.setAttribute('aria-busy', 'true');
    }

    function scheduleRelease(element, delay) {
        if (!isElement(element)) {
            return;
        }

        var token = Date.now().toString() + Math.random().toString(36).slice(2);
        element.setAttribute(RELEASE_TOKEN_ATTR, token);

        setTimeout(function () {
            if (!isElement(element) || !element.isConnected) {
                return;
            }

            if (element.getAttribute(RELEASE_TOKEN_ATTR) !== token) {
                return;
            }

            clearBusyState(element);
        }, delay);
    }

    function setPendingTrigger(element) {
        if (!isElement(element) || hasOptOut(element)) {
            pendingTrigger = null;
            return;
        }

        pendingTrigger = {
            element: element,
            expiresAt: Date.now() + TRIGGER_WINDOW_MS
        };
    }

    function consumePendingTrigger() {
        if (!pendingTrigger) {
            return null;
        }

        if (pendingTrigger.expiresAt < Date.now()) {
            pendingTrigger = null;
            return null;
        }

        var element = pendingTrigger.element;
        pendingTrigger = null;

        if (!isElement(element) || !element.isConnected || hasOptOut(element)) {
            return null;
        }

        return element;
    }

    function getRelatedForm(element) {
        if (!isElement(element)) {
            return null;
        }

        return element.closest('form');
    }

    function preventDuplicateInteraction(event) {
        event.preventDefault();
        event.stopPropagation();
        if (typeof event.stopImmediatePropagation === 'function') {
            event.stopImmediatePropagation();
        }
    }

    function onClickCapture(event) {
        var actionElement = getActionElement(event.target);
        if (!actionElement || hasOptOut(actionElement)) {
            return;
        }

        var relatedForm = getRelatedForm(actionElement);
        if (isBusy(actionElement) || isBusy(relatedForm)) {
            preventDuplicateInteraction(event);
            return;
        }

        setPendingTrigger(actionElement);
    }

    function onSubmit(event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement) || hasOptOut(form)) {
            return;
        }

        if (isBusy(form)) {
            preventDuplicateInteraction(event);
            return;
        }

        var method = form.getAttribute('method') || 'GET';
        if (!isMutatingMethod(method)) {
            return;
        }

        var submitter = event.submitter && isElement(event.submitter) ? event.submitter : null;
        pendingTrigger = null;
        setPendingState(form);
        if (submitter) {
            setPendingState(submitter);
        }

        setTimeout(function () {
            if (!form.isConnected) {
                return;
            }

            if (event.defaultPrevented) {
                clearBusyState(form);
                if (submitter) {
                    clearBusyState(submitter);
                }
                return;
            }

            setBusyState(form);
            if (submitter) {
                setBusyState(submitter);
                scheduleRelease(submitter, FORM_RELEASE_MS);
            }

            scheduleRelease(form, FORM_RELEASE_MS);
        }, 0);
    }

    function markTriggerBusy() {
        var trigger = consumePendingTrigger();
        if (!trigger) {
            return null;
        }

        setBusyState(trigger);
        return trigger;
    }

    function resolveFetchMethod(input, init) {
        if (init && init.method) {
            return init.method;
        }

        if (input && typeof input === 'object' && input.method) {
            return input.method;
        }

        return 'GET';
    }

    function wrapFetch() {
        if (typeof global.fetch !== 'function') {
            return;
        }

        var originalFetch = global.fetch.bind(global);
        global.fetch = function (input, init) {
            var method = resolveFetchMethod(input, init);
            var trigger = null;

            if (isMutatingMethod(method)) {
                trigger = markTriggerBusy();
            }

            var request = originalFetch(input, init);
            if (!trigger) {
                return request;
            }

            return Promise.resolve(request).finally(function () {
                scheduleRelease(trigger, REQUEST_RELEASE_MS);
            });
        };
    }

    function normalizeAjaxOptions(urlOrOptions, settings) {
        if (typeof urlOrOptions === 'string') {
            var nextSettings = settings || {};
            return { url: urlOrOptions, ...nextSettings };
        }

        return { ...(urlOrOptions || {}) };
    }

    function wrapAjax() {
        if (!$ || typeof $.ajax !== 'function') {
            return;
        }

        var originalAjax = $.ajax.bind($);
        $.ajax = function (urlOrOptions, settings) {
            var ajaxOptions = normalizeAjaxOptions(urlOrOptions, settings);
            var method = ajaxOptions.type || ajaxOptions.method || 'GET';
            var trigger = null;

            if (isMutatingMethod(method)) {
                trigger = markTriggerBusy();
            }

            var jqXhr = originalAjax(ajaxOptions);
            if (trigger && jqXhr && typeof jqXhr.always === 'function') {
                jqXhr.always(function () {
                    scheduleRelease(trigger, REQUEST_RELEASE_MS);
                });
            } else if (trigger) {
                scheduleRelease(trigger, REQUEST_RELEASE_MS);
            }

            return jqXhr;
        };
    }

    function submitFormWithGuard(form) {
        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        if (isBusy(form)) {
            return;
        }

        if (typeof form.requestSubmit === 'function') {
            form.requestSubmit();
            return;
        }

        setBusyState(form);
        scheduleRelease(form, FORM_RELEASE_MS);
        form.submit();
    }

    document.addEventListener('click', onClickCapture, true);
    document.addEventListener('submit', onSubmit, false);
    wrapFetch();
    wrapAjax();

    global.DuplicateSubmitGuard = {
        submitForm: submitFormWithGuard,
        clearState: clearBusyState
    };
})(window, window.jQuery);
