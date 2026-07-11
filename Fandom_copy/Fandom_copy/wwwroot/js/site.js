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
