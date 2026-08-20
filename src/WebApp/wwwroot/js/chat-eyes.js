// The assistant FAB is treated as a SPHERE: the two eyes ride its surface rather
// than sitting flat on top. A gaze offset (eased toward the pointer) shifts both
// eyes together across the orb; each eye is then projected orthographically onto the
// sphere and foreshortened along the radial direction by the surface normal's depth
// (z), so an eye compresses to a sliver as it slips toward the limb and the pair
// reads as the ball physically turning to look. Positions are written as inline
// transforms (never framework state). Blink + the hover burst live in CSS on the
// inner pupil, so gaze and blink never fight over a single transform. Skipped under
// reduced motion, and on touch / coarse pointers where the eyes rest FORWARD (centred
// gaze) and simply blink — there is no pointer to follow.
(function () {
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)');
    // Only a genuine hovering pointer (mouse / trackpad) drives the gaze. On phones and
    // other touch devices there is nothing to track, so we never wire pointer events and
    // the pair stays forward, blinking on the CSS timer.
    const fine = window.matchMedia('(hover: hover) and (pointer: fine)');

    const MAXG = 0.50;   // max gaze offset applied to both eyes (fraction of R)
    const RAMP = 320;    // px of pointer travel that maps to full gaze
    const RMAX = 0.90;   // keep an eye centre inside this fraction of the limb
    const EASE = 0.18;   // per-frame glide toward the gaze target
    const SEP = 0.28;    // rest half-separation of the eyes (fraction of R)
    const LIFT = -0.06;  // rest vertical lift (fraction of R)

    let px = null, py = null;            // last pointer position
    let gx = 0, gy = 0, tgx = 0, tgy = 0, raf = 0;

    const button = () => document.querySelector('[data-chat-eyes]');
    function eyes() {
        const b = button();
        if (!b) return null;
        const list = b.querySelectorAll('.chatbot-eye');
        return list.length === 2 ? list : null;
    }

    function computeTargets() {
        const b = button();
        if (!b || px === null || b.getAttribute('aria-expanded') === 'true') {
            tgx = 0; tgy = 0; return;
        }
        const r = b.getBoundingClientRect();
        const cx = r.left + r.width / 2, cy = r.top + r.height / 2;
        const R = r.width / 2 || 28;
        let nx = (px - cx) / RAMP, ny = (py - cy) / RAMP;
        const L = Math.hypot(nx, ny);
        if (L > 1) { nx /= L; ny /= L; }
        // Ease the gaze back to centre as the pointer settles onto the button, so the
        // eyes look straight ahead when hovered (pairing with the excited blink).
        const near = Math.min(1, Math.hypot(px - cx, py - cy) / (R * 0.75));
        tgx = nx * MAXG * near;
        tgy = ny * MAXG * near;
    }

    function place(el, bx, by, R) {
        let ex = bx + gx, ey = by + gy;
        const L = Math.hypot(ex, ey);
        if (L > RMAX) { ex = ex / L * RMAX; ey = ey / L * RMAX; }
        const z = Math.sqrt(Math.max(0, 1 - ex * ex - ey * ey)); // surface normal .z
        const a = Math.atan2(ey, ex) * 180 / Math.PI;            // radial direction
        // Translate onto the surface point, then squash along the radial direction by
        // z (tangential size preserved) so the slit foreshortens toward the limb.
        el.style.transform =
            'translate(' + (ex * R).toFixed(2) + 'px,' + (ey * R).toFixed(2) + 'px) ' +
            'rotate(' + a.toFixed(2) + 'deg) scaleX(' + z.toFixed(3) + ') rotate(' + (-a).toFixed(2) + 'deg)';
        el.style.opacity = (z < 0.16 ? Math.max(0, z / 0.16) : 1).toFixed(2);
    }

    function render() {
        const list = eyes();
        if (!list) return;
        const b = button();
        const R = (b ? b.getBoundingClientRect().width : 56) / 2 || 28;
        place(list[0], -SEP, LIFT, R);
        place(list[1], SEP, LIFT, R);
    }

    function frame() {
        computeTargets();
        gx += (tgx - gx) * EASE;
        gy += (tgy - gy) * EASE;
        render();
        if (Math.abs(tgx - gx) + Math.abs(tgy - gy) < 0.0006) {
            gx = tgx; gy = tgy; render(); raf = 0; return;
        }
        raf = requestAnimationFrame(frame);
    }
    function kick() { if (!raf) raf = requestAnimationFrame(frame); }

    function onMove(e) { px = e.clientX; py = e.clientY; kick(); }
    function rest() { px = py = null; tgx = tgy = 0; kick(); }

    function enable() {
        render();                       // set the resting pair immediately (forward gaze)
        if (reduce.matches) return;     // static under reduced motion
        if (!fine.matches) return;      // touch / coarse pointer: rest forward, blink only
        window.addEventListener('pointermove', onMove, { passive: true });
    }
    function disable() {
        window.removeEventListener('pointermove', onMove);
        if (raf) { cancelAnimationFrame(raf); raf = 0; }
        gx = gy = tgx = tgy = 0;
        render();
    }

    // The launcher resizes when it docks/undocks (CSS --orb on .show-chatbot
    // [aria-expanded]); the eyes ride the orb, so re-seat them the moment that state
    // flips and again once the size transition settles, otherwise the rest pair keeps
    // the previous radius and looks too wide on the smaller docked avatar.
    let dockWatched = false;
    function watchDockState() {
        if (dockWatched) return;
        const b = button();
        if (!b) return;
        dockWatched = true;
        if (window.MutationObserver) {
            new MutationObserver(() => { render(); kick(); })
                .observe(b, { attributes: true, attributeFilter: ['aria-expanded'] });
        }
        b.addEventListener('transitionend', (e) => {
            if (e.propertyName === 'width' || e.propertyName === 'height') render();
        });
    }

    function init() { enable(); watchDockState(); }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
    // Blazor enhanced navigation morphs the DOM and strips inline styles; re-seat the
    // eyes after each enhanced load so they never vanish to the overlapping centre.
    // enhancedload is a Blazor registry event (not a DOM event), so it must be wired
    // through Blazor.addEventListener like the sibling enhancement scripts.
    (function hookEnhancedNav() {
        if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
            window.Blazor.addEventListener('enhancedload', () => { render(); kick(); watchDockState(); });
        } else {
            setTimeout(hookEnhancedNav, 150);
        }
    })();

    if (reduce.addEventListener) {
        reduce.addEventListener('change', () => (reduce.matches ? disable() : enable()));
    }
    if (fine.addEventListener) {
        // A mouse being connected/removed (or the primary pointer changing) re-evaluates
        // whether we track the pointer; disable() first resets the pair to forward.
        fine.addEventListener('change', () => { disable(); enable(); });
    }
})();
