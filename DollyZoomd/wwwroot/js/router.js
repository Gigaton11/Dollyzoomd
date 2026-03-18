const VALID_PAGES = ["home", "login", "search", "diary", "profile"];

let _handlers = {};

export function initRouter(handlers) {
    _handlers = handlers;
    window.addEventListener("hashchange", () => dispatch());
}

export function dispatch() {
    const page = currentPage();
    updateNavActive(page);
    const fn = _handlers[page];
    if (fn) fn();
}

export function navigate(page, { force = false } = {}) {
    const raw = String(page ?? "home").replace(/^#/, "").trim().toLowerCase();
    const target = VALID_PAGES.includes(raw) ? raw : "home";

    if (currentPage() === target) {
        if (force) dispatch();
        return;
    }

    location.hash = `#${target}`;
}

export function currentPage() {
    const hash = location.hash.replace(/^#/, "").trim().toLowerCase();
    return VALID_PAGES.includes(hash) ? hash : "home";
}

export function updateNavActive(page) {
    document.querySelectorAll(".nav-link[data-page]").forEach(link => {
        link.classList.toggle("active", link.dataset.page === page);
    });
}
