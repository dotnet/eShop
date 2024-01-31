export function scrollToEnd(element) {
    element.scrollTo({ top: element.scrollHeight, behavior: 'smooth' });
}

export function submitOnEnter(element) {
    element.addEventListener('keydown', event => {
        if (event.key === 'Enter') {
            event.target.dispatchEvent(new Event('change'));
            event.target.closest('form').dispatchEvent(new Event('submit'));
        }
    });
}
