/**
 * Performance Optimizer
 * crmHuman - HR Management System
 * Version: 1.0.0
 */

(function() {
    'use strict';
    
    /**
     * 1. Debounce function for search/filter inputs
     */
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
    
    /**
     * 2. Throttle function for scroll events
     */
    function throttle(func, limit) {
        let inThrottle;
        return function(...args) {
            if (!inThrottle) {
                func.apply(this, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    }
    
    /**
     * 3. Lazy load images
     */
    function initLazyLoading() {
        const images = document.querySelectorAll('img[loading="lazy"]');
        
        if ('IntersectionObserver' in window) {
            const imageObserver = new IntersectionObserver((entries, observer) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const img = entry.target;
                        img.classList.add('loaded');
                        observer.unobserve(img);
                    }
                });
            });
            
            images.forEach(img => imageObserver.observe(img));
        } else {
            // Fallback for older browsers
            images.forEach(img => img.classList.add('loaded'));
        }
    }
    
    /**
     * 4. Optimize AJAX requests
     */
    const optimizedAjax = {
        cache: new Map(),
        
        async get(url, options = {}) {
            // Check cache first
            const cacheKey = url + JSON.stringify(options);
            if (this.cache.has(cacheKey) && options.cache !== false) {
                return this.cache.get(cacheKey);
            }
            
            try {
                const response = await fetch(url, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                        ...options.headers
                    },
                    ...options
                });
                
                if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
                
                const data = await response.json();
                
                // Cache if requested
                if (options.cache !== false) {
                    this.cache.set(cacheKey, data);
                }
                
                return data;
            } catch (error) {
                console.error('AJAX Error:', error);
                throw error;
            }
        },
        
        async post(url, data, options = {}) {
            try {
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        ...options.headers
                    },
                    body: JSON.stringify(data),
                    ...options
                });
                
                if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
                
                return await response.json();
            } catch (error) {
                console.error('AJAX Error:', error);
                throw error;
            }
        },
        
        clearCache() {
            this.cache.clear();
        }
    };
    
    /**
     * 5. Form validation optimization
     */
    function optimizeFormValidation() {
        const forms = document.querySelectorAll('form');
        
        forms.forEach(form => {
            // Debounce validation
            const inputs = form.querySelectorAll('input, textarea, select');
            
            inputs.forEach(input => {
                const validateInput = debounce(() => {
                    // Remove hasError class if valid
                    if (input.checkValidity()) {
                        input.closest('.form-group')?.classList.remove('hasError');
                    }
                }, 300);
                
                input.addEventListener('input', validateInput);
            });
        });
    }
    
    /**
     * 6. Optimize scroll performance
     */
    function optimizeScroll() {
        const backToTopButton = document.querySelector('.back-to-top');
        
        if (backToTopButton) {
            const toggleBackToTop = throttle(() => {
                const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
                backToTopButton.classList.toggle('active', scrollTop > 100);
            }, 100);
            
            window.addEventListener('scroll', toggleBackToTop, { passive: true });
        }
    }
    
    /**
     * 7. Optimize table rendering (for large datasets)
     */
    function virtualizeTable(tableSelector, rowHeight = 50) {
        const table = document.querySelector(tableSelector);
        if (!table) return;
        
        const tbody = table.querySelector('tbody');
        const rows = Array.from(tbody.querySelectorAll('tr'));
        
        if (rows.length < 50) return; // Only virtualize for large tables
        
        // Implement virtual scrolling
        const container = table.parentElement;
        const viewportHeight = container.clientHeight;
        const visibleRows = Math.ceil(viewportHeight / rowHeight) + 5; // Buffer
        
        let scrollTop = 0;
        
        function updateVisibleRows() {
            const startIndex = Math.floor(scrollTop / rowHeight);
            const endIndex = Math.min(startIndex + visibleRows, rows.length);
            
            rows.forEach((row, index) => {
                if (index >= startIndex && index < endIndex) {
                    row.style.display = '';
                } else {
                    row.style.display = 'none';
                }
            });
        }
        
        container.addEventListener('scroll', throttle(() => {
            scrollTop = container.scrollTop;
            updateVisibleRows();
        }, 16), { passive: true });
        
        updateVisibleRows();
    }
    
    /**
     * 8. Performance monitoring
     */
    function monitorPerformance() {
        if ('performance' in window && 'PerformanceObserver' in window) {
            // Monitor long tasks
            try {
                const observer = new PerformanceObserver((list) => {
                    for (const entry of list.getEntries()) {
                        if (entry.duration > 50) {
                            console.warn('Long task detected:', {
                                duration: entry.duration,
                                name: entry.name
                            });
                        }
                    }
                });
                
                observer.observe({ entryTypes: ['longtask'] });
            } catch (e) {
                // PerformanceObserver not supported
            }
            
            // Log Core Web Vitals
            window.addEventListener('load', () => {
                const perfData = performance.timing;
                const metrics = {
                    'DNS Lookup': perfData.domainLookupEnd - perfData.domainLookupStart,
                    'TCP Connection': perfData.connectEnd - perfData.connectStart,
                    'Request Time': perfData.responseEnd - perfData.requestStart,
                    'DOM Processing': perfData.domComplete - perfData.domLoading,
                    'Total Load Time': perfData.loadEventEnd - perfData.navigationStart
                };
                
                console.table(metrics);
            });
        }
    }
    
    /**
     * Initialize all optimizations
     */
    function init() {
        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => {
                initLazyLoading();
                optimizeFormValidation();
                optimizeScroll();
                monitorPerformance();
            });
        } else {
            initLazyLoading();
            optimizeFormValidation();
            optimizeScroll();
            monitorPerformance();
        }
    }
    
    // Auto-initialize
    init();
    
    // Export utilities
    window.PerformanceOptimizer = {
        debounce,
        throttle,
        optimizedAjax,
        virtualizeTable,
        initLazyLoading,
        optimizeFormValidation
    };
    
})();

