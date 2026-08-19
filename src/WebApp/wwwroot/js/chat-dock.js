// Client-side controller for the shopping-assistant dock.
// Owns visibility, dragging, resizing and geometry persistence so that opening
// and closing the panel never triggers a navigation or page re-render. The
// interactive Blazor circuit inside the panel is left completely untouched.
(function () {
    const GEOM_KEY = 'aw.chat.geometry.v2';
    const OPEN_KEY = 'aw.chat.open.v1';
    const MIN_W = 320;
    const MIN_H = 380;
    const EDGE = 8;

    const dock = () => document.querySelector('[data-chat-dock]');
    const fab = () => document.querySelector('[data-chat-toggle]');
    const clamp = (v, min, max) => Math.max(min, Math.min(max, v));
    const SHEET_MAX = 768; // at or below this width the CSS sheet layout owns geometry

    function readGeom() {
        try { return JSON.parse(localStorage.getItem(GEOM_KEY) || 'null'); } catch { return null; }
    }
    function writeGeom(g) {
        try { localStorage.setItem(GEOM_KEY, JSON.stringify(g)); } catch { /* ignore */ }
    }
    function isOpen() {
        try { return sessionStorage.getItem(OPEN_KEY) === '1'; } catch { return false; }
    }
    function setOpenFlag(v) {
        try { sessionStorage.setItem(OPEN_KEY, v ? '1' : '0'); } catch { /* ignore */ }
    }

    function persist(d) {
        if (!d || !d.dataset.moved) return;
        const r = d.getBoundingClientRect();
        writeGeom({ left: Math.round(r.left), top: Math.round(r.top), w: Math.round(r.width), h: Math.round(r.height) });
    }

    function applyGeom(d) {
        // On small screens the responsive CSS sheet owns the geometry: strip any
        // desktop drag/resize inline styles so the dock renders full-width there.
        if (window.innerWidth <= SHEET_MAX) {
            d.style.width = d.style.height = d.style.left = d.style.top = d.style.right = d.style.bottom = '';
            return;
        }
        const g = readGeom();
        if (!g) return; // fall back to the default CSS position (bottom-right)
        const w = Math.max(MIN_W, Math.min(g.w, window.innerWidth - EDGE * 2));
        const h = Math.max(MIN_H, Math.min(g.h, window.innerHeight - EDGE * 2));
        const left = Math.max(EDGE, Math.min(g.left, window.innerWidth - w - EDGE));
        const top = Math.max(EDGE, Math.min(g.top, window.innerHeight - h - EDGE));
        Object.assign(d.style, { width: w + 'px', height: h + 'px', left: left + 'px', top: top + 'px', right: 'auto', bottom: 'auto' });
        d.dataset.moved = '1';
    }

    function reflect(open) {
        const d = dock();
        if (!d) return;
        d.classList.toggle('is-open', open);
        const f = fab();
        if (f) f.setAttribute('aria-expanded', open ? 'true' : 'false');
        if (open) {
            const ta = d.querySelector('textarea');
            if (ta) setTimeout(() => { try { ta.focus({ preventScroll: true }); } catch { /* ignore */ } }, 40);
        }
    }

    function open() { setOpenFlag(true); reflect(true); }
    function close() { setOpenFlag(false); reflect(false); const f = fab(); if (f) f.focus(); }
    function toggle() { isOpen() ? close() : open(); }

    // Pin the current rect as explicit left/top/width/height so drag and resize
    // can work from a stable base regardless of the default right/bottom anchor.
    function pin(d) {
        const r = d.getBoundingClientRect();
        Object.assign(d.style, { left: r.left + 'px', top: r.top + 'px', width: r.width + 'px', height: r.height + 'px', right: 'auto', bottom: 'auto' });
        d.dataset.moved = '1';
        return r;
    }

    let drag = null;
    function onDragMove(e) {
        if (!drag) return;
        const d = dock();
        const w = d.offsetWidth, h = d.offsetHeight;
        let left = drag.left + (e.clientX - drag.x);
        let top = drag.top + (e.clientY - drag.y);
        left = Math.max(EDGE, Math.min(left, window.innerWidth - w - EDGE));
        top = Math.max(EDGE, Math.min(top, window.innerHeight - h - EDGE));
        d.style.left = left + 'px';
        d.style.top = top + 'px';
    }
    function onDragEnd() {
        document.removeEventListener('pointermove', onDragMove);
        document.removeEventListener('pointerup', onDragEnd);
        persist(dock());
        drag = null;
    }
    function startDrag(e) {
        const d = dock();
        if (!d || e.target.closest('[data-chat-close]')) return;
        const r = pin(d);
        drag = { x: e.clientX, y: e.clientY, left: r.left, top: r.top };
        document.addEventListener('pointermove', onDragMove);
        document.addEventListener('pointerup', onDragEnd);
        e.preventDefault();
    }

    let rez = null;
    function onResizeMove(e) {
        if (!rez) return;
        const d = dock();
        const dir = rez.dir;
        const dx = e.clientX - rez.x;
        const dy = e.clientY - rez.y;
        let left = rez.left, top = rez.top, w = rez.w, h = rez.h;

        if (dir.indexOf('e') !== -1) {
            w = clamp(rez.w + dx, MIN_W, window.innerWidth - EDGE - rez.left);
        }
        if (dir.indexOf('w') !== -1) {
            const right = rez.left + rez.w;      // right edge stays fixed
            w = clamp(rez.w - dx, MIN_W, right - EDGE);
            left = right - w;
        }
        if (dir.indexOf('s') !== -1) {
            h = clamp(rez.h + dy, MIN_H, window.innerHeight - EDGE - rez.top);
        }
        if (dir.indexOf('n') !== -1) {
            const bottom = rez.top + rez.h;      // bottom edge stays fixed
            h = clamp(rez.h - dy, MIN_H, bottom - EDGE);
            top = bottom - h;
        }
        Object.assign(d.style, { width: w + 'px', height: h + 'px', left: left + 'px', top: top + 'px' });
    }
    function onResizeEnd() {
        document.removeEventListener('pointermove', onResizeMove);
        document.removeEventListener('pointerup', onResizeEnd);
        persist(dock());
        rez = null;
    }
    function startResize(e) {
        const d = dock();
        if (!d) return;
        const handle = e.target.closest('[data-chat-resize]');
        const dir = (handle && handle.getAttribute('data-chat-resize')) || 'se';
        const r = pin(d);
        rez = { x: e.clientX, y: e.clientY, w: r.width, h: r.height, left: r.left, top: r.top, dir };
        document.addEventListener('pointermove', onResizeMove);
        document.addEventListener('pointerup', onResizeEnd);
        e.preventDefault();
        e.stopPropagation();
    }

    document.addEventListener('click', (e) => {
        if (e.target.closest('[data-chat-toggle]')) { e.preventDefault(); toggle(); }
        else if (e.target.closest('[data-chat-close]')) { e.preventDefault(); close(); }
    });
    document.addEventListener('pointerdown', (e) => {
        if (e.target.closest('[data-chat-resize]')) { startResize(e); }
        else if (e.target.closest('[data-chat-drag-handle]')) { startDrag(e); }
    });
    window.addEventListener('resize', () => { const d = dock(); if (d && d.dataset.moved) applyGeom(d); });

    // Support legacy ?chat=true deep links: open, then clean the URL with no reload.
    function consumeDeepLink() {
        const params = new URLSearchParams(location.search);
        if (params.get('chat') === 'true') {
            setOpenFlag(true);
            params.delete('chat');
            const qs = params.toString();
            history.replaceState(history.state, '', location.pathname + (qs ? '?' + qs : '') + location.hash);
        }
    }

    // Re-apply geometry + open state. Blazor's enhanced navigation morphs the DOM
    // back to server markup, stripping our JS-added class/styles, so this runs on
    // every enhanced load as well as the initial load.
    function sync() {
        const d = dock();
        if (!d) return;
        applyGeom(d);
        reflect(isOpen());
    }

    function init() {
        consumeDeepLink();
        sync();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    (function hookEnhancedNav() {
        if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
            window.Blazor.addEventListener('enhancedload', sync);
        } else {
            setTimeout(hookEnhancedNav, 150);
        }
    })();
})();
