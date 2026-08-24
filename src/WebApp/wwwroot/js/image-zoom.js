// Product image zoom for the item page.
// The framed shot is a click affordance only: an explicit click opens a native
// <dialog> lightbox that fits the image. No hover magnify, no dependencies, no
// navigation. Delegated clicks survive enhanced-nav DOM morphs without re-binding.
(function () {
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
})();
