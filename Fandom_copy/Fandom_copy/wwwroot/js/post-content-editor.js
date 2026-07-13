// Editor helpers for /posts/{id}/content:
//   - Rich text formatting toggles + live preview
//   - Drag & drop reordering of content blocks (with server sync)
//   - Drag & drop reordering of images inside gallery blocks
//   - Section-display picker: show/hide the "text link" field
// All server communication is plain form posts protected by the anti-forgery token.

(function () {
    'use strict';

    // ---------- Rich text ----------
    function toggleFlag(form, name) {
        const input = form.querySelector('input[type="hidden"][name="' + name + '"]');
        if (!input) return;
        input.value = input.value === 'true' ? 'false' : 'true';
        updatePreview(form);
    }

    function updatePreview(form) {
        const src = form.querySelector('.js-rt-source');
        const preview = form.querySelector('.js-rt-preview');
        if (!src || !preview) return;

        const bold = form.querySelector('input[name="NewTextBold"]');
        const italic = form.querySelector('input[name="NewTextItalic"]');
        const under = form.querySelector('input[name="NewTextUnderline"]');
        const strike = form.querySelector('input[name="NewTextStrike"]');
        const size = form.querySelector('[name="NewTextSize"]');
        const align = form.querySelector('[name="NewTextAlign"]');
        const style = form.querySelector('[name="NewTextStyle"]');
        const color = form.querySelector('[name="NewTextColor"]');

        const wrap = preview.parentElement;
        if (wrap) {
            wrap.className = 'rt-preview';
            const sizeMap = { '0': 'wiki-text-size-normal', '1': 'wiki-text-size-small', '2': 'wiki-text-size-large', '3': 'wiki-text-size-heading' };
            const alignMap = { '0': 'wiki-text-align-left', '1': 'wiki-text-align-center', '2': 'wiki-text-align-right' };
            const styleMap = { '0': 'wiki-text-style-paragraph', '1': 'wiki-text-style-bullet', '2': 'wiki-text-style-number', '3': 'wiki-text-style-quote' };
            if (size && sizeMap[size.value]) wrap.classList.add(sizeMap[size.value]);
            if (align && alignMap[align.value]) wrap.classList.add(alignMap[align.value]);
            if (style && styleMap[style.value]) wrap.classList.add(styleMap[style.value]);
            if (color && color.value) wrap.classList.add('wiki-text-color-' + color.value);
        }

        const source = src.value || '';
        // Render as plain text with inline flag wrapping – no raw HTML injection.
        preview.textContent = '';
        const lines = source.split(/\n+/);
        lines.forEach(function (line) {
            const p = document.createElement('div');
            let node = document.createTextNode(line);
            let el = p;
            if (bold && bold.value === 'true') { const s = document.createElement('strong'); s.appendChild(node); node = s; }
            if (italic && italic.value === 'true') { const s = document.createElement('em'); s.appendChild(node); node = s; }
            if (under && under.value === 'true') { const s = document.createElement('u'); s.appendChild(node); node = s; }
            if (strike && strike.value === 'true') { const s = document.createElement('s'); s.appendChild(node); node = s; }
            el.appendChild(node);
            preview.appendChild(el);
        });

        // Reflect pressed buttons visually.
        form.querySelectorAll('.js-rt-toggle').forEach(function (btn) {
            const flag = form.querySelector('input[name="' + btn.dataset.target + '"]');
            btn.classList.toggle('is-active', !!(flag && flag.value === 'true'));
        });
    }

    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.js-rt-toggle');
        if (!btn) return;
        e.preventDefault();
        const form = btn.closest('.js-rich-form');
        if (!form) return;
        toggleFlag(form, btn.dataset.target);
    });

    document.addEventListener('input', function (e) {
        const form = e.target.closest('.js-rich-form');
        if (form) updatePreview(form);
    });
    document.addEventListener('change', function (e) {
        const form = e.target.closest('.js-rich-form');
        if (form) updatePreview(form);
    });

    document.querySelectorAll('.js-rich-form').forEach(updatePreview);

    // ---------- Section display style: reveal TextLink field ----------
    function refreshTextLinkVisibility(select) {
        const container = select.closest('.tool-card, .structure-edit-form, .structure-section-link');
        if (!container) return;
        const target = container.querySelector('.js-textlink-input');
        if (!target) return;
        const wanted = target.getAttribute('data-visible-when');
        target.style.display = (select.value === wanted) ? '' : 'none';
    }
    document.querySelectorAll('.js-display-select').forEach(function (sel) {
        refreshTextLinkVisibility(sel);
        sel.addEventListener('change', function () { refreshTextLinkVisibility(sel); });
    });

    // ---------- Drag & drop: content block list ----------
    const list = document.querySelector('.js-block-list');
    if (list) {
        let dragging = null;

        list.addEventListener('dragstart', function (e) {
            const block = e.target.closest('.js-block');
            if (!block) return;
            dragging = block;
            block.classList.add('is-dragging');
            try { e.dataTransfer.setData('text/plain', block.dataset.blockId || ''); } catch (_) {}
            e.dataTransfer.effectAllowed = 'move';
        });

        list.addEventListener('dragend', function () {
            if (dragging) dragging.classList.remove('is-dragging');
            dragging = null;
            list.querySelectorAll('.drop-target').forEach(function (n) { n.classList.remove('drop-target'); });
            persistOrder(list);
        });

        list.addEventListener('dragover', function (e) {
            if (!dragging) return;
            e.preventDefault();
            const target = e.target.closest('.js-block');
            if (!target || target === dragging) return;
            const rect = target.getBoundingClientRect();
            const before = (e.clientY - rect.top) < rect.height / 2;
            if (before) list.insertBefore(dragging, target);
            else list.insertBefore(dragging, target.nextSibling);
        });
    }

    function persistOrder(container) {
        if (!container) return;
        const url = container.dataset.reorderUrl;
        if (!url) return;
        const ids = Array.from(container.querySelectorAll('.js-block')).map(function (b) { return b.dataset.blockId; }).filter(Boolean);
        if (!ids.length) return;

        const tokenInput = container.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';

        const body = new URLSearchParams();
        body.append('orderedIds', ids.join(','));
        if (token) body.append('__RequestVerificationToken', token);

        fetch(url, {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'Content-Type': 'application/x-www-form-urlencoded'
            },
            body: body.toString(),
            credentials: 'same-origin'
        }).then(function (r) {
            if (!r.ok) console.warn('Reorder failed', r.status);
        }).catch(function (e) { console.warn(e); });
    }

    // ---------- Drag & drop: gallery image tiles ----------
    document.querySelectorAll('.js-gallery-sort').forEach(function (grid) {
        let dragImg = null;
        grid.addEventListener('dragstart', function (e) {
            const thumb = e.target.closest('.gallery-editor-thumb');
            if (!thumb) return;
            dragImg = thumb;
            thumb.classList.add('is-dragging');
        });
        grid.addEventListener('dragend', function () {
            if (dragImg) dragImg.classList.remove('is-dragging');
            dragImg = null;
            const orderInputSelector = grid.dataset.orderInput;
            if (!orderInputSelector) return;
            const input = document.querySelector(orderInputSelector);
            if (!input) return;
            const ids = Array.from(grid.querySelectorAll('.gallery-editor-thumb')).map(function (t) { return t.dataset.imageId; });
            input.value = ids.join(',');
        });
        grid.addEventListener('dragover', function (e) {
            if (!dragImg) return;
            e.preventDefault();
            const target = e.target.closest('.gallery-editor-thumb');
            if (!target || target === dragImg) return;
            const rect = target.getBoundingClientRect();
            const before = (e.clientX - rect.left) < rect.width / 2;
            if (before) grid.insertBefore(dragImg, target);
            else grid.insertBefore(dragImg, target.nextSibling);
        });
    });
})();
