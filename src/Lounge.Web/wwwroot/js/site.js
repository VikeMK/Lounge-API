document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".utc-to-local").forEach(elem => {
        elem.innerText = new Date(elem.dataset.time).toLocaleString(undefined, { timeZoneName: "short" });
    });
});