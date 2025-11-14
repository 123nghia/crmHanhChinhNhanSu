/**
 * Date Formatter Utility - Format dates to dd/MM/yyyy
 * Đảm bảo format date luôn là dd/MM/yyyy trên mọi trình duyệt (đặc biệt là Firefox)
 */
(function (global) {
    'use strict';

    const isFirefox = /Firefox/i.test(navigator.userAgent);
    const formattedElements = new WeakSet();

    /**
     * Format date string từ mm/dd/yyyy sang dd/MM/yyyy
     */
    function formatDateToDDMMYYYY(dateStr) {
        if (!dateStr || typeof dateStr !== 'string') {
            return dateStr;
        }

        const trimmed = dateStr.trim();
        const datePattern = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/;
        const match = trimmed.match(datePattern);
        
        if (!match) {
            return dateStr;
        }

        const first = parseInt(match[1], 10);
        const second = parseInt(match[2], 10);
        const year = match[3];

        // Nếu first > 12 hoặc second > 12, đã là dd/MM/yyyy
        if (first > 12 || second > 12) {
            return dateStr;
        }

        // Nếu cả hai đều <= 12
        // Firefox có thể hiển thị mm/dd/yyyy nếu parse date string
        // Heuristic: Nếu first > second, có thể Firefox đã format thành mm/dd/yyyy
        // Đảo lại thành dd/MM/yyyy
        if (first > second) {
            return `${String(second).padStart(2, '0')}/${String(first).padStart(2, '0')}/${year}`;
        }

        // Nếu first <= second, có thể đã là dd/MM/yyyy hoặc cả hai bằng nhau
        // Giữ nguyên
        return dateStr;
    }

    /**
     * Format tất cả date strings trong container
     */
    function formatDatesInContainer(container) {
        if (!container) {
            container = document;
        }

        // Format tất cả text content trong elements
        const selectors = 'td, th, span, div, p, a, label, li, dt, dd, strong, b, em, i, small, h1, h2, h3, h4, h5, h6';
        const elements = container.querySelectorAll 
            ? container.querySelectorAll(selectors) 
            : [];
        
        elements.forEach(element => {
            if (formattedElements.has(element)) {
                return;
            }
            
            const text = element.textContent;
            if (!text || !text.trim()) {
                return;
            }
            
            // Tìm tất cả date patterns
            const datePattern = /\b(\d{1,2}\/\d{1,2}\/\d{4})\b/g;
            const dates = [];
            let match;
            
            while ((match = datePattern.exec(text)) !== null) {
                dates.push(match[1]);
            }
            
            if (dates.length === 0) {
                return;
            }
            
            let newText = text;
            let hasChanges = false;
            
            dates.forEach(dateStr => {
                const formatted = formatDateToDDMMYYYY(dateStr);
                if (formatted !== dateStr) {
                    hasChanges = true;
                    // Escape special regex characters
                    const escaped = dateStr.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
                    newText = newText.replace(new RegExp(escaped, 'g'), formatted);
                }
            });
            
            if (hasChanges) {
                element.textContent = newText;
                formattedElements.add(element);
            }
        });

        // Format text nodes trực tiếp (cho trường hợp text node không có parent element)
        const walker = document.createTreeWalker(
            container,
            NodeFilter.SHOW_TEXT,
            {
                acceptNode: function(node) {
                    if (formattedElements.has(node)) {
                        return NodeFilter.FILTER_REJECT;
                    }
                    
                    const parent = node.parentElement;
                    if (!parent) {
                        return NodeFilter.FILTER_REJECT;
                    }
                    
                    const tagName = parent.tagName;
                    if (tagName === 'SCRIPT' || tagName === 'STYLE' || tagName === 'NOSCRIPT') {
                        return NodeFilter.FILTER_REJECT;
                    }
                    
                    if (tagName === 'INPUT' && parent.type === 'date') {
                        return NodeFilter.FILTER_REJECT;
                    }
                    
                    if (formattedElements.has(parent)) {
                        return NodeFilter.FILTER_REJECT;
                    }
                    
                    // Chỉ format nếu có date pattern
                    if (!node.textContent || !/\d{1,2}\/\d{1,2}\/\d{4}/.test(node.textContent)) {
                        return NodeFilter.FILTER_REJECT;
                    }
                    
                    return NodeFilter.FILTER_ACCEPT;
                }
            }
        );

        const textNodes = [];
        let node;
        while ((node = walker.nextNode())) {
            textNodes.push(node);
        }

        // Format từng text node
        textNodes.forEach(textNode => {
            const text = textNode.textContent;
            const datePattern = /\b(\d{1,2}\/\d{1,2}\/\d{4})\b/g;
            let newText = text;
            let hasChanges = false;

            newText = newText.replace(datePattern, function(match) {
                const formatted = formatDateToDDMMYYYY(match);
                if (formatted !== match) {
                    hasChanges = true;
                    return formatted;
                }
                return match;
            });

            if (hasChanges) {
                textNode.textContent = newText;
                formattedElements.add(textNode);
            }
        });
    }

    /**
     * Khởi tạo date formatter
     */
    function initDateFormatter() {
        const formatAllDates = function() {
            try {
                formatDatesInContainer(document);
            } catch (e) {
                console.warn('DateFormatter error:', e);
            }
        };

        // Format ngay khi DOM sẵn sàng
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', formatAllDates);
        } else {
            formatAllDates();
        }

        // Format khi có dynamic content
        if (typeof MutationObserver !== 'undefined') {
            const observer = new MutationObserver(function(mutations) {
                let needsFormat = false;
                mutations.forEach(function(mutation) {
                    if (mutation.addedNodes && mutation.addedNodes.length > 0) {
                        needsFormat = true;
                    }
                    // Format nếu text content thay đổi
                    if (mutation.type === 'characterData' || 
                        (mutation.target && /\d{1,2}\/\d{1,2}\/\d{4}/.test(mutation.target.textContent))) {
                        needsFormat = true;
                    }
                });
                if (needsFormat) {
                    setTimeout(formatAllDates, 0);
                }
            });

            const setupObserver = function() {
                if (document.body) {
                    observer.observe(document.body, {
                        childList: true,
                        subtree: true,
                        characterData: true
                    });
                }
            };

            if (document.body) {
                setupObserver();
            } else {
                document.addEventListener('DOMContentLoaded', setupObserver);
            }
        }

        // Format lại nhiều lần để đảm bảo (đặc biệt cho Firefox)
        if (isFirefox) {
            setTimeout(formatAllDates, 0);
            setTimeout(formatAllDates, 50);
            setTimeout(formatAllDates, 100);
            setTimeout(formatAllDates, 200);
            setTimeout(formatAllDates, 400);
            setTimeout(formatAllDates, 600);
            setTimeout(formatAllDates, 1000);
            setTimeout(formatAllDates, 2000);
        } else {
            setTimeout(formatAllDates, 100);
            setTimeout(formatAllDates, 500);
        }
    }

    // Export
    global.DateFormatter = {
        format: formatDateToDDMMYYYY,
        formatDatesInContainer: formatDatesInContainer,
        init: initDateFormatter
    };

    // Auto init
    if (typeof window !== 'undefined') {
        initDateFormatter();
    }

})(typeof window !== 'undefined' ? window : this);
