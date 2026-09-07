// Applies the stored theme before first paint so the page does not flash.
//
// This lives in its own file rather than inline in App.razor so that the
// Content-Security-Policy can forbid inline script entirely. It is loaded
// synchronously from <head>, so it still runs before the body is painted.
(function () {
    try {
        var stored = localStorage.getItem('dental-theme');
        if (stored === 'dark' || stored === 'light') {
            document.documentElement.setAttribute('data-theme', stored);
        }
    } catch (e) { /* storage unavailable: fall back to the system theme */ }
})();
