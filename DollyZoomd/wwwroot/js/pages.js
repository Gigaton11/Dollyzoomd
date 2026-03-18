import * as Api    from "./api.js";
import * as Auth   from "./auth.js";
import { showToast }  from "./toast.js";
import { createShowCard, createWatchlistEntry, createFavoriteCard } from "./components.js";
import { navigate } from "./router.js";
import * as Carousel from "./carousel.js";

const app = () => document.getElementById("app");

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
    header.innerHTML = `
        <div class="profile-avatar">${initial}</div>
        <div>
            <div class="profile-name">@${profile.username}</div>
            <div class="profile-since">Member since ${new Date(profile.memberSinceUtc).toLocaleDateString("en-GB", { year: "numeric", month: "long" })}</div>
        </div>`;
    container.appendChild(header);

    const sum = profile.watchlistSummary ?? {};
    const statDefs = [
        { label: "Total",         value: sum.total       ?? 0 },
        { label: "Watching",      value: sum.watching    ?? 0 },
        { label: "Completed",     value: sum.completed   ?? 0 },
        { label: "Plan to Watch", value: sum.planToWatch ?? 0 },
        { label: "Dropped",       value: sum.dropped     ?? 0 },
    ];

    const statsGrid = document.createElement("div");
    statsGrid.className = "stats-grid fade-in";
    statDefs.forEach(({ label, value }) => {
        const tile = document.createElement("div");
        tile.className = "stat-tile";
        tile.innerHTML = `<div class="stat-value">${value}</div><div class="stat-label">${label}</div>`;
        statsGrid.appendChild(tile);
    });
    container.appendChild(statsGrid);

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
        watchlistSection.appendChild(watchlistHeading);

        const sortedEntries = Array.isArray(watchlistEntries)
            ? [...watchlistEntries].sort((a, b) => new Date(b.updatedAtUtc).getTime() - new Date(a.updatedAtUtc).getTime())
            : [];

        if (!sortedEntries.length) {
            watchlistSection.appendChild(emptyState("📋", "Your Watchlist is empty. Search for a show to add one."));
        } else {
            const watchlistGrid = document.createElement("div");
            watchlistGrid.className = "profile-watchlist-grid";

            sortedEntries.forEach((entry) => {
                watchlistGrid.appendChild(createWatchlistEntry(entry));
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
        ? { onAddWatchlist: handleHomeAddWatchlist, onAddFavorite: handleHomeAddFavorite }
        : {};

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
    const tvMazeShowId = Number(show?.tvMazeId ?? show?.showId ?? show?.id);
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
                const card = createShowCard(show, Auth.isAuthenticated()
                    ? {
                        onAddWatchlist: addToWatchlist,
                        onAddFavorite: addToFavorites,
                    }
                    : {});
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
