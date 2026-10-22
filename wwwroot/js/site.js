document.addEventListener('DOMContentLoaded', function () {
    // Mobile sidebar toggle (Host + Admin layouts)
    var toggle = document.getElementById('sidebarToggle');
    if (toggle) {
        toggle.addEventListener('click', function () {
            document.body.classList.toggle('sidebar-open');
        });
    }
});