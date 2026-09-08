// Small helpers invoked from Blazor. Everything degrades gracefully when
// storage or the DOM API is unavailable.

window.dentalApp = {

    /** Applies and stores a theme. Pass "system" to clear the override. */
    setTheme: function (theme) {
        try {
            if (theme === 'system') {
                document.documentElement.removeAttribute('data-theme');
                localStorage.removeItem('dental-theme');
            } else {
                document.documentElement.setAttribute('data-theme', theme);
                localStorage.setItem('dental-theme', theme);
            }
        } catch (e) { /* private mode or blocked storage */ }
    },

    getTheme: function () {
        try {
            return localStorage.getItem('dental-theme') || 'system';
        } catch (e) {
            return 'system';
        }
    },

    print: function () {
        window.print();
    },

    focus: function (selector) {
        try {
            const el = document.querySelector(selector);
            if (el) el.focus();
        } catch (e) { /* selector did not match */ }
    },

    scrollIntoView: function (selector) {
        try {
            const el = document.querySelector(selector);
            if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
        } catch (e) { /* element not rendered */ }
    },

    confirm: function (message) {
        return window.confirm(message);
    },

    // ----------------------------------------------------------------- help
    //
    // The help panel lives in the layout, so it outlives every page. Blazor
    // cannot listen for a global key press on its own, so the handler is
    // installed here once and calls back into the component.

    _help: null,

    registerHelp: function (dotNetRef) {
        window.dentalApp._help = dotNetRef;
    },

    unregisterHelp: function () {
        window.dentalApp._help = null;
    },

    openHelp: function () {
        if (window.dentalApp._help) {
            window.dentalApp._help.invokeMethodAsync('Toggle');
        }
    },

    /** Offers a generated file to the user without a round trip to the server. */
    downloadText: function (fileName, mimeType, content) {
        try {
            const blob = new Blob([content], { type: mimeType });
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            setTimeout(() => URL.revokeObjectURL(url), 1000);
            return true;
        } catch (e) {
            return false;
        }
    }
};

// ---------------------------------------------------------------------------
// Progressive enhancement for the statically rendered identity forms.
//
// These pages render without an interactive circuit, so their behaviour cannot
// come from Blazor event handlers. Delegated listeners on the document give
// them a show/hide password control and duplicate-submit protection while
// leaving the forms fully usable with scripting unavailable.
// ---------------------------------------------------------------------------

(function () {
    'use strict';

    var SHOW = 'Show password';
    var HIDE = 'Hide password';

    var EYE = '<path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7-10-7-10-7Z"/><circle cx="12" cy="12" r="3"/>';
    var EYE_OFF = '<path d="M10.6 5.1A9.9 9.9 0 0 1 12 5c6.4 0 10 7 10 7a18 18 0 0 1-2.4 3.3"/>' +
        '<path d="M6.6 6.6A18 18 0 0 0 2 12s3.6 7 10 7a9.7 9.7 0 0 0 5.4-1.6"/>' +
        '<path d="M9.9 9.9a3 3 0 0 0 4.2 4.2"/><line x1="3" y1="3" x2="21" y2="21"/>';

    document.addEventListener('click', function (event) {
        var toggle = event.target.closest('[data-password-toggle]');
        if (!toggle) return;

        var field = document.getElementById(toggle.getAttribute('data-password-toggle'));
        if (!field) return;

        var reveal = field.type === 'password';
        field.type = reveal ? 'text' : 'password';

        toggle.setAttribute('aria-pressed', reveal ? 'true' : 'false');
        toggle.setAttribute('aria-label', reveal ? HIDE : SHOW);
        toggle.setAttribute('title', reveal ? HIDE : SHOW);

        var svg = toggle.querySelector('svg');
        if (svg) svg.innerHTML = reveal ? EYE_OFF : EYE;

        // Returning focus to the field keeps the caret where the user left it.
        field.focus();
    });

    // A slow sign-in invites a second click, and a second click posts the form
    // twice. The button is disabled for the life of the navigation only, so a
    // validation failure that re-renders the page restores a working button.
    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement)) return;

        var button = form.querySelector('button[type="submit"][data-busy-label]');
        if (!button || button.disabled) return;

        var label = button.querySelector('.submit-label');
        if (label) label.textContent = button.getAttribute('data-busy-label');

        button.disabled = true;
        button.setAttribute('aria-busy', 'true');
    });
})();

// ---------------------------------------------------------------------------
// Global keyboard handling.
//
// Ctrl+H toggles help; Escape closes it. Both are ignored while the user is
// typing, so a shortcut never eats a keystroke meant for a clinical note.
// ---------------------------------------------------------------------------

document.addEventListener('keydown', function (event) {
    var target = event.target;
    var typing = target && (
        target.tagName === 'INPUT' ||
        target.tagName === 'TEXTAREA' ||
        target.tagName === 'SELECT' ||
        target.isContentEditable);

    if (event.key === 'Escape' && window.dentalApp._help) {
        window.dentalApp._help.invokeMethodAsync('CloseFromJs');
        return;
    }

    if (typing) return;

    // Ctrl+H is the documented shortcut, but browsers reserve it for their own
    // history panel and do not always release it. F1 and "?" are offered
    // alongside it so the shortcut is reachable whatever the browser does with
    // the first one.
    var wantsHelp =
        ((event.ctrlKey || event.metaKey) && (event.key === 'h' || event.key === 'H')) ||
        event.key === 'F1' ||
        (event.key === '?' && !event.ctrlKey && !event.metaKey && !event.altKey);

    if (wantsHelp) {
        event.preventDefault();
        window.dentalApp.openHelp();
    }
});
