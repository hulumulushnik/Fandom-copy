// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener('change', (event) => {
    const input = event.target;
    if (!(input instanceof HTMLInputElement) || !input.classList.contains('js-image-input')) return;

    const previewSelector = input.dataset.preview;
    const preview = previewSelector ? document.querySelector(previewSelector) : null;
    if (!preview) return;

    preview.innerHTML = '';
    const files = Array.from(input.files || []);
    files.slice(0, 12).forEach((file) => {
        if (!file.type.startsWith('image/')) return;

        const card = document.createElement('figure');
        const img = document.createElement('img');
        const caption = document.createElement('figcaption');

        img.src = URL.createObjectURL(file);
        img.onload = () => URL.revokeObjectURL(img.src);
        caption.textContent = file.name;

        card.appendChild(img);
        card.appendChild(caption);
        preview.appendChild(card);
    });
});

// Settings page: live toggle .active on lang/theme cards when a radio inside them changes.
document.addEventListener('change', (event) => {
    const input = event.target;
    if (!(input instanceof HTMLInputElement) || input.type !== 'radio') return;
    if (!input.name || (input.name !== 'lang' && input.name !== 'theme')) return;

    const cls = input.name === 'lang' ? 'lang-option' : 'theme-option';
    document.querySelectorAll('.' + cls).forEach(el => el.classList.remove('active'));
    const wrapper = input.closest('.' + cls);
    if (wrapper) wrapper.classList.add('active');
});

document.addEventListener('click', (event) => {
    const button = event.target.closest('[data-template-target]');
    if (!button) return;

    const target = document.querySelector(button.dataset.templateTarget);
    if (!(target instanceof HTMLTextAreaElement)) return;

    const template = button.dataset.template || '';
    const start = target.selectionStart ?? target.value.length;
    const end = target.selectionEnd ?? target.value.length;
    const before = target.value.slice(0, start);
    const after = target.value.slice(end);
    const spacer = before && !before.endsWith('\n') ? '\n' : '';

    target.value = `${before}${spacer}${template}${after}`;
    target.focus();
    const cursor = before.length + spacer.length + template.length;
    target.setSelectionRange(cursor, cursor);
});

// ---------------------------------------------------------------------
// Reveal-on-scroll: fade/slide in card-like content as it enters view.
// Works on any existing markup without touching the .cshtml views.
// ---------------------------------------------------------------------
(() => {
    const revealSelectors = [
        '.post-row', '.home-jump-card', '.home-category-card', '.category-card',
        '.search-result-card', '.saved-item', '.tool-card', '.structure-block',
        '.admin-metric-card', '.frame-picker-card', '.post-author-card',
        '.wiki-image-block', '.wiki-topic-card', '.attachment-item',
        '.post-version-item', '.post-comment-item', '.settings-card',
        '.account-card', '.profile-card', '.editor-card'
    ];

    const targets = document.querySelectorAll(revealSelectors.join(','));
    if (!targets.length) return;

    document.documentElement.classList.add('js-reveal-ready');
    targets.forEach((el, i) => {
        el.classList.add('reveal-on-scroll');
        el.style.setProperty('--reveal-i', String(i % 10));
    });

    if (!('IntersectionObserver' in window)) {
        targets.forEach((el) => el.classList.add('in-view'));
        return;
    }

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (!entry.isIntersecting) return;
            entry.target.classList.add('in-view');
            observer.unobserve(entry.target);
        });
    }, { threshold: 0.12, rootMargin: '0px 0px -40px 0px' });

    targets.forEach((el) => observer.observe(el));
})();

// ---------------------------------------------------------------------
// Small ripple on primary/secondary/nav buttons for a livelier feel.
// ---------------------------------------------------------------------
document.addEventListener('click', (event) => {
    const btn = event.target.closest('.primary-btn, .secondary-btn, .nav-join, .btn-primary, .rt-btn');
    if (!btn) return;

    const rect = btn.getBoundingClientRect();
    const ripple = document.createElement('span');
    const size = Math.max(rect.width, rect.height);

    ripple.className = 'fx-ripple';
    ripple.style.width = ripple.style.height = `${size}px`;
    ripple.style.left = `${event.clientX - rect.left - size / 2}px`;
    ripple.style.top = `${event.clientY - rect.top - size / 2}px`;

    const previousPosition = getComputedStyle(btn).position;
    if (previousPosition === 'static') btn.style.position = 'relative';
    btn.appendChild(ripple);
    ripple.addEventListener('animationend', () => ripple.remove());
});
