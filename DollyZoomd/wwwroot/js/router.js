const VALID_PAGES = ["home", "login", "search", "diary", "profile", "show-details"];

let _handlers = {};

function parseRoute() {
    const hash = location.hash.replace(/^#/, "").trim().toLowerCase();

    if (!hash || hash === "home") {
        return { page: "home", params: {} };
    }

    const showMatch = hash.match(/^shows\/(\d+)$/);
    if (showMatch) {
        return {
            page: "show-details",
            params: { id: Number(showMatch[1]) },
        };
    }

    if (VALID_PAGES.includes(hash)) {
        return { page: hash, params: {} };
    }

    return { page: "home", params: {} };
}

export function initRouter(handlers) {
    _handlers = handlers;
    window.addEventListener("hashchange", () => dispatch());
}

export function dispatch() {
    const route = parseRoute();
    updateNavActive(route.page);
    const fn = _handlers[route.page];
    if (fn) fn(route.params);
}

export function navigate(page, { force = false, params = {} } = {}) {
    const raw = String(page ?? "home").replace(/^#/, "").trim().toLowerCase();

    let target = "home";
    if (raw === "show-details") {
        const id = Number(params.id);
        if (Number.isInteger(id) && id > 0) {
            target = `shows/${id}`;
        }
    } else if (VALID_PAGES.includes(raw)) {
        target = raw;
    }

    const currentHash = location.hash.replace(/^#/, "").trim().toLowerCase();

    if (currentHash === target) {
        if (force) dispatch();
        return;
    }

    location.hash = `#${target}`;
}

export function currentPage() {
    return parseRoute().page;
}

export function updateNavActive(page) {
    document.querySelectorAll(".nav-link[data-page]").forEach(link => {
        link.classList.toggle("active", link.dataset.page === page);
    });
}
