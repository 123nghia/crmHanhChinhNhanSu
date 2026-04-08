(function () {
    function getStickyTableOffset() {
        var header = document.getElementById("header");
        var pageTitle = document.querySelector(".pagetitle");

        var headerHeight = header ? Math.ceil(header.getBoundingClientRect().height) : 0;
        var pageTitleHeight = pageTitle ? Math.ceil(pageTitle.getBoundingClientRect().height) : 0;

        return headerHeight + pageTitleHeight;
    }

    function applyStickyTableHeaders() {
        var stickyOffset = getStickyTableOffset();
        document.documentElement.style.setProperty("--sticky-table-offset", stickyOffset + "px");

        var tables = document.querySelectorAll("table.table");
        tables.forEach(function (table) {
            if (!table || !table.tHead || table.id === "employeeGrid" || table.closest(".modal")) {
                return;
            }

            var wrapper = table.closest(".table-responsive");
            var canUseSticky = window.innerWidth >= 992;

            if (wrapper) {
                wrapper.classList.toggle("table-sticky-host", canUseSticky);
            }

            table.classList.toggle("table-sticky-enabled", canUseSticky);

            if (!canUseSticky) {
                var clearRows = Array.prototype.slice.call(table.tHead.rows || []);
                clearRows.forEach(function (row) {
                    Array.prototype.forEach.call(row.cells, function (cell) {
                        cell.style.top = "";
                    });
                });
                return;
            }

            var runningTop = stickyOffset;
            var rows = Array.prototype.slice.call(table.tHead.rows || []);

            rows.forEach(function (row) {
                var rowHeight = Math.ceil(row.getBoundingClientRect().height) || 0;
                Array.prototype.forEach.call(row.cells, function (cell) {
                    cell.style.top = runningTop + "px";
                });
                runningTop += rowHeight;
            });
        });
    }

    function queueStickyTableHeaders() {
        window.requestAnimationFrame(applyStickyTableHeaders);
    }

    function normalizePath(path) {
        if (!path) {
            return "/";
        }

        var normalized = path.split("?")[0].split("#")[0];
        if (normalized.length > 1 && normalized.endsWith("/")) {
            normalized = normalized.slice(0, -1);
        }

        return normalized || "/";
    }

    function scoreMatch(currentPath, candidatePath) {
        if (!candidatePath || candidatePath === "#") {
            return -1;
        }

        var normalizedCandidate = normalizePath(candidatePath);
        if (normalizedCandidate === "/") {
            return currentPath === "/" ? 1 : -1;
        }

        if (currentPath === normalizedCandidate) {
            return normalizedCandidate.length + 100;
        }

        if (currentPath.startsWith(normalizedCandidate + "/")) {
            return normalizedCandidate.length;
        }

        return -1;
    }

    function activateSidebarLink(link) {
        if (!link) {
            return;
        }

        link.classList.add("active");

        var navContent = link.closest(".nav-content");
        if (!navContent || !navContent.id) {
            return;
        }

        navContent.classList.add("show");

        var parentToggle = document.querySelector('.sidebar-nav [data-bs-target="#' + navContent.id + '"]');
        if (parentToggle) {
            parentToggle.classList.remove("collapsed");
            parentToggle.classList.add("active");
            parentToggle.setAttribute("aria-expanded", "true");
        }
    }

    function markSidebarActive() {
        var sidebar = document.getElementById("sidebar-nav");
        if (!sidebar) {
            return;
        }

        var currentPath = normalizePath(window.location.pathname || "/");
        var links = Array.prototype.slice.call(sidebar.querySelectorAll("a[href]"));
        var bestMatch = null;
        var bestScore = -1;

        links.forEach(function (link) {
            var href = link.getAttribute("href");
            var score = scoreMatch(currentPath, href);
            if (score > bestScore) {
                bestScore = score;
                bestMatch = link;
            }
        });

        activateSidebarLink(bestMatch);
    }

    document.addEventListener("DOMContentLoaded", function () {
        markSidebarActive();
        queueStickyTableHeaders();
        setTimeout(queueStickyTableHeaders, 120);
    });

    window.addEventListener("load", queueStickyTableHeaders);
    window.addEventListener("resize", queueStickyTableHeaders);
})();
