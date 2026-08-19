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
        let w = rez.w + (e.clientX - rez.x);
        let h = rez.h + (e.clientY - rez.y);
        w = Math.max(MIN_W, Math.min(w, window.innerWidth - rez.left - EDGE));
        h = Math.max(MIN_H, Math.min(h, window.innerHeight - rez.top - EDGE));
        d.style.width = w + 'px';
        d.style.height = h + 'px';
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
        const r = pin(d);
        rez = { x: e.clientX, y: e.clientY, w: r.width, h: r.height, left: r.left, top: r.top };
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
