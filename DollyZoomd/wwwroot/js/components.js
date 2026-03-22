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

    const heading = document.createElement("div");
    heading.className = "wl-heading";

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

    heading.appendChild(badges);
    body.appendChild(heading);

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

            sel.onchange = () => onStatusChange(entry.showId, Number(sel.value));

            controls.appendChild(sel);
        }

        if (onRate) {
            const rateBtn = document.createElement("button");
            rateBtn.className = "btn btn-ghost btn-sm";
            rateBtn.textContent = entry.rating == null ? "Rate" : String(entry.rating);
            rateBtn.onclick = () => {
                const promptDefault = entry.rating == null ? "" : String(entry.rating);
                const value = window.prompt("Rate this show (1-10)", promptDefault);
                if (value == null) {
                    return;
                }

                const parsed = Number(value);
                if (!Number.isInteger(parsed) || parsed < 1 || parsed > 10) {
                    return;
                }

                onRate(entry.showId, parsed);
            };

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
