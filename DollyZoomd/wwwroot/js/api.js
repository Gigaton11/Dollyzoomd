import * as Auth from "./auth.js";

export class ApiError extends Error {
    constructor(message, status) {
        super(message);
        this.status = status;
        this.name = "ApiError";
    }
}

async function request(url, { method = "GET", body, auth = false } = {}) {
    const headers = { "Content-Type": "application/json" };
    if (auth) {
        const token = Auth.getToken();
        if (token) headers["Authorization"] = `Bearer ${token}`;
    }

    const init = { method, headers };
    if (body !== undefined) init.body = JSON.stringify(body);

    const res = await fetch(url, init);

    if (res.status === 401) {
        Auth.clearAuth();
    }

    if (!res.ok) {
        let msg = `HTTP ${res.status}`;
        try {
            const err = await res.json();
            msg = err.message ?? err.error ?? err.title ?? err.detail ?? JSON.stringify(err);
        } catch { /* ignore parse error */ }
        throw new ApiError(msg, res.status);
    }

    if (res.status === 204) return null;

    const payload = await res.text();
    if (!payload) return null;

    try {
        return JSON.parse(payload);
    } catch {
        return payload;
    }
}

/* ── Auth ── */
export const register  = (body) => request("/api/auth/register",  { method: "POST", body });
export const login     = (body) => request("/api/auth/login",     { method: "POST", body });

/* ── Shows ── */
export const searchShows = (q)  => request(`/api/shows/search?q=${encodeURIComponent(q)}`);

/* ── Watchlist ── */
export const getWatchlist        = ()            => request("/api/watchlist", { auth: true });
export const addToWatchlist      = (body)        => request("/api/watchlist", { method: "POST", body, auth: true });
export const updateStatus        = (showId, body)=> request(`/api/watchlist/${showId}/status`, { method: "PUT", body, auth: true });
export const rateShow            = (showId, body)=> request(`/api/watchlist/${showId}/rating`, { method: "PUT", body, auth: true });
export const removeFromWatchlist = (showId)      => request(`/api/watchlist/${showId}`,        { method: "DELETE", auth: true });

/* ── Favorites ── */
export const getFavorites   = ()       => request("/api/favorites",        { auth: true });
export const addFavorite    = (body)   => request("/api/favorites",        { method: "POST", body, auth: true });
export const removeFavorite = (showId) => request(`/api/favorites/${showId}`, { method: "DELETE", auth: true });

/* ── Profile ── */
export const getProfile = (username) => request(`/api/profile/${encodeURIComponent(username)}`);

/* ── Discover ── */
export const getDiscoverPopular = (take = 20, skip = 0) => request(`/api/discover/popular?take=${take}&skip=${skip}`);
export const getDiscoverAllTimeGreats = (take = 20, skip = 0) => request(`/api/discover/all-time-greats?take=${take}&skip=${skip}`);
