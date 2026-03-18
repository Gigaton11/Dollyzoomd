import * as Auth   from "./auth.js";
import { initRouter, dispatch, navigate, currentPage, updateNavActive } from "./router.js";
import { renderHome, renderLogin, renderSearch, renderDiary, renderProfile } from "./pages.js";
import { showToast } from "./toast.js";

function updateNavAuth() {
    const nav = document.getElementById("nav-auth");
    if (!nav) return;
    nav.innerHTML = "";

    if (Auth.isAuthenticated()) {
        const chip = document.createElement("a");
        chip.href = "#profile";
        chip.dataset.page = "profile";
        chip.className = "user-chip nav-link nav-link-auth";
        chip.textContent = `@${Auth.getUsername()}`;
        chip.onclick = (event) => {
            event.preventDefault();
            navigate("profile", { force: true });
        };
        nav.appendChild(chip);

        const logoutBtn = document.createElement("button");
        logoutBtn.className = "btn btn-link";
        logoutBtn.textContent = "Sign out";
        logoutBtn.onclick = () => {
            Auth.clearAuth();
            updateNavAuth();
            showToast("Signed out.", "info");
            navigate("home");
        };
        nav.appendChild(logoutBtn);
    } else {
        const link = document.createElement("a");
        link.href = "#login";
        link.className = "btn btn-ghost btn-sm";
        link.textContent = "Login";
        link.onclick = (event) => {
            event.preventDefault();
            navigate("login", { force: true });
        };
        nav.appendChild(link);
    }
}

Auth.loadAuth();

initRouter({
    home:    renderHome,
    login:   renderLogin,
    search:  renderSearch,
    diary:   renderDiary,
    profile: renderProfile,
});

window.addEventListener("hashchange", () => {
    updateNavAuth();
    updateNavActive(currentPage());
});

updateNavAuth();
updateNavActive(currentPage());
dispatch();
