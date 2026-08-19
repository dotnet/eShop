// Gives the assistant FAB a pair of vertical slit eyes whose ANGLE tracks the
// cursor: the horizontal direction to the pointer is mapped to a clamped tilt and
// written to the --eye-rot custom property (never React state). Blink and the
// hover "excited" burst live in CSS. Skipped under reduced motion, where the
// slits rest upright.
(function () {
    const face = () => document.querySelector('[data-chat-eyes]');
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)');
    const MAX_DEG = 20; // max slit tilt toward the cursor
    const REACH = 48; // px; within this radius the tilt eases back to upright

    let raf = 0, rot = 0;

    function onMove(e) {
        const f = face();
        if (!f) return;
        const r = f.getBoundingClientRect();
        const dx = e.clientX - (r.left + r.width / 2);
        const dy = e.clientY - (r.top + r.height / 2);
        const dist = Math.hypot(dx, dy) || 1;
        const reach = Math.min(1, dist / REACH);
        rot = (dx / dist) * reach * MAX_DEG;
        if (!raf) raf = requestAnimationFrame(apply);
    }

    function apply() {
        raf = 0;
        const f = face();
        if (f) f.style.setProperty('--eye-rot', rot.toFixed(2) + 'deg');
    }

    function upright() {
        const f = face();
        if (f) f.style.setProperty('--eye-rot', '0deg');
    }

    function enable() {
        if (reduce.matches) { upright(); return; }
        window.addEventListener('pointermove', onMove, { passive: true });
    }
    function disable() {
        window.removeEventListener('pointermove', onMove);
        if (raf) { cancelAnimationFrame(raf); raf = 0; }
        upright();
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
