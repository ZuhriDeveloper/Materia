(function () {
    var KEY = 'materia_sidebar';

    window.toggleSidebar = function () {
        var collapsed = document.documentElement.classList.toggle('sidebar-collapsed');
        try { localStorage.setItem(KEY, collapsed ? '1' : '0'); } catch (_) { }
    };
})();
