// Product image zoom for the item page.
// Desktop (fine pointer, motion allowed): hover magnifies the framed shot and
// the magnified region tracks the cursor. Everywhere: clicking the image opens
// a native <dialog> lightbox for a larger view. No dependencies, no navigation.
(function () {
    const canHover = window.matchMedia('(hover: hover) and (pointer: fine)');
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)');

    function initFrame(frame) {
        if (frame.dataset.zoomReady === '1') return;
        frame.dataset.zoomReady = '1';

        const trigger = frame.querySelector('[data-zoom-trigger]');
        const img = frame.querySelector('[data-zoom-image]');
        if (!trigger || !img) return;

        // Inner magnify: only where hovering makes sense and motion is welcome.
        if (canHover.matches && !reduce.matches) {
            const track = (e) => {
                const r = img.getBoundingClientRect();
                if (r.width === 0 || r.height === 0) return;
                const x = ((e.clientX - r.left) / r.width) * 100;
                const y = ((e.clientY - r.top) / r.height) * 100;
                frame.style.setProperty('--zoom-x', Math.max(0, Math.min(100, x)).toFixed(2) + '%');
                frame.style.setProperty('--zoom-y', Math.max(0, Math.min(100, y)).toFixed(2) + '%');
            };
            trigger.addEventListener('pointerenter', (e) => { track(e); frame.classList.add('is-zooming'); });
            trigger.addEventListener('pointermove', track, { passive: true });
            trigger.addEventListener('pointerleave', () => frame.classList.remove('is-zooming'));
        }
    }

    function openDialog() {
        const dlg = document.querySelector('[data-zoom-dialog]');
        if (dlg && typeof dlg.showModal === 'function' && !dlg.open) dlg.showModal();
    }
    function closeDialog() {
        const dlg = document.querySelector('[data-zoom-dialog]');
        if (dlg && dlg.open) dlg.close();
    }

    // Delegated clicks survive enhanced-nav DOM morphs without re-binding.
    document.addEventListener('click', (e) => {
        if (e.target.closest('[data-zoom-trigger]')) { e.preventDefault(); openDialog(); }
        else if (e.target.closest('[data-zoom-close]')) { e.preventDefault(); closeDialog(); }
        else {
            const dlg = e.target.closest('[data-zoom-dialog]');
            // Click on the backdrop (the dialog element itself) closes it.
            if (dlg && e.target === dlg) closeDialog();
        }
    });

    function sync() {
        document.querySelectorAll('[data-zoom]').forEach(initFrame);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', sync);
    } else {
        sync();
    }

    (function hookEnhancedNav() {
        if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
            window.Blazor.addEventListener('enhancedload', sync);
        } else {
            setTimeout(hookEnhancedNav, 150);
        }
    })();
})();
