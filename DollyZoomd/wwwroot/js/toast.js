let _timer = null;

export function showToast(message, type = "info") {
    const el = document.getElementById("toast");
    if (!el) return;
    el.textContent = message;
    el.className = `toast show ${type}`;
    clearTimeout(_timer);
    _timer = setTimeout(() => { el.className = "toast"; }, 3200);
}
