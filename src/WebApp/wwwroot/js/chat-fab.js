// Positions the assistant launcher (FAB) so it FLOATS at the bottom-right corner
// while the page scrolls, then DOCKS onto the content/footer divider as the footer
// rises into view — so on tablet and desktop it never drifts down over the dark
// footer. We drive the CSS custom property --fab-bottom (never transform, which the
// button reserves for its hover/press states), so position changes track scrolling
// 1:1 with no transition lag. Progressive enhancement: with no JS the CSS default
// simply floats the button at the corner gap.
(function () {
    const fab = () => document.querySelector('.show-chatbot[data-chat-toggle]');
    const footer = () => document.querySelector('.eshop-footer');
    const actions = () => document.querySelector('.eshop-topbar-actions');

    let raf = 0;

    // Comfortable corner gap, mirroring the CSS clamp(1.25rem, 4vw, 2rem).
    function gapPx() {
        return Math.min(32, Math.max(20, window.innerWidth * 0.04));
    }

    function measure() {
        raf = 0;
        const f = fab();
        if (!f) return;
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

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', measure);
    } else {
        measure();
    }

    // Enhanced navigation morphs the DOM back to server markup, stripping our inline
    // --fab-bottom; recompute after each enhanced load once the new content settles.
    (function hookEnhancedNav() {
        if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
            window.Blazor.addEventListener('enhancedload', () => { schedule(); setTimeout(measure, 60); });
        } else {
            setTimeout(hookEnhancedNav, 150);
        }
    })();
})();
