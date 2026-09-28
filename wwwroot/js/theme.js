(function () {
    var KEY = "kechap-theme";
    var root = document.documentElement;

    function readSaved() {
        try {
            return localStorage.getItem(KEY) === "dark";
        } catch (e) {
            return false;
        }
    }

    // Runs immediately (before the page is drawn), so there is no light-mode flash.
    root.classList.toggle("dark", readSaved());

    window.kechapTheme = {
        isDark: function () {
            return root.classList.contains("dark");
        },
        toggle: function () {
            var dark = !root.classList.contains("dark");
            root.classList.toggle("dark", dark);
            try {
                localStorage.setItem(KEY, dark ? "dark" : "light");
            } catch (e) { }
            return dark;
        }
    };
})();