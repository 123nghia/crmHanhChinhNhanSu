(function () {
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

    document.addEventListener("DOMContentLoaded", markSidebarActive);
})();
