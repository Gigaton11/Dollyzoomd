const KEY = "dollyzoomd.auth";

let _auth = null;

export function loadAuth() {
    try {
        const raw = localStorage.getItem(KEY);
        if (!raw) return;
        const data = JSON.parse(raw);
        const expiry = new Date(data.expiresAtUtc);
        if (expiry > new Date()) {
            _auth = data;
        } else {
            localStorage.removeItem(KEY);
            _auth = null;
        }
    } catch {
        _auth = null;
    }
}

export function setAuth(data) {
    _auth = data;
    localStorage.setItem(KEY, JSON.stringify(data));
}

export function clearAuth() {
    _auth = null;
    localStorage.removeItem(KEY);
}

export function isAuthenticated() {
    if (!_auth) return false;
    return new Date(_auth.expiresAtUtc) > new Date();
}

export function getToken() {
    return _auth?.accessToken ?? null;
}

export function getUsername() {
    return _auth?.username ?? null;
}
