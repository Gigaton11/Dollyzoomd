export const WATCH_STATUS = [
    { value: 0, label: "Plan to Watch" },
    { value: 1, label: "Watching"      },
    { value: 2, label: "Completed"     },
    { value: 3, label: "Dropped"       },
];

export function createStatusBadge(statusValue) {
    const info = WATCH_STATUS.find(s => s.value === statusValue);
    const span = document.createElement("span");
    span.className = `status-badge status-${statusValue}`;
    span.textContent = info ? info.label : "Unknown";
    return span;
}

export function createShowCard(show, { onAddWatchlist, onAddFavorite, onOpenShow } = {}) {
    const article = document.createElement("article");
    article.className = "card show-card fade-in";

    if (typeof onOpenShow === "function") {
        article.classList.add("show-card-clickable");
        article.setAttribute("role", "link");
        article.setAttribute("tabindex", "0");
        article.setAttribute("aria-label", `Open details for ${show.name}`);

        const openDetails = () => onOpenShow(show);

        article.addEventListener("click", (event) => {
            if (event.target instanceof Element && event.target.closest("button")) {
                return;
            }
            openDetails();
        });

        article.addEventListener("keydown", (event) => {
            if (event.key !== "Enter" && event.key !== " ") {
                return;
            }

            event.preventDefault();
            openDetails();
        });
    }

    /* poster */
    const wrap = document.createElement("div");
    wrap.className = "poster-wrap";

    if (show.posterUrl) {
        const img = document.createElement("img");
        img.src = show.posterUrl;
        img.alt = show.name;
        img.loading = "lazy";
        wrap.appendChild(img);
    } else {
        const fb = document.createElement("div");
        fb.className = "poster-fallback";
        fb.textContent = show.name;
        wrap.appendChild(fb);
    }

    if (show.averageRating) {
        const badge = document.createElement("div");
        badge.className = "rating-badge";
        badge.textContent = `★ ${Number(show.averageRating).toFixed(1)}`;
        wrap.appendChild(badge);
    }

    article.appendChild(wrap);

    /* body */
    const body = document.createElement("div");
    body.className = "card-body";

    const title = document.createElement("div");
    title.className = "card-title";
    title.textContent = show.name;
    body.appendChild(title);

    if (show.premieredOn) {
        const meta = document.createElement("div");
        meta.className = "card-meta";
        meta.textContent = new Date(show.premieredOn).getFullYear();
        body.appendChild(meta);
    }

    if (show.genres?.length) {
        const row = document.createElement("div");
        row.className = "tag-row";
        show.genres.slice(0, 3).forEach(g => {
            const t = document.createElement("span");
            t.className = "tag";
            t.textContent = g;
            row.appendChild(t);
        });
        body.appendChild(row);
    }

    /* actions */
    const actions = document.createElement("div");
    actions.style.cssText = "display:flex;gap:0.35rem;flex-wrap:wrap;margin-top:auto;padding-top:0.35rem;";

    if (onAddWatchlist) {
        const btn = document.createElement("button");
        btn.className = "btn btn-ghost btn-sm";
        btn.textContent = "+ Watchlist";
        btn.onclick = (event) => {
            event.stopPropagation();
            onAddWatchlist(show);
        };
        actions.appendChild(btn);
    }

    if (onAddFavorite) {
        const btn = document.createElement("button");
        btn.className = "btn btn-ghost btn-sm";
        btn.textContent = "★ Fav";
        btn.onclick = (event) => {
            event.stopPropagation();
            onAddFavorite(show);
        };
        actions.appendChild(btn);
    }

    if (actions.children.length) body.appendChild(actions);
    article.appendChild(body);
    return article;
}

export function createWatchlistEntry(entry, { onStatusChange, onRate, onRemove, onOpenShow } = {}) {
    const item = document.createElement("div");
    item.className = "card wl-entry";

    if (typeof onOpenShow === "function") {
        item.classList.add("show-card-clickable");
        item.setAttribute("role", "link");
        item.setAttribute("tabindex", "0");
        item.setAttribute("aria-label", `Open details for ${entry.showName}`);

        const openDetails = () => onOpenShow(entry);

        item.addEventListener("click", (event) => {
            if (event.target instanceof Element && event.target.closest("button, input, select, textarea, a, label")) {
                return;
            }

            openDetails();
        });

        item.addEventListener("keydown", (event) => {
            if (event.key !== "Enter" && event.key !== " ") {
                return;
            }

            if (event.target instanceof Element && event.target.closest("button, input, select, textarea, a, label")) {
                return;
            }

            event.preventDefault();
            openDetails();
        });
    }

    /* thumbnail */
    if (entry.posterUrl) {
        const img = document.createElement("img");
        img.src = entry.posterUrl;
        img.alt = entry.showName;
        img.className = "wl-thumb";
        img.loading = "lazy";
        item.appendChild(img);
    } else {
        const fb = document.createElement("div");
        fb.className = "wl-thumb-fallback";
        fb.textContent = entry.showName;
        item.appendChild(fb);
    }

    /* body */
    const body = document.createElement("div");
    body.className = "wl-body";

    const title = document.createElement("div");
    title.className = "wl-title";
    title.textContent = entry.showName;
    body.appendChild(title);

    /* badges row */
    const badges = document.createElement("div");
    badges.className = "wl-badges";
    badges.appendChild(createStatusBadge(entry.status));

    if (entry.rating != null) {
        const rb = document.createElement("span");
        rb.className = "rating-badge";
        rb.style.cssText = "position:static;font-size:0.72rem;line-height:1;padding:0.2rem 0.45rem;border-radius:4px;";
        rb.textContent = `★ ${entry.rating}`;
        badges.appendChild(rb);
    }
    body.appendChild(badges);

    if (entry.genres?.length) {
        const row = document.createElement("div");
        row.className = "tag-row";
        entry.genres.slice(0, 3).forEach(g => {
            const t = document.createElement("span");
            t.className = "tag";
            t.textContent = g;
            row.appendChild(t);
        });
        body.appendChild(row);
    }

    /* controls */
    const hasControls = typeof onStatusChange === "function"
        || typeof onRate === "function"
        || typeof onRemove === "function";

    if (hasControls) {
        const controls = document.createElement("div");
        controls.className = "wl-controls";

        if (onStatusChange) {
            const sel = document.createElement("select");
            sel.className = "input-field";
            sel.style.cssText = "font-size:0.78rem;padding:0.28rem 2rem 0.28rem 0.55rem;";
            WATCH_STATUS.forEach(s => {
                const opt = document.createElement("option");
                opt.value = s.value;
                opt.textContent = s.label;
                if (s.value === entry.status) opt.selected = true;
                sel.appendChild(opt);
            });

            const updateBtn = document.createElement("button");
            updateBtn.className = "btn btn-ghost btn-sm";
            updateBtn.textContent = "Update";
            updateBtn.onclick = () => onStatusChange(entry.showId, Number(sel.value));

            controls.appendChild(sel);
            controls.appendChild(updateBtn);
        }

        if (onRate) {
            const rateInput = document.createElement("input");
            rateInput.type = "number";
            rateInput.min = "1";
            rateInput.max = "10";
            rateInput.step = "1";
            rateInput.placeholder = "1-10";
            rateInput.className = "input-field";
            rateInput.style.cssText = "font-size:0.78rem;padding:0.28rem 0.5rem;";
            if (entry.rating != null) rateInput.value = entry.rating;

            const rateBtn = document.createElement("button");
            rateBtn.className = "btn btn-ghost btn-sm";
            rateBtn.textContent = "Rate";
            rateBtn.onclick = () => {
                const val = Number(rateInput.value);
                if (val >= 1 && val <= 10) onRate(entry.showId, val);
            };

            controls.appendChild(rateInput);
            controls.appendChild(rateBtn);
        }

        if (onRemove) {
            const removeBtn = document.createElement("button");
            removeBtn.className = "btn btn-danger btn-sm";
            removeBtn.textContent = "Remove";
            removeBtn.onclick = () => onRemove(entry.showId);
            controls.appendChild(removeBtn);
        }

        body.appendChild(controls);
    }

    item.appendChild(body);
    return item;
}

export function createFavoriteCard(fav, { onRemove, readonly = false, onOpenShow } = {}) {
    const article = document.createElement("article");
    article.className = "card show-card fav-card fade-in";

    if (typeof onOpenShow === "function") {
        article.classList.add("show-card-clickable");
        article.setAttribute("role", "link");
        article.setAttribute("tabindex", "0");
        article.setAttribute("aria-label", `Open details for ${fav.showName}`);

        const openDetails = () => onOpenShow(fav);

        article.addEventListener("click", (event) => {
            if (event.target instanceof Element && event.target.closest("button")) {
                return;
            }
            openDetails();
        });

        article.addEventListener("keydown", (event) => {
            if (event.key !== "Enter" && event.key !== " ") {
                return;
            }

            if (event.target instanceof Element && event.target.closest("button")) {
                return;
            }

            event.preventDefault();
            openDetails();
        });
    }

    const wrap = document.createElement("div");
    wrap.className = "poster-wrap";

    if (fav.posterUrl) {
        const img = document.createElement("img");
        img.src = fav.posterUrl;
        img.alt = fav.showName;
        img.loading = "lazy";
        wrap.appendChild(img);
    } else {
        const fb = document.createElement("div");
        fb.className = "poster-fallback";
        fb.textContent = fav.showName;
        wrap.appendChild(fb);
    }

    if (fav.displayOrder != null) {
        const badge = document.createElement("div");
        badge.className = "order-badge";
        badge.textContent = `#${fav.displayOrder}`;
        wrap.appendChild(badge);
    }

    article.appendChild(wrap);

    const body = document.createElement("div");
    body.className = "card-body";

    const title = document.createElement("div");
    title.className = "card-title";
    title.textContent = fav.showName;
    body.appendChild(title);

    if (fav.genres?.length) {
        const row = document.createElement("div");
        row.className = "tag-row";
        fav.genres.slice(0, 3).forEach(g => {
            const t = document.createElement("span");
            t.className = "tag";
            t.textContent = g;
            row.appendChild(t);
        });
        body.appendChild(row);
    }

    if (!readonly && onRemove) {
        const btn = document.createElement("button");
        btn.className = "btn btn-danger btn-sm";
        btn.style.marginTop = "auto";
        btn.textContent = "Remove";
        btn.onclick = () => onRemove(fav.showId);
        body.appendChild(btn);
    }

    article.appendChild(body);
    return article;
}
