// ==========================================================================
// SkillHub Theme Toggle (Dark / Light Mode) & Interactivity
// ==========================================================================

(function () {
    const THEME_STORAGE_KEY = 'skillhub-theme';

    function getPreferredTheme() {
        const storedTheme = localStorage.getItem(THEME_STORAGE_KEY);
        if (storedTheme) {
            return storedTheme;
        }
        // Default to dark theme for modern black & orange aesthetic
        return window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
    }

    function setTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        localStorage.setItem(THEME_STORAGE_KEY, theme);
        updateToggleButtons(theme);
    }

    function updateToggleButtons(theme) {
        const sunIcons = document.querySelectorAll('.theme-icon-sun');
        const moonIcons = document.querySelectorAll('.theme-icon-moon');
        const labels = document.querySelectorAll('.theme-label');

        if (theme === 'dark') {
            sunIcons.forEach(icon => icon.classList.remove('d-none'));
            moonIcons.forEach(icon => icon.classList.add('d-none'));
            labels.forEach(lbl => lbl.textContent = 'Modo Claro');
        } else {
            sunIcons.forEach(icon => icon.classList.add('d-none'));
            moonIcons.forEach(icon => icon.classList.remove('d-none'));
            labels.forEach(lbl => lbl.textContent = 'Modo Oscuro');
        }
    }

    // Apply theme on initial script execution to avoid layout shift
    const initialTheme = getPreferredTheme();
    document.documentElement.setAttribute('data-bs-theme', initialTheme);

    document.addEventListener('DOMContentLoaded', () => {
        updateToggleButtons(document.documentElement.getAttribute('data-bs-theme') || initialTheme);

        const toggleBtn = document.getElementById('themeToggleBtn');
        if (toggleBtn) {
            toggleBtn.addEventListener('click', (e) => {
                e.preventDefault();
                const currentTheme = document.documentElement.getAttribute('data-bs-theme') || 'dark';
                const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';
                setTheme(nextTheme);
            });
        }
    });
})();
