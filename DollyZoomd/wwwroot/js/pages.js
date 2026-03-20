import * as Api    from "./api.js?v=20260320d";
import * as Auth   from "./auth.js?v=20260320d";
import { showToast }  from "./toast.js?v=20260320d";
import { createShowCard, createWatchlistEntry, createFavoriteCard } from "./components.js?v=20260320d";
import { navigate } from "./router.js?v=20260320d";
import * as Carousel from "./carousel.js?v=20260320d";

const app = () => document.getElementById("app");
const IMAGE_PLACEHOLDER_URL = `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(
    "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 400 225'><rect width='400' height='225' fill='#2c3440'/><text x='50%' y='50%' dominant-baseline='middle' text-anchor='middle' fill='#7b8d9a' font-family='Inter,Segoe UI,sans-serif' font-size='22'>No Image</text></svg>"
)}`;

function clear() {
    const el = app();
    el.innerHTML = "";
    el.className = "page-content";
    return el;
}

function emptyState(icon, text) {
    const div = document.createElement("div");
    div.className = "empty-state";
    div.innerHTML = `<div class="empty-icon">${icon}</div><p>${text}</p>`;
    return div;
}

function skeletonGrid(count = 8) {
    const grid = document.createElement("div");
    grid.className = "show-grid";
    for (let i = 0; i < count; i++) {
        const card = document.createElement("div");
        card.className = "card";
        card.innerHTML = `<div class="skeleton skeleton-poster"></div>
            <div class="card-body" style="gap:0.4rem">
                <div class="skeleton skeleton-line" style="width:85%"></div>
                <div class="skeleton skeleton-line short"></div>
            </div>`;
        grid.appendChild(card);
    }
    return grid;
}

function setLoadingState(container, message = "Loading…") {
    container.innerHTML = `<div style="color:var(--text-muted);font-size:0.85rem;">${message}</div>`;
}

function applyImageFallback(img) {
    img.addEventListener("error", () => {
        if (img.dataset.fallbackApplied === "1") {
            return;
        }

        img.dataset.fallbackApplied = "1";
        img.src = IMAGE_PLACEHOLDER_URL;
    });
}

function getShowId(show) {
    const showId = Number(show?.tvMazeId ?? show?.showId ?? show?.id);
    return Number.isInteger(showId) && showId > 0 ? showId : null;
}

function openShowDetails(show) {
    const showId = getShowId(show);
    if (!showId) {
        showToast("Unable to open details for this show right now.", "error");
        return;
    }

    navigate("show-details", { params: { id: showId } });
}

function formatDisplayDate(value) {
    if (!value || typeof value !== "string") {
        return "TBA";
    }

    const parts = value.split("-").map((part) => Number(part));
    if (parts.length !== 3 || parts.some((part) => !Number.isInteger(part))) {
        return "TBA";
    }

    const [year, month, day] = parts;
    const parsed = new Date(Date.UTC(year, month - 1, day));
    return parsed.toLocaleDateString("en-GB", {
        day: "2-digit",
        month: "short",
        year: "numeric",
        timeZone: "UTC",
    });
}

function formatEpisodeCode(season, number) {
    const safeSeason = Number.isInteger(season) && season > 0 ? season : 0;
    const safeNumber = Number.isInteger(number) && number > 0 ? number : 0;
    return `S${String(safeSeason).padStart(2, "0")}E${String(safeNumber).padStart(2, "0")}`;
}

function stripHtmlToText(html) {
    if (!html) {
        return "";
    }

    const temp = document.createElement("div");
    temp.innerHTML = html;
    return (temp.textContent || temp.innerText || "").trim();
}

function toShortSummary(html, maxLength = 240) {
    const plain = stripHtmlToText(html);
    if (!plain) {
        return "No summary available.";
    }

    if (plain.length <= maxLength) {
        return plain;
    }

    return `${plain.slice(0, maxLength - 1).trimEnd()}…`;
}

function goBackToPreviousRoute() {
    if (window.history.length > 1) {
        window.history.back();
        return;
    }

    navigate("home", { force: true });
}

function wirePasswordPeekButtons(form) {
    const toggleButtons = form.querySelectorAll("button[data-password-toggle]");

    toggleButtons.forEach((toggleButton) => {
        const targetName = toggleButton.getAttribute("data-password-toggle");
        if (!targetName) return;

        const input = form.querySelector(`input[name="${targetName}"]`);
        if (!input) return;

        toggleButton.addEventListener("click", () => {
            const reveal = input.type === "password";
            input.type = reveal ? "text" : "password";
            toggleButton.textContent = reveal ? "Hide" : "Show";
            toggleButton.setAttribute("aria-label", reveal ? "Hide password" : "Show password");
            toggleButton.setAttribute("aria-pressed", String(reveal));
            input.focus({ preventScroll: true });

            if (typeof input.setSelectionRange === "function") {
                const cursor = input.value.length;
                input.setSelectionRange(cursor, cursor);
            }
        });
    });
}

function renderProfileView(profile, container, { enableOwnActions = true, watchlistEntries = [] } = {}) {
    container.innerHTML = "";

    const isOwn = enableOwnActions && Auth.isAuthenticated() && Auth.getUsername()?.toLowerCase() === profile.username?.toLowerCase();

    const header = document.createElement("div");
    header.className = "profile-header fade-in";
    const initial = (profile.username?.[0] ?? "?").toUpperCase();
    header.innerHTML = `<div class="profile-avatar">${initial}</div>`;

    const headerMeta = document.createElement("div");
    headerMeta.className = "profile-header-meta";

    const nameRow = document.createElement("div");
    nameRow.className = "profile-name-row";

    const profileName = document.createElement("div");
    profileName.className = "profile-name";
    profileName.textContent = `@${profile.username}`;
    nameRow.appendChild(profileName);

    if (isOwn) {
        const watchlistBtn = document.createElement("button");
        watchlistBtn.type = "button";
        watchlistBtn.className = "btn btn-ghost btn-sm profile-watchlist-btn";
        watchlistBtn.textContent = "Watchlist";
        watchlistBtn.onclick = () => navigate("diary", { force: true });
        nameRow.appendChild(watchlistBtn);
    }

    const profileSince = document.createElement("div");
    profileSince.className = "profile-since";
    profileSince.textContent = `Member since ${new Date(profile.memberSinceUtc).toLocaleDateString("en-GB", { year: "numeric", month: "long" })}`;

    headerMeta.appendChild(nameRow);
    headerMeta.appendChild(profileSince);
    header.appendChild(headerMeta);
    container.appendChild(header);

    const sum = profile.watchlistSummary ?? {};
    const statDefs = [
        { label: "Total",         value: sum.total       ?? 0 },
        { label: "Watching",      value: sum.watching    ?? 0 },
        { label: "Completed",     value: sum.completed   ?? 0 },
        { label: "Plan to Watch", value: sum.planToWatch ?? 0 },
        { label: "Dropped",       value: sum.dropped     ?? 0 },
    ];

    if (profile.favorites?.length) {
        const favSection = document.createElement("div");
        const eyebrowDiv = document.createElement("div");
        eyebrowDiv.innerHTML = `<p class="section-eyebrow">Favourite</p><h3>Tv Series</h3>`;
        eyebrowDiv.style.marginBottom = "1rem";
        favSection.appendChild(eyebrowDiv);

        const favGrid = document.createElement("div");
        favGrid.className = "favorites-grid";

        profile.favorites.forEach(fav => {
            const card = createFavoriteCard(fav, {
                onOpenShow: () => openShowDetails({
                    showId: fav.showId,
                    name: fav.showName,
                }),
                readonly: !isOwn,
                onRemove: isOwn ? async (showId) => {
                    try {
                        await Api.removeFavorite(showId);
                        showToast("Removed from favorites.", "success");
                        const [updatedProfile, updatedWatchlist] = await Promise.all([
                            Api.getProfile(profile.username),
                            Api.getWatchlist(),
                        ]);
                        renderProfileView(updatedProfile, container, {
                            enableOwnActions: true,
                            watchlistEntries: Array.isArray(updatedWatchlist) ? updatedWatchlist : [],
                        });
                    } catch (err) {
                        showToast(err.message, "error");
                    }
                } : undefined,
            });
            favGrid.appendChild(card);
        });

        favSection.appendChild(favGrid);
        container.appendChild(favSection);
    } else {
        const noFavs = document.createElement("div");
        noFavs.innerHTML = `<p class="section-eyebrow">Favourites</p>`;
        noFavs.style.marginBottom = "0.75rem";
        container.appendChild(noFavs);
        container.appendChild(emptyState("★", isOwn
            ? "Add up to 6 all-time favourite shows via Search."
            : "No favourites added yet."));
    }

    if (isOwn) {
        const watchlistSection = document.createElement("div");
        watchlistSection.className = "profile-watchlist-section fade-in";

        const watchlistHeading = document.createElement("div");
        watchlistHeading.className = "profile-watchlist-heading";
        watchlistHeading.innerHTML = `<p class="section-eyebrow">Watchlist</p><h3>TV Series</h3>`;

        const viewWatchlistBtn = document.createElement("button");
        viewWatchlistBtn.type = "button";
        viewWatchlistBtn.className = "btn btn-ghost btn-sm";
        viewWatchlistBtn.textContent = "View Watchlist";
        viewWatchlistBtn.onclick = () => navigate("diary", { force: true });
        watchlistHeading.appendChild(viewWatchlistBtn);

        watchlistSection.appendChild(watchlistHeading);

        const sortedEntries = Array.isArray(watchlistEntries)
            ? [...watchlistEntries].sort((a, b) => new Date(b.updatedAtUtc).getTime() - new Date(a.updatedAtUtc).getTime())
            : [];

        if (!sortedEntries.length) {
            watchlistSection.appendChild(emptyState("📋", "Your Watchlist is empty. Search for a show to add one."));
        } else {
            const watchlistGrid = document.createElement("div");
            watchlistGrid.className = "profile-watchlist-grid";

            sortedEntries.slice(0, 6).forEach((entry) => {
                watchlistGrid.appendChild(createWatchlistEntry(entry, {
                    onOpenShow: () => openShowDetails({
                        showId: entry.showId,
                        name: entry.showName,
                    }),
                }));
            });

            watchlistSection.appendChild(watchlistGrid);
        }

        container.appendChild(watchlistSection);
    }
}

/* ────────────────────────────────────────────────────────── Home ── */
export function renderHome() {
    const root = clear();
    const page = document.createElement("div");
    page.className = "home-page fade-in";

    const hero = buildAuthHero({
        isAuthenticated: Auth.isAuthenticated(),
        username: Auth.getUsername(),
    });
    page.appendChild(hero);

    const popularMount = document.createElement("section");
    popularMount.className = "carousel-section";
    popularMount.appendChild(emptyState("⏳", "Loading Popular Right Now…"));
    page.appendChild(popularMount);

    const allTimeMount = document.createElement("section");
    allTimeMount.className = "carousel-section";
    allTimeMount.appendChild(emptyState("⏳", "Loading All-Time Greats…"));
    page.appendChild(allTimeMount);

    root.appendChild(page);

    const cardActions = Auth.isAuthenticated()
        ? { onAddWatchlist: handleHomeAddWatchlist, onAddFavorite: handleHomeAddFavorite, onOpenShow: openShowDetails }
        : { onOpenShow: openShowDetails };

    void loadDiscoverSection(popularMount, "Popular Right Now", () => Api.getDiscoverPopular(20, 0), cardActions);
    void loadDiscoverSection(allTimeMount, "All-Time Greats", () => Api.getDiscoverAllTimeGreats(20, 0), cardActions);
}

export function renderLogin() {
    const root = clear();
    const page = document.createElement("div");
    page.className = "auth-page fade-in";

    page.appendChild(buildAuthHero());
    page.appendChild(buildHomeAuthCard());

    root.appendChild(page);
}

function buildAuthHero({ isAuthenticated = false, username = "" } = {}) {
    const hero = document.createElement("div");
    hero.className = "auth-hero";

    if (isAuthenticated) {
        const normalizedUsername = String(username ?? "").trim().replace(/^@+/, "") || "member";

        hero.classList.add("auth-hero-returning");

        const eyebrow = document.createElement("p");
        eyebrow.className = "section-eyebrow";
        eyebrow.textContent = "To Binge or Not to Binge";

        const heading = document.createElement("h1");
        heading.className = "auth-hero-returning-title";
        heading.append(document.createTextNode("Welcome back "));

        const profileLink = document.createElement("a");
        profileLink.href = "#profile";
        profileLink.className = "auth-hero-user-link";
        profileLink.textContent = `@${normalizedUsername}`;
        profileLink.onclick = (event) => {
            event.preventDefault();
            navigate("profile", { force: true });
        };

        heading.appendChild(profileLink);
        heading.append(document.createTextNode("."));

        const copy = document.createElement("p");
        copy.textContent = "Jump back update your Watchlist and keep your TV story moving.";

        hero.appendChild(eyebrow);
        hero.appendChild(heading);
        hero.appendChild(copy);

        return hero;
    }

    hero.innerHTML = `
        <p class="section-eyebrow">TV Tracker</p>
        <h1>Your shows<br><em>Your story</em></h1>
        <p>Track every series you've watched. Rate them and share your taste with the world</p>
        <div class="auth-features">
            <p>Login with your account to</p>
            <div class="auth-feature"><div class="auth-feature-dot"></div>Track Watching, Completed & Dropped Shows</div>
            <div class="auth-feature"><div class="auth-feature-dot"></div>Rate on a 1–10 scale</div>
            <div class="auth-feature"><div class="auth-feature-dot"></div>Showcase up to 6 all-time favourites</div>
        </div>`;
    return hero;
}

function buildHomeAuthCard() {
    const card = document.createElement("div");
    card.className = "auth-card";

    const tabs = document.createElement("div");
    tabs.className = "auth-tabs";

    const loginTab = document.createElement("button");
    loginTab.className = "auth-tab active";
    loginTab.textContent = "Login";

    const registerTab = document.createElement("button");
    registerTab.className = "auth-tab";
    registerTab.textContent = "Register";

    tabs.appendChild(loginTab);
    tabs.appendChild(registerTab);
    card.appendChild(tabs);

    const formWrap = document.createElement("div");

    function buildLoginForm() {
        formWrap.innerHTML = "";
        const form = document.createElement("form");
        form.className = "auth-form";
        form.innerHTML = `
            <div class="input-group">
                <label class="input-label">Username or Email</label>
                <input class="input-field" type="text" name="identifier" required placeholder="username or you@example.com" autocomplete="username">
            </div>
            <div class="input-group">
                <label class="input-label">Password</label>
                <div class="password-field-wrap">
                    <input class="input-field" type="password" name="password" required minlength="8" placeholder="••••••••" autocomplete="current-password">
                    <button class="password-toggle" type="button" data-password-toggle="password" aria-label="Show password" aria-pressed="false">Show</button>
                </div>
            </div>
            <button type="submit" class="btn btn-primary btn-full">Login</button>`;

        wirePasswordPeekButtons(form);

        form.onsubmit = async (event) => {
            event.preventDefault();
            const btn = form.querySelector("button[type=submit]");
            const data = Object.fromEntries(new FormData(form));
            btn.disabled = true;
            btn.textContent = "Logging in…";
            try {
                const res = await Api.login(data);
                Auth.setAuth(res);
                showToast(`Welcome back ${res.username}!`, "success");
                form.reset();
                navigate("profile");
            } catch (err) {
                showToast(err.message, "error");
                btn.disabled = false;
                btn.textContent = "Login";
            }
        };

        formWrap.appendChild(form);
    }

    function buildRegisterForm() {
        formWrap.innerHTML = "";
        const form = document.createElement("form");
        form.className = "auth-form";
        form.innerHTML = `
            <div class="input-group">
                <label class="input-label">Username</label>
                <input class="input-field" type="text" name="username" required placeholder="johndoe" autocomplete="username">
            </div>
            <div class="input-group">
                <label class="input-label">Email</label>
                <input class="input-field" type="email" name="email" required placeholder="you@example.com" autocomplete="email">
            </div>
            <div class="input-group">
                <label class="input-label">Password</label>
                <div class="password-field-wrap">
                    <input class="input-field" type="password" name="password" required minlength="8" placeholder="••••••••" autocomplete="new-password">
                    <button class="password-toggle" type="button" data-password-toggle="password" aria-label="Show password" aria-pressed="false">Show</button>
                </div>
            </div>
            <div class="input-group">
                <label class="input-label">Confirm Password</label>
                <div class="password-field-wrap">
                    <input class="input-field" type="password" name="confirmPassword" required minlength="8" placeholder="••••••••" autocomplete="new-password">
                    <button class="password-toggle" type="button" data-password-toggle="confirmPassword" aria-label="Show password" aria-pressed="false">Show</button>
                </div>
            </div>
            <button type="submit" class="btn btn-primary btn-full">Create Account</button>`;

        wirePasswordPeekButtons(form);

        form.onsubmit = async (event) => {
            event.preventDefault();
            const btn = form.querySelector("button[type=submit]");
            const data = Object.fromEntries(new FormData(form));

            if (data.password !== data.confirmPassword) {
                showToast("Passwords do not match.", "error");
                return;
            }

            btn.disabled = true;
            btn.textContent = "Creating account…";
            try {
                const res = await Api.register(data);
                Auth.setAuth(res);
                showToast(`Welcome, ${res.username}!`, "success");
                form.reset();
                navigate("profile");
            } catch (err) {
                showToast(err.message, "error");
                btn.disabled = false;
                btn.textContent = "Create Account";
            }
        };

        formWrap.appendChild(form);
    }

    loginTab.onclick = () => {
        loginTab.classList.add("active");
        registerTab.classList.remove("active");
        buildLoginForm();
    };

    registerTab.onclick = () => {
        registerTab.classList.add("active");
        loginTab.classList.remove("active");
        buildRegisterForm();
    };

    buildLoginForm();
    card.appendChild(formWrap);
    return card;
}

async function loadDiscoverSection(mount, title, loader, cardActions) {
    try {
        const shows = await loader();
        if (!shows?.length) {
            const section = document.createElement("section");
            section.className = "carousel-section fade-in";
            section.innerHTML = `<div class="carousel-header"><h2 class="section-title">${title}</h2></div>`;
            section.appendChild(emptyState("📺", "No shows are available right now."));
            mount.replaceWith(section);
            return;
        }

        const section = Carousel.createCarouselSection(title, shows, cardActions);
        mount.replaceWith(section);
    } catch (err) {
        const section = document.createElement("section");
        section.className = "carousel-section fade-in";
        section.innerHTML = `<div class="carousel-header"><h2 class="section-title">${title}</h2></div>`;
        section.appendChild(emptyState("⚠️", err.message || "Unable to load this section right now."));
        mount.replaceWith(section);
    }
}

function buildShowMutationRequest(show) {
    const tvMazeShowId = getShowId(show);
    const showName = String(show?.name ?? "").trim();

    if (!Number.isInteger(tvMazeShowId) || tvMazeShowId <= 0 || !showName) {
        throw new Error("Unable to add this show right now. Please try another result.");
    }

    const genres = Array.isArray(show?.genres)
        ? show.genres.filter((genre) => typeof genre === "string" && genre.trim().length > 0)
        : [];

    return {
        tvMazeShowId,
        showName,
        posterUrl: show.posterUrl ?? null,
        genresCsv: genres.length ? genres.join(", ") : null,
    };
}

async function handleHomeAddWatchlist(show) {
    try {
        await Api.addToWatchlist(buildShowMutationRequest(show));
        showToast(`Added "${show.name}" to your Watchlist list.`, "success");
    } catch (err) {
        showToast(err.message, "error");
    }
}

async function handleHomeAddFavorite(show) {
    try {
        await Api.addFavorite(buildShowMutationRequest(show));
        showToast(`Added "${show.name}" to favorites.`, "success");
    } catch (err) {
        showToast(err.message, "error");
    }
}

/* ────────────────────────────────────────────────────────── Search ── */
export function renderSearch() {
    const root = clear();
    root.innerHTML = `<div class="section-header"><div><p class="section-eyebrow">Discover</p><h2>Search</h2></div></div>`;

    const modeBar = document.createElement("div");
    modeBar.className = "filter-bar";
    root.appendChild(modeBar);

    const modeDescription = document.createElement("p");
    modeDescription.className = "mode-description";
    root.appendChild(modeDescription);

    const form = document.createElement("form");
    form.className = "search-form";

    const wrap = document.createElement("div");
    wrap.className = "search-bar-wrap";
    wrap.innerHTML = `
        <div class="search-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
            </svg>
        </div>
        <input class="search-bar" type="search" autocomplete="off">`;

    const submitBtn = document.createElement("button");
    submitBtn.type = "submit";
    submitBtn.className = "btn btn-primary";

    form.appendChild(wrap);
    form.appendChild(submitBtn);
    root.appendChild(form);

    const results = document.createElement("div");
    root.appendChild(results);

    const input = wrap.querySelector(".search-bar");
    let mode = "shows";
    let debounce = null;
    let lastQuery = "";
    let requestId = 0;

    const modes = [
        {
            key: "shows",
            label: "Shows",
            placeholder: "Search for a TV show…",
            button: "Search",
            description: "Search Watchlist by title. Show results update as you type.",
            emptyIcon: "🎬",
            emptyText: "Start typing to search for Watchlist.",
        },
        {
            key: "profiles",
            label: "Profiles",
            placeholder: "Enter an exact username…",
            button: "View Profile",
            description: "Switch to Profiles to preview a member page by exact username.",
            emptyIcon: "👤",
            emptyText: "Enter a username to view their public profile.",
        },
    ];

    function getModeConfig() {
        return modes.find((item) => item.key === mode) ?? modes[0];
    }

    function renderModeIntro() {
        const config = getModeConfig();
        results.innerHTML = "";
        results.appendChild(emptyState(config.emptyIcon, config.emptyText));
    }

    function syncModeUi() {
        const config = getModeConfig();
        input.placeholder = config.placeholder;
        submitBtn.textContent = config.button;
        modeDescription.textContent = config.description;
    }

    function setMode(nextMode) {
        if (mode === nextMode) return;
        mode = nextMode;
        clearTimeout(debounce);
        lastQuery = "";
        syncModeUi();
        renderModeIntro();
        input.focus();
    }

    async function addToWatchlist(show) {
        try {
            await Api.addToWatchlist(buildShowMutationRequest(show));
            showToast(`Added "${show.name}" to your Watchlist list.`, "success");
        } catch (err) {
            showToast(err.message, "error");
        }
    }

    async function addToFavorites(show) {
        try {
            await Api.addFavorite(buildShowMutationRequest(show));
            showToast(`Added "${show.name}" to favorites.`, "success");
        } catch (err) {
            showToast(err.message, "error");
        }
    }

    async function searchShows(query) {
        const q = query.trim();
        if (!q) {
            lastQuery = "";
            renderModeIntro();
            return;
        }

        const myRequest = ++requestId;
        lastQuery = q;

        results.innerHTML = "";
        results.appendChild(skeletonGrid(8));

        try {
            const items = await Api.searchShows(q);
            if (myRequest !== requestId || mode !== "shows") return;

            results.innerHTML = "";
            if (!items?.length) {
                results.appendChild(emptyState("🔍", "No Watchlist found. Try another title."));
                return;
            }

            const grid = document.createElement("div");
            grid.className = "show-grid";

            items.forEach((show) => {
                const cardOptions = Auth.isAuthenticated()
                    ? {
                        onAddWatchlist: addToWatchlist,
                        onAddFavorite: addToFavorites,
                        onOpenShow: openShowDetails,
                    }
                    : {
                        onOpenShow: openShowDetails,
                    };

                const card = createShowCard(show, cardOptions);
                grid.appendChild(card);
            });

            results.appendChild(grid);
        } catch (err) {
            if (myRequest !== requestId) return;
            results.innerHTML = "";
            results.appendChild(emptyState("⚠️", err.message || "Failed to search Watchlist."));
        }
    }

    async function searchProfiles(query) {
        const username = query.trim();
        if (!username) {
            lastQuery = "";
            renderModeIntro();
            return;
        }

        lastQuery = username;
        setLoadingState(results, "Loading profile…");

        try {
            const profile = await Api.getProfile(username);
            if (mode !== "profiles" || lastQuery !== username) return;
            renderProfileView(profile, results, { enableOwnActions: false });
        } catch (err) {
            results.innerHTML = "";
            if (err.status === 404) {
                results.appendChild(emptyState("👤", `No profile found for "${username}".`));
                return;
            }
            results.appendChild(emptyState("⚠️", err.message || "Failed to load profile."));
        }
    }

    modes.forEach((modeItem) => {
        const button = document.createElement("button");
        button.type = "button";
        button.className = `filter-btn${modeItem.key === mode ? " active" : ""}`;
        button.textContent = modeItem.label;
        button.onclick = () => {
            modeBar.querySelectorAll(".filter-btn").forEach((btn) => btn.classList.remove("active"));
            button.classList.add("active");
            setMode(modeItem.key);
        };
        modeBar.appendChild(button);
    });
    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        clearTimeout(debounce);

        if (mode === "shows") {
            await searchShows(input.value);
            return;
        }

        await searchProfiles(input.value);
    });

    input.addEventListener("input", () => {
        if (mode !== "shows") {
            if (!input.value.trim()) {
                renderModeIntro();
            }
            return;
        }

        clearTimeout(debounce);
        debounce = setTimeout(() => searchShows(input.value), 450);
    });

    syncModeUi();
    renderModeIntro();
    input.focus();
}

/* ────────────────────────────────────────────────────────── Diary ── */
export async function renderDiary() {
    const root = clear();
    root.innerHTML = `<div class="section-header"><div><p class="section-eyebrow">My List</p><h2>Watchlist</h2></div></div>`;

    if (!Auth.isAuthenticated()) {
        root.appendChild(emptyState("📋", "Login to view your Watchlist list."));
        return;
    }

    const filterBar = document.createElement("div");
    filterBar.className = "filter-bar";
    const filterOptions = [
        { label: "All",           value: null },
        { label: "Watching",      value: 1 },
        { label: "Completed",     value: 2 },
        { label: "Plan to Watch", value: 0 },
        { label: "Dropped",       value: 3 },
    ];
    let activeFilter = null;
    const filterBtns = [];

    const listWrap = document.createElement("div");
    root.appendChild(filterBar);
    root.appendChild(listWrap);

    async function loadDiary() {
        listWrap.innerHTML = "";
        listWrap.appendChild(skeletonGrid(12));

        try {
            let items = await Api.getWatchlist();
            listWrap.innerHTML = "";

            if (activeFilter !== null) {
                items = items.filter(e => e.status === activeFilter);
            }

            if (!items?.length) {
                const label = filterOptions.find(f => f.value === activeFilter)?.label ?? "All";
                listWrap.appendChild(emptyState("📺", activeFilter === null
                    ? "Your Watchlist list is empty. Search for a show to add one."
                    : `No series with status "${label}".`));
                return;
            }

            const list = document.createElement("div");
            list.className = "stack-list";

            items.forEach(entry => {
                const el = createWatchlistEntry(entry, {
                    onStatusChange: async (showId, status) => {
                        try {
                            await Api.updateStatus(showId, { status });
                            showToast("Status updated.", "success");
                            await loadDiary();
                        } catch (err) { showToast(err.message, "error"); }
                    },
                    onRate: async (showId, rating) => {
                        try {
                            await Api.rateShow(showId, { rating });
                            showToast("Rating saved.", "success");
                            await loadDiary();
                        } catch (err) { showToast(err.message, "error"); }
                    },
                    onRemove: async (showId) => {
                        try {
                            await Api.removeFromWatchlist(showId);
                            showToast("Removed from your Watchlist list.", "success");
                            await loadDiary();
                        } catch (err) { showToast(err.message, "error"); }
                    },
                });
                list.appendChild(el);
            });

            listWrap.appendChild(list);
        } catch (err) {
            listWrap.innerHTML = "";
            listWrap.appendChild(emptyState("⚠️", err.message));
        }
    }

    filterOptions.forEach(opt => {
        const btn = document.createElement("button");
        btn.className = "filter-btn" + (opt.value === activeFilter ? " active" : "");
        btn.textContent = opt.label;
        btn.onclick = () => {
            activeFilter = opt.value;
            filterBtns.forEach(b => b.classList.remove("active"));
            btn.classList.add("active");
            loadDiary();
        };
        filterBar.appendChild(btn);
        filterBtns.push(btn);
    });

    await loadDiary();
}

/* ────────────────────────────────────────────────────────── Profile ── */
export async function renderProfile() {
    const root = clear();

    const header = document.createElement("div");
    header.className = "section-header";
    header.innerHTML = `<div><p class="section-eyebrow">Account</p><h2>Profile</h2></div>`;
    root.appendChild(header);

    if (!Auth.isAuthenticated()) {
        root.appendChild(emptyState("👤", "Login to view your profile. Use Search in Profiles mode to look up other members."));
        return;
    }

    const username = String(Auth.getUsername() ?? "").trim().replace(/^@+/, "");
    if (!username) {
        root.appendChild(emptyState("👤", "Login to view your profile. Use Search in Profiles mode to look up other members."));
        return;
    }

    const profileView = document.createElement("div");
    root.appendChild(profileView);
    setLoadingState(profileView);

    try {
        const [profileResult, watchlistResult] = await Promise.allSettled([
            Api.getProfile(username),
            Api.getWatchlist(),
        ]);

        if (profileResult.status === "rejected") {
            throw profileResult.reason;
        }

        const profile = profileResult.value;
        const watchlistEntries = watchlistResult.status === "fulfilled" && Array.isArray(watchlistResult.value)
            ? watchlistResult.value
            : [];

        if (watchlistResult.status === "rejected") {
            showToast(watchlistResult.reason?.message ?? "Unable to load your Watchlist right now.", "error");
        }

        if (!Array.isArray(profile.favorites) || profile.favorites.length === 0) {
            try {
                const ownFavorites = await Api.getFavorites();
                if (Array.isArray(ownFavorites) && ownFavorites.length > 0) {
                    profile.favorites = ownFavorites;
                }
            } catch {
                // Keep profile response as-is if dedicated favorites fetch fails.
            }
        }

        renderProfileView(profile, profileView, {
            enableOwnActions: true,
            watchlistEntries,
        });
    } catch (err) {
        profileView.innerHTML = "";
        profileView.appendChild(emptyState("⚠️", err.message));
    }
}

/* ─────────────────────────────────────────────────── Show Details ── */
export async function renderShowDetails(params = {}) {
    const root = clear();
    root.classList.add("page-content-show-detail");

    const showId = Number(params?.id);
    if (!Number.isInteger(showId) || showId <= 0) {
        root.appendChild(emptyState("⚠️", "Invalid show link."));
        return;
    }

    setLoadingState(root, "Loading show details…");

    try {
        const details = await Api.getShowDetails(showId);
        root.innerHTML = "";
        root.appendChild(buildShowDetailsPage(details));
    } catch (err) {
        root.innerHTML = "";
        if (err.status === 404) {
            root.appendChild(emptyState("📺", "This show could not be found."));
            return;
        }

        root.appendChild(emptyState("⚠️", err.message || "Unable to load show details right now."));
    }
}

function buildShowDetailsPage(details) {
    const page = document.createElement("div");
    page.className = "show-detail-page fade-in";

    const hero = document.createElement("section");
    hero.className = "show-hero";

    const backdropImage = details.bannerUrl || details.posterUrl;
    if (backdropImage) {
        const backdrop = document.createElement("div");
        backdrop.className = "show-hero-backdrop";
        backdrop.style.backgroundImage = `url("${backdropImage}")`;
        hero.appendChild(backdrop);
    }

    const heroOverlay = document.createElement("div");
    heroOverlay.className = "show-hero-overlay";

    const navRow = document.createElement("div");
    navRow.className = "show-hero-nav";

    const backBtn = document.createElement("button");
    backBtn.className = "btn btn-ghost btn-sm show-back-btn";
    backBtn.textContent = "← Back";
    backBtn.onclick = () => goBackToPreviousRoute();
    navRow.appendChild(backBtn);

    const breadcrumb = document.createElement("div");
    breadcrumb.className = "show-breadcrumb";

    const homeLink = document.createElement("a");
    homeLink.href = "#home";
    homeLink.textContent = "Home";
    homeLink.onclick = (event) => {
        event.preventDefault();
        navigate("home");
    };

    const separator = document.createElement("span");
    separator.textContent = "/";

    const current = document.createElement("span");
    current.textContent = details.name || "Show";

    breadcrumb.appendChild(homeLink);
    breadcrumb.appendChild(separator);
    breadcrumb.appendChild(current);
    navRow.appendChild(breadcrumb);
    heroOverlay.appendChild(navRow);

    const heroContent = document.createElement("div");
    heroContent.className = "show-hero-content";

    const posterWrap = document.createElement("div");
    posterWrap.className = "show-hero-poster-wrap";
    const poster = document.createElement("img");
    poster.className = "show-hero-poster";
    poster.src = details.posterUrl || IMAGE_PLACEHOLDER_URL;
    poster.alt = `${details.name} poster`;
    poster.loading = "lazy";
    applyImageFallback(poster);
    posterWrap.appendChild(poster);
    heroContent.appendChild(posterWrap);

    const meta = document.createElement("div");
    meta.className = "show-hero-meta";

    const eyebrow = document.createElement("p");
    eyebrow.className = "section-eyebrow";
    eyebrow.textContent = "TV Series";
    meta.appendChild(eyebrow);

    const title = document.createElement("h1");
    title.className = "show-hero-title";
    title.textContent = details.name || "Untitled Show";
    meta.appendChild(title);

    const genreRow = document.createElement("div");
    genreRow.className = "show-hero-tags";
    if (Array.isArray(details.genres) && details.genres.length) {
        details.genres.forEach((genre) => {
            const tag = document.createElement("span");
            tag.className = "tag";
            tag.textContent = genre;
            genreRow.appendChild(tag);
        });
    } else {
        const tag = document.createElement("span");
        tag.className = "tag";
        tag.textContent = "Unknown Genre";
        genreRow.appendChild(tag);
    }
    meta.appendChild(genreRow);

    const rating = document.createElement("div");
    rating.className = "show-hero-rating";
    rating.textContent = details.averageRating
        ? `★ ${Number(details.averageRating).toFixed(1)}`
        : "★ N/A";
    meta.appendChild(rating);

    const statItems = [
        { label: "Network", value: details.networkName || "Unknown" },
        { label: "Status", value: details.status || "Unknown" },
        { label: "Premiered", value: formatDisplayDate(details.premieredOn) },
    ];

    if (details.endedOn) {
        statItems.push({ label: "Ended", value: formatDisplayDate(details.endedOn) });
    }

    const statGrid = document.createElement("div");
    statGrid.className = "show-hero-stats";
    statItems.forEach((item) => {
        const row = document.createElement("div");
        row.className = "show-hero-stat";

        const label = document.createElement("span");
        label.className = "show-hero-stat-label";
        label.textContent = item.label;

        const value = document.createElement("span");
        value.className = "show-hero-stat-value";
        value.textContent = item.value;

        row.appendChild(label);
        row.appendChild(value);
        statGrid.appendChild(row);
    });
    meta.appendChild(statGrid);

    heroContent.appendChild(meta);
    heroOverlay.appendChild(heroContent);
    hero.appendChild(heroOverlay);
    page.appendChild(hero);

    const body = document.createElement("section");
    body.className = "show-detail-body";

    const tabBar = document.createElement("div");
    tabBar.className = "show-tabs";
    tabBar.setAttribute("role", "tablist");

    const panelWrap = document.createElement("div");
    panelWrap.className = "show-tab-panels";

    let activateTab = () => {};

    const panels = {
        main: createMainTabPanel(details, {
            onViewEpisodes: () => activateTab("episodes"),
            onViewCast: () => activateTab("cast"),
        }),
        episodes: createEpisodesTabPanel(details),
        cast: createCastTabPanel(details),
    };

    const tabOrder = [
        { key: "main", label: "Overview" },
        { key: "episodes", label: "Episodes" },
        { key: "cast", label: "Cast" },
    ];

    const buttons = {};
    tabOrder.forEach((tab) => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "show-tab-btn";
        btn.dataset.tab = tab.key;
        btn.textContent = tab.label;
        btn.setAttribute("role", "tab");
        btn.onclick = () => activateTab(tab.key);
        tabBar.appendChild(btn);
        buttons[tab.key] = btn;
    });

    activateTab = (tabKey) => {
        const activeTab = tabOrder.some((tab) => tab.key === tabKey) ? tabKey : "main";

        tabOrder.forEach((tab) => {
            const active = tab.key === activeTab;
            buttons[tab.key].classList.toggle("active", active);
            buttons[tab.key].setAttribute("aria-selected", String(active));
        });

        panelWrap.innerHTML = "";
        panelWrap.appendChild(panels[activeTab]);
    };

    activateTab("main");

    body.appendChild(tabBar);
    body.appendChild(panelWrap);
    page.appendChild(body);

    return page;
}

function createMainTabPanel(details, { onViewEpisodes, onViewCast }) {
    const panel = document.createElement("div");
    panel.className = "show-tab-panel";

    const infoSection = document.createElement("section");
    infoSection.className = "show-section";
    infoSection.innerHTML = `<h2>Show Info</h2>`;

    const infoLayout = document.createElement("div");
    infoLayout.className = "show-info-layout";

    const posterWrap = document.createElement("div");
    posterWrap.className = "show-info-poster-wrap";
    const poster = document.createElement("img");
    poster.className = "show-info-poster";
    poster.src = details.posterUrl || IMAGE_PLACEHOLDER_URL;
    poster.alt = `${details.name} poster`;
    poster.loading = "lazy";
    applyImageFallback(poster);
    posterWrap.appendChild(poster);
    infoLayout.appendChild(posterWrap);

    const summary = document.createElement("div");
    summary.className = "show-summary";
    summary.innerHTML = details.summaryHtml?.trim()
        ? details.summaryHtml
        : "<p>No description is available for this show yet.</p>";
    infoLayout.appendChild(summary);
    infoSection.appendChild(infoLayout);

    panel.appendChild(infoSection);

    const previousSection = document.createElement("section");
    previousSection.className = "show-section";

    const previousHeader = document.createElement("div");
    previousHeader.className = "show-section-header";
    previousHeader.innerHTML = "<h2>Previous Episodes</h2>";

    const viewEpisodesBtn = document.createElement("button");
    viewEpisodesBtn.className = "btn btn-ghost btn-sm";
    viewEpisodesBtn.textContent = "View full episode list";
    viewEpisodesBtn.onclick = () => onViewEpisodes();
    previousHeader.appendChild(viewEpisodesBtn);
    previousSection.appendChild(previousHeader);

    const recentEpisodes = getRecentAiredEpisodes(details.episodes, 3);
    if (!recentEpisodes.length) {
        previousSection.appendChild(emptyState("📺", "No aired episodes are available yet."));
    } else {
        const list = document.createElement("div");
        list.className = "previous-episodes-list";
        recentEpisodes.forEach((episode) => list.appendChild(createPreviousEpisodeCard(episode)));
        previousSection.appendChild(list);
    }

    panel.appendChild(previousSection);

    const castSection = document.createElement("section");
    castSection.className = "show-section";

    const castHeader = document.createElement("div");
    castHeader.className = "show-section-header";
    castHeader.innerHTML = "<h2>Cast</h2>";

    const viewCastBtn = document.createElement("button");
    viewCastBtn.className = "btn btn-ghost btn-sm";
    viewCastBtn.textContent = "View full cast list";
    viewCastBtn.onclick = () => onViewCast();
    castHeader.appendChild(viewCastBtn);
    castSection.appendChild(castHeader);

    const previewCast = Array.isArray(details.cast) ? details.cast.slice(0, 8) : [];
    if (!previewCast.length) {
        castSection.appendChild(emptyState("🎭", "Cast information is unavailable."));
    } else {
        const castGrid = document.createElement("div");
        castGrid.className = "cast-grid";
        previewCast.forEach((member) => castGrid.appendChild(createCastCard(member)));
        castSection.appendChild(castGrid);
    }

    panel.appendChild(castSection);
    return panel;
}

function createEpisodesTabPanel(details) {
    const panel = document.createElement("div");
    panel.className = "show-tab-panel";

    const episodes = Array.isArray(details.episodes) ? [...details.episodes] : [];
    episodes.sort((a, b) => {
        const seasonDiff = (Number(a.season) || 0) - (Number(b.season) || 0);
        if (seasonDiff !== 0) {
            return seasonDiff;
        }

        return (Number(a.number) || 0) - (Number(b.number) || 0);
    });

    if (!episodes.length) {
        panel.appendChild(emptyState("📺", "No episodes were returned for this show."));
        return panel;
    }

    const grouped = new Map();
    episodes.forEach((episode) => {
        const season = Number.isInteger(episode.season) && episode.season > 0 ? episode.season : 0;
        if (!grouped.has(season)) {
            grouped.set(season, []);
        }
        grouped.get(season).push(episode);
    });

    const seasons = [...grouped.keys()].sort((a, b) => a - b);
    seasons.forEach((season) => {
        const section = document.createElement("section");
        section.className = "show-section season-section";

        const heading = document.createElement("h2");
        heading.textContent = season > 0 ? `Season ${season}` : "Specials";
        section.appendChild(heading);

        const list = document.createElement("div");
        list.className = "episode-list";

        grouped.get(season).forEach((episode) => {
            const row = document.createElement("details");
            row.className = "episode-row";

            const summary = document.createElement("summary");
            summary.className = "episode-row-head";

            const code = document.createElement("span");
            code.className = "episode-row-code";
            code.textContent = formatEpisodeCode(Number(episode.season), Number(episode.number));

            const title = document.createElement("span");
            title.className = "episode-row-title";
            title.textContent = episode.name || "Untitled Episode";

            const airDate = document.createElement("span");
            airDate.className = "episode-row-date";
            airDate.textContent = formatDisplayDate(episode.airDate);

            summary.appendChild(code);
            summary.appendChild(title);
            summary.appendChild(airDate);
            row.appendChild(summary);

            const body = document.createElement("div");
            body.className = "episode-row-body";
            body.textContent = toShortSummary(episode.summaryHtml);
            row.appendChild(body);

            list.appendChild(row);
        });

        section.appendChild(list);
        panel.appendChild(section);
    });

    return panel;
}

function createCastTabPanel(details) {
    const panel = document.createElement("div");
    panel.className = "show-tab-panel";

    const castMembers = Array.isArray(details.cast) ? details.cast : [];
    if (!castMembers.length) {
        panel.appendChild(emptyState("🎭", "No cast information is available for this show."));
        return panel;
    }

    const grid = document.createElement("div");
    grid.className = "cast-grid cast-grid-full";
    castMembers.forEach((member) => grid.appendChild(createCastCard(member)));
    panel.appendChild(grid);
    return panel;
}

function createCastCard(member) {
    const card = document.createElement("article");
    card.className = "cast-card";

    const image = document.createElement("img");
    image.className = "cast-photo";
    image.src = member.personImageUrl || IMAGE_PLACEHOLDER_URL;
    image.alt = member.personName || "Cast member";
    image.loading = "lazy";
    applyImageFallback(image);

    const body = document.createElement("div");
    body.className = "cast-card-body";

    const actor = document.createElement("div");
    actor.className = "cast-actor-name";
    actor.textContent = member.personName || "Unknown Actor";

    const character = document.createElement("div");
    character.className = "cast-character-name";
    character.textContent = member.characterName || "Unknown Character";

    body.appendChild(actor);
    body.appendChild(character);

    card.appendChild(image);
    card.appendChild(body);
    return card;
}

function createPreviousEpisodeCard(episode) {
    const card = document.createElement("article");
    card.className = "previous-episode-card";

    const thumb = document.createElement("img");
    thumb.className = "previous-episode-thumb";
    thumb.src = episode.thumbnailUrl || IMAGE_PLACEHOLDER_URL;
    thumb.alt = episode.name || "Episode thumbnail";
    thumb.loading = "lazy";
    applyImageFallback(thumb);

    const body = document.createElement("div");
    body.className = "previous-episode-body";

    const name = document.createElement("div");
    name.className = "previous-episode-name";
    name.textContent = episode.name || "Untitled Episode";

    const code = document.createElement("div");
    code.className = "previous-episode-code";
    code.textContent = formatEpisodeCode(Number(episode.season), Number(episode.number));

    const date = document.createElement("div");
    date.className = "previous-episode-date";
    date.textContent = formatDisplayDate(episode.airDate);

    body.appendChild(name);
    body.appendChild(code);
    body.appendChild(date);

    card.appendChild(thumb);
    card.appendChild(body);
    return card;
}

function getRecentAiredEpisodes(episodes, take = 3) {
    if (!Array.isArray(episodes) || !episodes.length) {
        return [];
    }

    const today = new Date().toISOString().slice(0, 10);

    return episodes
        .filter((episode) => typeof episode.airDate === "string" && episode.airDate <= today)
        .sort((a, b) => {
            const dateCompare = String(b.airDate).localeCompare(String(a.airDate));
            if (dateCompare !== 0) {
                return dateCompare;
            }

            const seasonDiff = (Number(b.season) || 0) - (Number(a.season) || 0);
            if (seasonDiff !== 0) {
                return seasonDiff;
            }

            return (Number(b.number) || 0) - (Number(a.number) || 0);
        })
        .slice(0, take);
}

