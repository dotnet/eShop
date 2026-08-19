// Gives the assistant FAB a pair of curved slit eyes whose ANGLE eases toward the
// cursor. The horizontal offset to the pointer is mapped PROPORTIONALLY over a ramp
// (not a unit vector, which would snap to full tilt the moment the cursor leaves the
// button) and clamped to a gentle maximum, then eased frame-by-frame so the gaze
// glides instead of jumping. The result is written to --eye-rot (never React state).
// Blink and the hover "excited" burst live in CSS. Skipped under reduced motion,
// where the slits rest upright.
(function () {
    const face = () => document.querySelector('[data-chat-eyes]');
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)');
    const MAX_DEG = 15;  // gentle max gaze tilt
    const RAMP = 460;    // px of horizontal travel that maps to full tilt
    const EASE = 0.16;   // per-frame glide toward the target angle

    let px = null, py = null;     // last pointer position
    let rot = 0, target = 0, raf = 0;

    function computeTarget() {
        const f = face();
        if (!f || px === null) { target = 0; return; }
        const r = f.getBoundingClientRect();
        const dx = px - (r.left + r.width / 2);
        const dy = py - (r.top + r.height / 2);
        // Proportional horizontal lean, softened as the pointer nears the button so
        // the eyes settle upright when the cursor is right on top of the FAB.
        const near = Math.min(1, Math.hypot(dx, dy) / 30);
        target = Math.max(-1, Math.min(1, dx / RAMP)) * MAX_DEG * near;
    }

    function frame() {
        computeTarget();
        rot += (target - rot) * EASE;
        const f = face();
        if (Math.abs(target - rot) < 0.05) {
            rot = target;
            if (f) f.style.setProperty('--eye-rot', rot.toFixed(2) + 'deg');
            raf = 0;
            return;
        }
        if (f) f.style.setProperty('--eye-rot', rot.toFixed(2) + 'deg');
        raf = requestAnimationFrame(frame);
    }

    function kick() { if (!raf) raf = requestAnimationFrame(frame); }

    function onMove(e) { px = e.clientX; py = e.clientY; kick(); }

    function upright() {
        px = py = null;
        target = rot = 0;
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
