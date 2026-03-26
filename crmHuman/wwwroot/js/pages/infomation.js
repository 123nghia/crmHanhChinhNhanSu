(function () {
    var defaults = {
        selector: '#mailSignature',
        formSelector: '#mailSignatureForm',
        tabSelector: 'button[data-bs-target="#profile-mail-signature"]',
        baseUrl: '/assets/vendor/tinymce',
        uploadUrl: '/Infomation?handler=UploadMailSignatureImage'
    };

    var options = Object.assign({}, defaults, window.mailSignatureEditorOptions || {});
    var editorInitialized = false;
    var editorInitializing = false;

    function getMailSignatureEditor() {
        if (!window.tinymce) {
            return null;
        }

        return window.tinymce.get('mailSignature');
    }

    function syncEditorValue() {
        if (window.tinymce) {
            window.tinymce.triggerSave();
        }
    }

    function getAntiForgeryToken() {
        var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    function uploadImage(file, progressHandler) {
        if (!options.uploadUrl) {
            return Promise.reject('Image upload URL is not configured.');
        }

        return new Promise(function (resolve, reject) {
            var xhr = new XMLHttpRequest();
            xhr.open('POST', options.uploadUrl, true);

            var token = getAntiForgeryToken();
            if (token) {
                xhr.setRequestHeader('RequestVerificationToken', token);
            }

            xhr.upload.onprogress = function (event) {
                if (typeof progressHandler === 'function' && event.lengthComputable) {
                    progressHandler((event.loaded / event.total) * 100);
                }
            };

            xhr.onload = function () {
                if (xhr.status < 200 || xhr.status >= 300) {
                    reject('Upload failed with status ' + xhr.status + '.');
                    return;
                }

                var result;
                try {
                    result = JSON.parse(xhr.responseText);
                } catch (error) {
                    reject('Invalid upload response.');
                    return;
                }

                if (!result || typeof result.location !== 'string' || result.location.length === 0) {
                    reject(result && result.error ? result.error : 'Upload response is missing image URL.');
                    return;
                }

                resolve(result.location);
            };

            xhr.onerror = function () {
                reject('Image upload failed.');
            };

            var formData = new FormData();
            formData.append('file', file, file.name || 'signature-image.png');
            if (token) {
                formData.append('__RequestVerificationToken', token);
            }

            xhr.send(formData);
        });
    }

    function initMailSignatureEditor() {
        if (editorInitialized || editorInitializing || !window.tinymce) {
            return;
        }

        var textarea = document.querySelector(options.selector);
        if (!textarea) {
            return;
        }

        if (getMailSignatureEditor()) {
            editorInitialized = true;
            return;
        }

        editorInitializing = true;

        window.tinymce.init({
            target: textarea,
            base_url: options.baseUrl,
            suffix: '.min',
            menubar: 'edit view insert format table tools',
            plugins: 'autolink autoresize code image link lists preview searchreplace table wordcount',
            toolbar: 'undo redo | blocks fontfamily fontsize | bold italic underline forecolor backcolor | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | link image table | removeformat code preview',
            toolbar_sticky: true,
            min_height: 320,
            autoresize_bottom_margin: 16,
            branding: false,
            promotion: false,
            browser_spellcheck: true,
            automatic_uploads: true,
            paste_data_images: true,
            contextmenu: 'undo redo | link image inserttable | cell row column deletetable',
            relative_urls: false,
            remove_script_host: false,
            convert_urls: false,
            image_title: true,
            image_advtab: true,
            image_uploadtab: true,
            file_picker_types: 'image',
            file_picker_callback: function (callback, value, meta) {
                if (meta.filetype !== 'image') {
                    return;
                }

                var picker = document.createElement('input');
                picker.setAttribute('type', 'file');
                picker.setAttribute('accept', 'image/*');

                picker.addEventListener('change', function () {
                    var file = picker.files && picker.files[0];
                    if (!file) {
                        return;
                    }

                    uploadImage(file).then(function (location) {
                        callback(location, { alt: file.name });
                    }).catch(function (error) {
                        window.alert(error);
                    });
                });

                picker.click();
            },
            images_upload_handler: function (blobInfo, progress) {
                var file = new File([blobInfo.blob()], blobInfo.filename(), { type: blobInfo.blob().type });
                return uploadImage(file, progress);
            },
            content_style: [
                'body { font-family: Arial, Helvetica, sans-serif; font-size: 14px; color: #212529; }',
                'p { margin: 0 0 8px; }',
                'table { border-collapse: collapse; }',
                'img { max-width: 100%; height: auto; }'
            ].join(' '),
            setup: function (editor) {
                editor.on('init', function () {
                    editorInitializing = false;
                    editorInitialized = true;
                });

                editor.on('change input undo redo', function () {
                    editor.save();
                });
            }
        }).catch(function (error) {
            editorInitializing = false;
            console.error('Unable to initialize mail signature editor.', error);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var form = document.querySelector(options.formSelector);
        if (form) {
            form.addEventListener('submit', syncEditorValue);
        }

        var tabButton = document.querySelector(options.tabSelector);
        if (tabButton) {
            tabButton.addEventListener('shown.bs.tab', function () {
                window.setTimeout(initMailSignatureEditor, 0);
            });

            if (tabButton.classList.contains('active')) {
                initMailSignatureEditor();
            }

            return;
        }

        initMailSignatureEditor();
    });
})();
