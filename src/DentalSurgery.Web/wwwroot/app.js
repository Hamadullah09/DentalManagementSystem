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
