document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".utc-to-local").forEach(elem => {
        elem.innerText = new Date(elem.getAttribute("data-time")).toLocaleString(undefined, { timeZoneName: "short" });
    });
});