// Gives the assistant FAB a pair of eyes whose pupils follow the cursor, so the
// button reads as a friendly, discoverable helper rather than a generic icon.
// Pointer position is written to CSS custom properties (never React state), the
// blink lives in CSS, and the whole effect is skipped under reduced motion.
(function () {
    const face = () => document.querySelector('[data-chat-eyes]');
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)');
    const MAX = 2.4; // px of pupil travel from centre
    const REACH = 260; // px at which the gaze is fully extended

    let raf = 0, tx = 0, ty = 0;

    function onMove(e) {
        const f = face();
        if (!f) return;
        const r = f.getBoundingClientRect();
        const dx = e.clientX - (r.left + r.width / 2);
        const dy = e.clientY - (r.top + r.height / 2);
        const dist = Math.hypot(dx, dy) || 1;
        const reach = Math.min(1, dist / REACH);
        tx = (dx / dist) * reach * MAX;
        ty = (dy / dist) * reach * MAX;
        if (!raf) raf = requestAnimationFrame(apply);
    }

    function apply() {
        raf = 0;
        const f = face();
        if (!f) return;
        f.style.setProperty('--eye-x', tx.toFixed(2) + 'px');
        f.style.setProperty('--eye-y', ty.toFixed(2) + 'px');
    }

    function center() {
        const f = face();
        if (f) { f.style.setProperty('--eye-x', '0px'); f.style.setProperty('--eye-y', '0px'); }
    }

    function enable() {
        if (reduce.matches) { center(); return; }
        window.addEventListener('pointermove', onMove, { passive: true });
    }
    function disable() {
        window.removeEventListener('pointermove', onMove);
        if (raf) { cancelAnimationFrame(raf); raf = 0; }
        center();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', enable);
    } else {
        enable();
    }

    if (reduce.addEventListener) {
        reduce.addEventListener('change', () => (reduce.matches ? disable() : enable()));
    }
})();
