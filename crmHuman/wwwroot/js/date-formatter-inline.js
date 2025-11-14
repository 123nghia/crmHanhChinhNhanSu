/**
 * Inline Date Formatter - Chạy ngay khi parse HTML để format date trước khi Firefox render
 * Đặt script này trong <head> hoặc ngay sau <body>
 */
(function() {
    'use strict';
    
    // Detect Firefox
    const isFirefox = /Firefox/i.test(navigator.userAgent);
    
    if (!isFirefox) {
        return; // Chỉ chạy trên Firefox
    }

    function ensureVietnameseLocale(locales) {
        if (!locales) {
            return 'vi-VN';
        }
        if (Array.isArray(locales) && locales.length === 0) {
            return 'vi-VN';
        }
        return locales;
    }

    // Override locale-sensitive APIs để luôn dùng dd/MM/yyyy trên Firefox
    (function enforceLocaleOverrides() {
        if (typeof Intl !== 'undefined' && typeof Intl.DateTimeFormat === 'function') {
            const NativeDateTimeFormat = Intl.DateTimeFormat;
            const PatchedDateTimeFormat = function(locales, options) {
                const normalizedLocales = ensureVietnameseLocale(locales);
                return new NativeDateTimeFormat(normalizedLocales, options);
            };
            PatchedDateTimeFormat.prototype = NativeDateTimeFormat.prototype;
            PatchedDateTimeFormat.supportedLocalesOf = NativeDateTimeFormat.supportedLocalesOf.bind(NativeDateTimeFormat);
            Intl.DateTimeFormat = PatchedDateTimeFormat;
        }

        if (typeof Date !== 'undefined' && typeof Date.prototype.toLocaleDateString === 'function') {
            const originalToLocaleDateString = Date.prototype.toLocaleDateString;
            Date.prototype.toLocaleDateString = function(locales, options) {
                const normalizedLocales = ensureVietnameseLocale(locales);
                return originalToLocaleDateString.call(this, normalizedLocales, options);
            };
        }
    })();
    
    // Function để format date string - Logic chắc chắn hơn
    function formatDateStr(str) {
        if (!str || typeof str !== 'string') return str;
        
        // Loại bỏ zero-width space và các ký tự ẩn
        let cleaned = str.replace(/\u200B/g, ''); // Zero-width space
        cleaned = cleaned.trim();
        
        // Tìm pattern date với nhiều format khác nhau
        const patterns = [
            /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/,  // dd/MM/yyyy hoặc mm/dd/yyyy
            /^(\d{1,2})\.(\d{1,2})\.(\d{4})$/,  // dd.MM.yyyy
            /^(\d{1,2})-(\d{1,2})-(\d{4})$/     // dd-MM-yyyy
        ];
        
        let match = null;
        let separator = '/';
        for (const pattern of patterns) {
            match = cleaned.match(pattern);
            if (match) {
                separator = cleaned.includes('.') ? '.' : (cleaned.includes('-') ? '-' : '/');
                break;
            }
        }
        
        if (!match) return str;
        
        const first = parseInt(match[1], 10);
        const second = parseInt(match[2], 10);
        const year = match[3];
        
        // Nếu một trong hai > 12, chắc chắn đã là dd/MM/yyyy
        if (first > 12 || second > 12) {
            return `${String(first).padStart(2, '0')}/${String(second).padStart(2, '0')}/${year}`;
        }
        
        // Nếu cả hai đều <= 12:
        // - Nếu first > second, có thể Firefox đã format thành mm/dd/yyyy → đảo lại
        // - Nếu first < second, có thể đã là dd/MM/yyyy → giữ nguyên
        // - Nếu first == second, giữ nguyên
        
        if (first > second) {
            // Đảo vị trí: mm/dd/yyyy → dd/MM/yyyy
            return `${String(second).padStart(2, '0')}/${String(first).padStart(2, '0')}/${year}`;
        }
        
        // first <= second, format thành dd/MM/yyyy
        return `${String(first).padStart(2, '0')}/${String(second).padStart(2, '0')}/${year}`;
    }
    
    // Observer để format khi DOM changes
    function setupFormatter() {
        function formatAll() {
            // Format text nodes
            const walker = document.createTreeWalker(
                document.body || document.documentElement,
                NodeFilter.SHOW_TEXT,
                {
                    acceptNode: function(node) {
                        const parent = node.parentElement;
                        if (!parent) return NodeFilter.FILTER_REJECT;
                        if (parent.tagName === 'SCRIPT' || 
                            parent.tagName === 'STYLE') {
                            return NodeFilter.FILTER_REJECT;
                        }
                        if (parent.tagName === 'INPUT' && parent.type === 'date') {
                            return NodeFilter.FILTER_REJECT;
                        }
                        return NodeFilter.FILTER_ACCEPT;
                    }
                }
            );
            
            const textNodes = [];
            let node;
            while ((node = walker.nextNode())) {
                const text = node.textContent;
                // Tìm cả date với slash và dấu chấm
                if (text && (/\d{1,2}\/\d{1,2}\/\d{4}/.test(text) || /\d{1,2}\.\d{1,2}\.\d{4}/.test(text))) {
                    textNodes.push(node);
                }
            }
            
            textNodes.forEach(textNode => {
                const text = textNode.textContent;
                // Tìm nhiều format date: dd/MM/yyyy, dd.MM.yyyy, dd-MM-yyyy, mm/dd/yyyy
                const datePattern = /\b(\d{1,2}[\/\.\-]\d{1,2}[\/\.\-]\d{4})\b/g;
                let newText = text;
                let hasChanges = false;
                
                newText = newText.replace(datePattern, function(match) {
                    // Nếu có dấu chấm, đây là format từ server (dd.MM.yyyy) → convert sang dd/MM/yyyy
                    if (match.includes('.')) {
                        hasChanges = true;
                        return match.replace(/\./g, '/');
                    }
                    // Nếu có slash, format lại để đảm bảo dd/MM/yyyy
                    const formatted = formatDateStr(match);
                    if (formatted !== match) {
                        hasChanges = true;
                        return formatted;
                    }
                    return match;
                });
                
                if (hasChanges) {
                    textNode.textContent = newText;
                }
            });
            
            // Format trong tất cả table cells để đảm bảo
            if (document.body) {
                const cells = document.body.querySelectorAll('td, th');
                cells.forEach(cell => {
                    const text = cell.textContent;
                    if (text && text.includes('.')) {
                        // Nếu có dấu chấm, convert sang slash
                        const newText = text.replace(/\b(\d{1,2})\.(\d{1,2})\.(\d{4})\b/g, '$1/$2/$3');
                        if (newText !== text) {
                            cell.textContent = newText;
                        }
                    }
                });
            }
        }
        
        // Format ngay nếu body đã có
        if (document.body) {
            formatAll();
        }
        
        // Format khi DOM ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', formatAll);
        }
        
        // Format lại nhiều lần sau delay để đảm bảo
        setTimeout(formatAll, 0);
        setTimeout(formatAll, 25);
        setTimeout(formatAll, 50);
        setTimeout(formatAll, 100);
        setTimeout(formatAll, 200);
        setTimeout(formatAll, 300);
        setTimeout(formatAll, 500);
        setTimeout(formatAll, 800);
        setTimeout(formatAll, 1200);
        
        // Observer cho dynamic content
        if (typeof MutationObserver !== 'undefined' && document.body) {
            const observer = new MutationObserver(function() {
                setTimeout(formatAll, 0);
            });
            observer.observe(document.body, {
                childList: true,
                subtree: true,
                characterData: true
            });
        }
    }
    
    // Chạy ngay khi script load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setupFormatter);
    } else {
        setupFormatter();
    }
})();

