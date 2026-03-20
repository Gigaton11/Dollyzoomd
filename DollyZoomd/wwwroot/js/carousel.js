import { createShowCard } from "./components.js?v=20260320d";

/**
 * Creates a carousel section with title, navigation buttons, and horizontal scrolling card list.
 * Supports both guest (no action buttons) and authenticated (with watchlist/favorite buttons) modes.
 * 
 * @param {string} title - Section title (e.g., "Popular Right Now")
 * @param {Array<ShowSearchItemDto>} shows - List of shows to display
 * @param {Object} options - Configuration object
 * @param {Function} options.onAddWatchlist - Callback when user clicks "+ Watchlist" button
 * @param {Function} options.onAddFavorite - Callback when user clicks "★ Fav" button
 * @param {Function} options.onOpenShow - Callback when user clicks a show card
 * @returns {HTMLElement} Carousel section element
 */
export function createCarouselSection(title, shows, { onAddWatchlist, onAddFavorite, onOpenShow } = {}) {
    const section = document.createElement("section");
    section.className = "carousel-section fade-in";

    /* section header */
    const header = document.createElement("div");
    header.className = "carousel-header";

    const heading = document.createElement("h2");
    heading.className = "section-title";
    heading.textContent = title;
    header.appendChild(heading);

    section.appendChild(header);

    const shell = document.createElement("div");
    shell.className = "carousel-shell";

    const prevBtn = document.createElement("button");
    prevBtn.className = "carousel-edge-btn carousel-edge-btn-left";
    prevBtn.setAttribute("aria-label", "Scroll left");
    prevBtn.innerHTML = `<svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="2">
        <polyline points="13 4 7 10 13 16"></polyline>
    </svg>`;

    const nextBtn = document.createElement("button");
    nextBtn.className = "carousel-edge-btn carousel-edge-btn-right";
    nextBtn.setAttribute("aria-label", "Scroll right");
    nextBtn.innerHTML = `<svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="2">
        <polyline points="7 4 13 10 7 16"></polyline>
    </svg>`;

    /* carousel container */
    const carouselWrap = document.createElement("div");
    carouselWrap.className = "carousel-wrap";

    const carousel = document.createElement("div");
    carousel.className = "carousel";
    carousel.setAttribute("role", "region");
    carousel.setAttribute("aria-label", title);

    /* populate carousel with show cards */
    shows.forEach((show) => {
        const cardContainer = document.createElement("div");
        cardContainer.className = "carousel-item";
        const card = createShowCard(show, { onAddWatchlist, onAddFavorite, onOpenShow });
        cardContainer.appendChild(card);
        carousel.appendChild(cardContainer);
    });

    carouselWrap.appendChild(carousel);
    shell.appendChild(prevBtn);
    shell.appendChild(carouselWrap);
    shell.appendChild(nextBtn);
    section.appendChild(shell);

    /* wire up navigation buttons */
    const getScrollAmount = () => {
        const first = carousel.querySelector(".carousel-item");
        if (!first) return 300;

        const itemWidth = first.getBoundingClientRect().width;
        const styles = window.getComputedStyle(carousel);
        const gap = Number.parseFloat(styles.gap || styles.columnGap || "0") || 0;
        return Math.max((itemWidth + gap) * 3, carousel.clientWidth * 0.85);
    };

    const updateButtonState = () => {
        const maxScroll = carousel.scrollWidth - carousel.clientWidth;
        const hasOverflow = maxScroll > 6;

        if (!hasOverflow) {
            prevBtn.disabled = true;
            nextBtn.disabled = true;
            prevBtn.style.visibility = "hidden";
            nextBtn.style.visibility = "hidden";
            return;
        }

        prevBtn.style.visibility = "visible";
        nextBtn.style.visibility = "visible";
        prevBtn.disabled = carousel.scrollLeft <= 2;
        nextBtn.disabled = carousel.scrollLeft >= maxScroll - 2;
    };

    prevBtn.onclick = () => {
        carousel.scrollBy({ left: -getScrollAmount(), behavior: "smooth" });
    };
    nextBtn.onclick = () => {
        carousel.scrollBy({ left: getScrollAmount(), behavior: "smooth" });
    };

    carousel.addEventListener("scroll", updateButtonState, { passive: true });
    window.addEventListener("resize", updateButtonState);
    requestAnimationFrame(updateButtonState);

    return section;
}
