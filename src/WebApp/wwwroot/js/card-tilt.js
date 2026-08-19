// Astro-Shop-inspired pointer tilt for catalog product cards. On hover the card
// tilts in 3D toward the cursor. The rotation is written to CSS custom properties
// (never framework state) and applied on a requestAnimationFrame tick; the card's
// own CSS transition eases the motion so it follows smoothly rather than snapping.
// Runs only for fine pointers, is fully skipped under reduced motion, and is safe
// across Blazor enhanced navigation because every listener is delegated from
// document (new cards just work).
(function () {
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)');
    const fine = window.matchMedia('(hover: hover) and (pointer: fine)');
    const MAX = 7; // degrees of tilt at the card edge

    let active = null, raf = 0, rx = 0, ry = 0;

    function apply() {
        raf = 0;
        if (!active) return;
        active.style.setProperty('--card-rx', rx.toFixed(2) + 'deg');
        active.style.setProperty('--card-ry', ry.toFixed(2) + 'deg');
    }

    function onMove(e) {
        if (!active) return;
        const r = active.getBoundingClientRect();
        const px = (e.clientX - r.left) / r.width;
        const py = (e.clientY - r.top) / r.height;
        ry = (px - 0.5) * 2 * MAX;   // horizontal position -> rotateY
        rx = -(py - 0.5) * 2 * MAX;  // vertical position -> rotateX
        if (!raf) raf = requestAnimationFrame(apply);
    }

    function reset(card) {
        card.classList.remove('is-tilting');
        card.style.setProperty('--card-rx', '0deg');
        card.style.setProperty('--card-ry', '0deg');
    }

    function onOver(e) {
        const card = e.target.closest && e.target.closest('.catalog-product');
        if (!card) return;
        if (active && active !== card) reset(active);
        active = card;
        card.classList.add('is-tilting');
    }

    function onOut(e) {
        const card = e.target.closest && e.target.closest('.catalog-product');
        if (!card) return;
        if (e.relatedTarget && card.contains(e.relatedTarget)) return; // still inside
        reset(card);
        if (active === card) {
            active = null;
            if (raf) { cancelAnimationFrame(raf); raf = 0; }
        }
    }

    function enable() {
        if (reduce.matches || !fine.matches) return;
        document.addEventListener('pointerover', onOver, { passive: true });
        document.addEventListener('pointerout', onOut, { passive: true });
        document.addEventListener('pointermove', onMove, { passive: true });
    }

    function disable() {
        document.removeEventListener('pointerover', onOver);
        document.removeEventListener('pointerout', onOut);
        document.removeEventListener('pointermove', onMove);
        if (active) reset(active);
        active = null;
        if (raf) { cancelAnimationFrame(raf); raf = 0; }
    }

    enable();
    if (reduce.addEventListener) {
        reduce.addEventListener('change', () => (reduce.matches ? disable() : enable()));
    }
})();
