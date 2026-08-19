// Smooth page-to-page transitions for Blazor enhanced navigation. Native
// cross-document view transitions (@view-transition) never fire here because
// enhanced navigation morphs the DOM in place instead of loading a new document,
// so we replay a short entrance animation on the routed content whenever the path
// actually changes. We drive it with the Web Animations API rather than a CSS
// class: enhanced-nav morphs the routed element's class attribute back to the
// server markup, which would strip an added class, but a WAAPI animation binds to
// the live node and survives that reconciliation. Query-only updates (catalog
// filters) and in-place enhanced form posts (add-to-cart) keep the same path and
// are intentionally skipped, and the whole effect is disabled under reduced motion.
(function () {
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)');
    let lastPath = location.pathname;

    function play() {
        if (reduce.matches) return;
        const main = document.querySelector('.eshop-main');
        if (!main || typeof main.animate !== 'function') return;
        main.animate(
            [
                { opacity: 0, transform: 'translateY(14px)' },
                { opacity: 1, transform: 'none' },
            ],
            { duration: 360, easing: 'cubic-bezier(0.16, 1, 0.3, 1)' }
        );
    }

    function onEnhancedLoad() {
        const path = location.pathname;
        if (path === lastPath) return; // same page: filter/query change or form post
        lastPath = path;
        play();
    }

    (function hookEnhancedNav() {
        if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
            window.Blazor.addEventListener('enhancedload', onEnhancedLoad);
        } else {
            setTimeout(hookEnhancedNav, 150);
        }
    })();
})();
