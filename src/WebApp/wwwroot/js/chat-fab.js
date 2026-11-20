// Positions the assistant launcher (FAB) so it FLOATS at the bottom-right corner
// while the page scrolls, then DOCKS onto the content/footer divider as the footer
// rises into view — so on tablet and desktop it never drifts down over the dark
// footer. We drive the CSS custom property --fab-bottom (never transform, which the
// button reserves for its hover/press states), so position changes track scrolling
// 1:1 with no transition lag. Progressive enhancement: with no JS the CSS default
// simply floats the button at the corner gap.
//
// When the assistant panel is OPEN the launcher leaves its corner and pins to the
// panel's top-left, becoming the assistant avatar; it follows the dock through drag,
// resize and the wide toggle by watching the dock's attributes. Only the open/close
// flip eases (via the .is-morphing class); scroll/drag updates stay instant.
(function () {
    const fab = () => document.querySelector('.show-chatbot[data-chat-toggle]');
    const footer = () => document.querySelector('.eshop-footer');
    const actions = () => document.querySelector('.eshop-topbar-actions');
    const dock = () => document.querySelector('[data-chat-dock]');

    let raf = 0;
    let wasOpen = null;     // last seen panel open state, for the open/close morph
    let armed = false;      // suppress a morph on the very first frames after load
    let morphTimer = 0;
    let dockObserver = null;

    // Docked, the (shrunken) orb sits INSIDE the header's top-left as the avatar, in
    // place of a title icon: inset from the panel's left edge and centred in the header.
    const DOCK_IN_X = 12;   // px inset of the avatar from the panel's left edge

    // Comfortable corner gap, mirroring the CSS clamp(1.25rem, 4vw, 2rem).
    function gapPx() {
        return Math.min(32, Math.max(20, window.innerWidth * 0.04));
    }

    // Pin the launcher inside the open panel's header (its avatar spot), vertically
    // centred in the header band and inset from the left edge. Written through the same
    // --fab-right/--fab-bottom channel as the float position so the .is-morphing
    // transition can ease between the two. The orb's offset size reflects the docked
    // shrink (CSS --orb), so measuring offsetWidth/Height keeps the placement exact.
    function placeDocked(f, d) {
        const dr = d.getBoundingClientRect();
        const header = d.querySelector('.chatbot-header');
        const hr = header ? header.getBoundingClientRect() : dr;
        const cw = document.documentElement.clientWidth;
        const fw = f.offsetWidth || 34;
        const fh = f.offsetHeight || 34;
        const left = dr.left + DOCK_IN_X;
        const top = hr.top + (hr.height - fh) / 2;
        f.style.setProperty('--fab-right', Math.round(cw - left - fw) + 'px');
        f.style.setProperty('--fab-bottom', Math.round(window.innerHeight - top - fh) + 'px');
    }

    function measure() {
        raf = 0;
        const f = fab();
        if (!f) return;

        const d = dock();
        const open = !!(d && d.classList.contains('is-open'));
        // Ease only the open/close FLIP; scroll- and drag-driven updates stay instant.
        if (armed && wasOpen !== null && wasOpen !== open) {
            f.classList.add('is-morphing');
            clearTimeout(morphTimer);
            morphTimer = setTimeout(() => f.classList.remove('is-morphing'), 460);
            // The panel plays a ~360ms entrance animation on open, so the dock rect read
            // right now is mid-flight; re-measure a few times to settle the avatar on the
            // final corner (the .is-morphing transition eases it there smoothly).
            [90, 200, 380, 520].forEach(t => setTimeout(schedule, t));
        }
        wasOpen = open;
        if (open && d) { placeDocked(f, d); return; }

        const gap = gapPx();
        let bottom = gap;
        const ft = footer();
        if (ft) {
            // Lift the FAB so its centre rides the footer's top edge (the seam) once
            // the footer is high enough; below that it settles back to the corner.
            const seam = ft.getBoundingClientRect().top; // px from the viewport top
            const fabH = f.offsetHeight || 56;
            const docked = window.innerHeight - seam - fabH / 2;
            if (docked > bottom) bottom = docked;
        }
        f.style.setProperty('--fab-bottom', Math.round(bottom) + 'px');

        // Right-align the FAB's right edge with the top-nav actions (the cart pill),
        // measured directly so it matches regardless of scrollbar width or padding.
        // For a fixed element, right:X puts its right edge at clientWidth - X, and
        // getBoundingClientRect shares that origin, so X = clientWidth - actions.right.
        const a = actions();
        if (a) {
            const cw = document.documentElement.clientWidth;
            const right = Math.max(gap, cw - a.getBoundingClientRect().right);
            f.style.setProperty('--fab-right', Math.round(right) + 'px');
        }
    }

    function schedule() { if (!raf) raf = requestAnimationFrame(measure); }

    // The panel's open class and its drag/resize inline geometry both live on the dock;
    // watching them lets the avatar follow without coupling to chat-dock.js internals.
    function observeDock() {
        const d = dock();
        if (!d || !window.MutationObserver) return;
        if (dockObserver) dockObserver.disconnect();
        dockObserver = new MutationObserver(schedule);
        dockObserver.observe(d, { attributes: true, attributeFilter: ['class', 'style'] });
    }

    window.addEventListener('scroll', schedule, { passive: true });
    window.addEventListener('resize', schedule, { passive: true });
    window.addEventListener('load', schedule);
    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', schedule, { passive: true });
        window.visualViewport.addEventListener('scroll', schedule, { passive: true });
    }
    // Any layout height change (images loading, filters expanding, route content
    // swapping) moves the footer, so recompute whenever the page height changes.
    if (window.ResizeObserver) {
        try { new ResizeObserver(schedule).observe(document.body); } catch { /* ignore */ }
    }

    // First paint: place the FAB, start watching the dock, then arm the open/close
    // morph so a panel restored open on load simply appears docked (no fly-in).
    function boot() {
        measure();
        observeDock();
        setTimeout(() => { armed = true; }, 400);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }

    // Enhanced navigation morphs the DOM back to server markup, stripping our inline
    // --fab-bottom and replacing the dock node; recompute and re-observe after each
    // enhanced load once the new content settles.
    (function hookEnhancedNav() {
        if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
            window.Blazor.addEventListener('enhancedload', () => { schedule(); observeDock(); setTimeout(measure, 60); });
        } else {
            setTimeout(hookEnhancedNav, 150);
        }
    })();
})();
