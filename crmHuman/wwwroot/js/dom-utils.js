(function (global, $) {
    'use strict';

    const ensureElement = (selectorOrElement) =>
        typeof selectorOrElement === 'string'
            ? document.getElementById(selectorOrElement)
            : selectorOrElement || null;

    const dispatchChange = (element) => {
        const target = ensureElement(element);
        if (!target) {
            return;
        }

        if (typeof target.onchange === 'function') {
            target.onchange();
            return;
        }

        target.dispatchEvent(new Event('change'));
    };

    const triggerJQueryChange = (selector) => {
        if (!$ || !selector) {
            return;
        }

        const target = $(selector);
        if (target.length) {
            target.trigger('change');
        }
    };

    const DomUtils = {
        get: (id) => document.getElementById(id),

        createOption: (value, text) => {
            const option = document.createElement('option');
            option.value = value;
            option.textContent = text;
            return option;
        },

        clearChildren: (element) => {
            const target = ensureElement(element);
            if (target) {
                target.innerHTML = '';
            }
        },

        populateSelect: (select, items, config = {}) => {
            const target = ensureElement(select);
            if (!target) {
                return;
            }

            const {
                textKey = 'text',
                valueKey = 'value',
                defaultOption,
                onOption
            } = config;

            DomUtils.clearChildren(target);

            if (defaultOption) {
                target.appendChild(DomUtils.createOption(defaultOption.value, defaultOption.text));
            }

            (items || []).forEach((item) => {
                const option = DomUtils.createOption(item[valueKey], item[textKey]);
                if (onOption) {
                    onOption(option, item);
                }
                target.appendChild(option);
            });
        },

        dispatchChange,

        triggerChange: (selectorOrElement) => {
            if (typeof selectorOrElement === 'string' && selectorOrElement.startsWith('#')) {
                triggerJQueryChange(selectorOrElement);
                return;
            }
            dispatchChange(selectorOrElement);
        },

        setDisplay: (selectorOrElement, visible) => {
            const target = ensureElement(selectorOrElement);
            if (target) {
                target.style.display = visible ? 'block' : 'none';
            }
        }
    };

    global.DomUtils = DomUtils;
})(window, window.jQuery);

