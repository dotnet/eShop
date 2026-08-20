// Client-side sort for the catalog grid. A custom, on-theme "Sort" control in the toolbar
// (an ARIA select-only combobox: a button-like trigger plus a listbox popup) reorders the
// product cards on the current page by name or price (ascending or descending), or restores
// the server's default "Featured" order. The choice is persisted to localStorage so it
// sticks across pages and visits, and re-applied after every Blazor enhanced navigation and
// after streaming-render populates the grid. This is pure DOM reordering of existing nodes;
// it never touches framework state and adds no motion.
(function () {
    const STORAGE_KEY = 'aw.catalog.sort.v1';
    const OPTIONS = [
        { value: 'featured', label: 'Featured' },
        { value: 'name-asc', label: 'Name: A to Z' },
        { value: 'name-desc', label: 'Name: Z to A' },
        { value: 'price-asc', label: 'Price: Low to High' },
        { value: 'price-desc', label: 'Price: High to Low' },
    ];
    const VALID = new Set(OPTIONS.map(o => o.value));
    const LABELS = new Map(OPTIONS.map(o => [o.value, o.label]));

    // -- preference + sort engine -------------------------------------------------

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

    let sorting = false;

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
        sorting = true;
        grid.appendChild(fragment);
        sorting = false;
    }

    // -- custom combobox control --------------------------------------------------

    const root = () => document.querySelector('[data-catalog-sort]');
    const triggerOf = r => r.querySelector('[data-catalog-sort-trigger]');
    const listboxOf = r => r.querySelector('[data-catalog-sort-listbox]');
    const optionsOf = r => Array.from(r.querySelectorAll('[role="option"]'));
    const isOpen = r => triggerOf(r).getAttribute('aria-expanded') === 'true';

    function reflect(r, value) {
        const valueEl = r.querySelector('[data-catalog-sort-value]');
        if (valueEl) valueEl.textContent = LABELS.get(value) || LABELS.get('featured');
        for (const opt of optionsOf(r)) {
            opt.setAttribute('aria-selected', opt.dataset.value === value ? 'true' : 'false');
        }
    }

    function setActive(r, opt) {
        if (!opt) return;
        for (const o of optionsOf(r)) o.classList.toggle('is-active', o === opt);
        triggerOf(r).setAttribute('aria-activedescendant', opt.id);
        opt.scrollIntoView({ block: 'nearest' });
    }

    function activeOption(r) {
        return optionsOf(r).find(o => o.classList.contains('is-active')) || null;
    }

    function open(r) {
        listboxOf(r).hidden = false;
        triggerOf(r).setAttribute('aria-expanded', 'true');
        r.classList.add('is-open');
        const selected = optionsOf(r).find(o => o.getAttribute('aria-selected') === 'true');
        setActive(r, selected || optionsOf(r)[0]);
        triggerOf(r).focus({ preventScroll: true });
    }

    function close(r, focusTrigger) {
        const tr = triggerOf(r);
        listboxOf(r).hidden = true;
        tr.setAttribute('aria-expanded', 'false');
        tr.removeAttribute('aria-activedescendant');
        r.classList.remove('is-open');
        for (const o of optionsOf(r)) o.classList.remove('is-active');
        if (focusTrigger) tr.focus({ preventScroll: true });
    }

    function selectValue(r, value) {
        if (!VALID.has(value)) value = 'featured';
        writePref(value);
        reflect(r, value);
        applySort(value);
    }

    function move(r, delta) {
        const opts = optionsOf(r);
        const cur = activeOption(r);
        let idx = cur ? opts.indexOf(cur) : -1;
        idx = Math.max(0, Math.min(opts.length - 1, idx + delta));
        setActive(r, opts[idx]);
    }

    let typeBuffer = '';
    let typeTimer = null;
    function typeAhead(r, ch) {
        clearTimeout(typeTimer);
        typeBuffer += ch.toLowerCase();
        typeTimer = setTimeout(() => { typeBuffer = ''; }, 500);
        const match = optionsOf(r).find(o => o.textContent.trim().toLowerCase().startsWith(typeBuffer));
        if (!match) return;
        if (isOpen(r)) setActive(r, match);
        else selectValue(r, match.dataset.value);
    }

    document.addEventListener('click', function (event) {
        const r = root();
        if (!r) return;
        const target = event.target;

        const opt = target.closest && target.closest('[role="option"]');
        if (opt && r.contains(opt)) {
            selectValue(r, opt.dataset.value);
            close(r, true);
            return;
        }

        const trg = target.closest && target.closest('[data-catalog-sort-trigger]');
        if (trg && r.contains(trg)) {
            if (isOpen(r)) close(r, true); else open(r);
            return;
        }

        if (isOpen(r) && !r.contains(target)) close(r, false);
    });

    document.addEventListener('keydown', function (event) {
        const r = root();
        if (!r || !r.contains(document.activeElement)) return;

        const opened = isOpen(r);
        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                if (!opened) open(r); else move(r, 1);
                break;
            case 'ArrowUp':
                event.preventDefault();
                if (!opened) open(r); else move(r, -1);
                break;
            case 'Home':
                if (opened) { event.preventDefault(); setActive(r, optionsOf(r)[0]); }
                break;
            case 'End':
                if (opened) { event.preventDefault(); const o = optionsOf(r); setActive(r, o[o.length - 1]); }
                break;
            case 'Enter':
            case ' ':
            case 'Spacebar':
                event.preventDefault();
                if (!opened) { open(r); }
                else { const a = activeOption(r); if (a) selectValue(r, a.dataset.value); close(r, true); }
                break;
            case 'Escape':
                if (opened) { event.preventDefault(); close(r, true); }
                break;
            case 'Tab':
                if (opened) {
                    const a = activeOption(r);
                    if (a) selectValue(r, a.dataset.value);
                    close(r, false);
                }
                break;
            default:
                if (event.key.length === 1 && /\S/.test(event.key) && !event.ctrlKey && !event.metaKey && !event.altKey) {
                    event.preventDefault();
                    typeAhead(r, event.key);
                }
                break;
        }
    });

    // -- keep the grid + control in sync ------------------------------------------

    function sync() {
        const pref = readPref();
        const r = root();
        if (r) reflect(r, pref);
        applySort(pref);
        observeCatalog();
    }

    // Streaming render and enhanced navigation both repopulate the grid without firing a
    // full page load. Observe the catalog container and re-apply the sort whenever its
    // children change so a persisted preference survives the streamed first paint and
    // every subsequent filter / pagination morph. applySort is idempotent and guarded by
    // the `sorting` flag, so its own reordering does not loop.
    let observer = null;
    let observed = null;
    let scheduled = false;
    function observeCatalog() {
        const container = document.querySelector('.catalog');
        if (!container || observed === container) return;
        if (observer) observer.disconnect();
        observed = container;
        observer = new MutationObserver(function () {
            if (sorting || scheduled) return;
            scheduled = true;
            requestAnimationFrame(function () {
                scheduled = false;
                applySort(readPref());
            });
        });
        observer.observe(container, { childList: true, subtree: true });
    }

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
