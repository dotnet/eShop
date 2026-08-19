// Client-side sort for the catalog grid. The "Sort" dropdown in the toolbar reorders
// the product cards on the current page by name or price (ascending or descending), or
// restores the server's default "Featured" order. The choice is persisted to
// localStorage so it sticks across pages and visits, and re-applied after every Blazor
// enhanced navigation (which morphs the grid back to server order). This is pure DOM
// reordering of existing nodes; it never touches framework state and adds no motion.
(function () {
    const STORAGE_KEY = 'aw.catalog.sort.v1';
    const VALID = new Set(['featured', 'name-asc', 'name-desc', 'price-asc', 'price-desc']);

    function readPref() {
        try {
            const value = localStorage.getItem(STORAGE_KEY);
            return VALID.has(value) ? value : 'featured';
        } catch {
            return 'featured';
        }
    }

    function writePref(value) {
        try {
            localStorage.setItem(STORAGE_KEY, value);
        } catch {
            /* private mode / storage disabled: the sort still works for this page */
        }
    }

    function comparatorFor(mode) {
        const name = el => el.dataset.sortName || '';
        const price = el => parseFloat(el.dataset.sortPrice) || 0;
        const featured = el => parseInt(el.dataset.featuredIndex, 10) || 0;

        const byName = (a, b) => name(a).localeCompare(name(b), undefined, { sensitivity: 'base', numeric: true });
        const byPrice = (a, b) => price(a) - price(b);
        const byFeatured = (a, b) => featured(a) - featured(b);

        let primary;
        switch (mode) {
            case 'name-asc': primary = byName; break;
            case 'name-desc': primary = (a, b) => byName(b, a); break;
            case 'price-asc': primary = byPrice; break;
            case 'price-desc': primary = (a, b) => byPrice(b, a); break;
            default: primary = byFeatured; break;
        }

        // Featured order is the stable tie-breaker so equal names/prices keep a sensible order.
        return (a, b) => primary(a, b) || byFeatured(a, b);
    }

    function applySort(mode) {
        const grid = document.querySelector('.catalog-items');
        if (!grid) return;

        const items = Array.from(grid.querySelectorAll(':scope > .catalog-item'));
        if (items.length < 2) return;

        const sorted = items.slice().sort(comparatorFor(mode));
        const changed = sorted.some((el, i) => el !== items[i]);
        if (!changed) return;

        const fragment = document.createDocumentFragment();
        for (const el of sorted) {
            fragment.appendChild(el);
        }
        grid.appendChild(fragment);
    }

    function sync() {
        const pref = readPref();
        const select = document.querySelector('[data-catalog-sort]');
        if (select && select.value !== pref) {
            select.value = pref;
        }
        applySort(pref);
    }

    document.addEventListener('change', function (event) {
        const select = event.target.closest && event.target.closest('[data-catalog-sort]');
        if (!select) return;
        const value = VALID.has(select.value) ? select.value : 'featured';
        writePref(value);
        applySort(value);
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', sync);
    } else {
        sync();
    }

    (function hookEnhancedNav() {
        if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
            window.Blazor.addEventListener('enhancedload', sync);
        } else {
            setTimeout(hookEnhancedNav, 150);
        }
    })();
})();
