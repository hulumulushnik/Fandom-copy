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

// Lightweight UI localization for views that still contain fixed text directly
// in Razor markup. User-authored post/category/comment content is not touched.
(() => {
    const lang = (document.documentElement.getAttribute('lang') || 'uk').toLowerCase();
    if (lang !== 'en' && lang !== 'uk') return;

    const toEn = new Map([
        ['Адмінка', 'Admin'],
        ['Панель керування', 'Control panel'],
        ['Керувати', 'Manage'],
        ['Переглянути', 'View'],
        ['Структура вікі', 'Wiki structure'],
        ['Створити категорію', 'Create category'],
        ['Назва', 'Name'],
        ['Опис', 'Description'],
        ['Створити', 'Create'],
        ['Категорій ще немає', 'No categories yet'],
        ['Створіть першу категорію для групування постів.', 'Create the first category to group posts.'],
        ['Редагувати', 'Edit'],
        ['Видалити', 'Delete'],
        ['Інфо', 'Info'],
        ['Увага', 'Warning'],
        ['Цитата', 'Quote'],
        ['Розділювач', 'Divider'],
        ['Факт', 'Fact'],
        ['Лор', 'Lore'],
        ['Характеристики', 'Stats'],
        ['Шаблон', 'Template'],
        ['Користувачі', 'Users'],
        ['Користувачів немає', 'No users yet'],
        ['Список буде заповнено після реєстрації користувачів.', 'The list will fill after users register.'],
        ['Активний', 'Active'],
        ['Забанений', 'Banned'],
        ['Розбанити', 'Unban'],
        ['Забанити', 'Ban'],
        ['Пости', 'Posts'],
        ['Постів немає', 'No posts yet'],
        ['Пости з’являться тут після створення.', 'Posts will appear here after creation.'],
        ['Власник', 'Owner'],
        ['Дії', 'Actions'],
        ['Нова Fandom-сторінка', 'New Fandom page'],
        ['Створити пост', 'Create post'],
        ['Заповніть основу сторінки й одразу прикріпіть зображення — вони з’являться у структурі поста як фото-блоки.', 'Fill in the page basics and attach images right away — they will appear in the post structure as photo blocks.'],
        ['Назва сторінки', 'Page title'],
        ['Вступний текст', 'Intro text'],
        ['Категорія', 'Category'],
        ['— оберіть —', '— choose —'],
        ['Теги (через кому)', 'Tags (comma-separated)'],
        ['Теги допомагають знаходити пости в пошуку.', 'Tags help people find posts in search.'],
        ['Публічний', 'Public'],
        ['Приватний', 'Private'],
        ['Іконка поста', 'Post icon'],
        ['Значок картки поста в списках і на головній. Не потрапляє у текст статті — це окреме зображення.', 'Card icon for lists and the home page. It does not appear in the article text.'],
        ['Зображення поста', 'Post images'],
        ['Можна обрати одразу кілька фото. Вони будуть вставлені в корінь сторінки після вступного тексту.', 'You can choose multiple photos. They will be inserted into the page root after the intro text.'],
        ['Скасувати', 'Cancel'],
        ['Налаштування сторінки', 'Page settings'],
        ['Редагувати пост', 'Edit post'],
        ['Тут можна змінити основні дані й швидко додати нові зображення в корінь поста. Точний порядок блоків змінюється у «Структурі».', 'Change the main details and quickly add new images to the post root. The exact block order is changed in Structure.'],
        ['Опис / вступ', 'Description / intro'],
        ['Додати фото в пост', 'Add photos to post'],
        ['Нові зображення з’являться в кінці основної структури поста.', 'New images will appear at the end of the main post structure.'],
        ['Фото в корені', 'Root photos'],
        ['У корені поста поки немає зображень.', 'There are no root images yet.'],
        ['Відкрити структуру', 'Open structure'],
        ['← До публікацій', '← Back to posts'],
        ['Автор поста', 'Post author'],
        ['Структура', 'Structure'],
        ['+ Підпост', '+ Sub-post'],
        ['Резервні копії поста', 'Post backups'],
        ['Переглянути історію змін і відкотити пост до попередньої версії', 'View change history and roll back to an earlier version'],
        ['Підпостів поки немає.', 'No sub-posts yet.'],
        ['Відкрити →', 'Open →'],
        ['Обговорення', 'Discussion'],
        ['Коментарі', 'Comments'],
        ['Надіслати', 'Send'],
        ['Щоб залишити коментар,', 'To leave a comment,'],
        ['увійдіть', 'sign in'],
        ['у свій акаунт.', 'to your account.'],
        ['Коментарів поки немає. Будьте першим!', 'No comments yet. Be the first!'],
        ['Структура публікації', 'Post structure'],
        ['← до публікації', '← back to post'],
        ['← до підпоста', '← back to sub-post'],
        ['Редактор сторінки', 'Page editor'],
        ['Збирайте сторінку як на Fandom: вступ, галерея, картки розділів і вкладені сторінки. Перетягуйте блоки, форматуйте текст, додавайте шаблони.', 'Build the page like Fandom: intro, gallery, section cards, and nested pages. Drag blocks, format text, and add templates.'],
        ['+ Створити підпост', '+ Create sub-post'],
        ['Перетягуйте блоки мишею', 'Drag blocks with the mouse'],
        ['Сторінка поки порожня.', 'The page is empty.'],
        ['Додайте перший текст, фото, шаблон або підпост через панель інструментів.', 'Add the first text, photo, template, or sub-post from the tools panel.'],
        ['Текстовий блок', 'Text block'],
        ['Підпис до фото', 'Photo caption'],
        ['Зберегти підпис', 'Save caption'],
        ['Підпис галереї', 'Gallery caption'],
        ['Зберегти галерею', 'Save gallery'],
        ['Підпост', 'Sub-post'],
        ['Стиль відображення', 'Display style'],
        ['Текст посилання (необов’язково)', 'Link text (optional)'],
        ['Додати текст', 'Add text'],
        ['Додати фото', 'Add photo'],
        ['Спільний підпис', 'Shared caption'],
        ['Стиль галереї', 'Gallery style'],
        ['Додати галерею', 'Add gallery'],
        ['Готові блоки: інфобокс, попередження, факт, лор тощо. Можна редагувати як звичайний текст.', 'Ready-made blocks: infobox, warning, fact, lore, etc. Editable like normal text.'],
        ['Текст шаблону', 'Template text'],
        ['Додати шаблон', 'Add template'],
        ['+ Наявний підпост', '+ Existing sub-post'],
        ['Оберіть підпост', 'Choose a sub-post'],
        ['Вставити підпост', 'Insert sub-post'],
        ['Усі підпости цього рівня вже вставлено. Можна створити новий.', 'All sub-posts at this level are already inserted. You can create a new one.'],
        ['Відкрити підпост →', 'Open sub-post →'],
        ['малий', 'small'],
        ['великий', 'large'],
        ['заголовок', 'heading'],
        ['звичайний', 'normal'],
        ['ліворуч', 'left'],
        ['праворуч', 'right'],
        ['по центру', 'center'],
        ['маркований список', 'bulleted list'],
        ['нумерований список', 'numbered list'],
        ['цитата', 'quote'],
        ['абзац', 'paragraph'],
        ['текст-посилання', 'text link'],
        ['компактний рядок', 'compact row'],
        ['картка з іконкою', 'card with icon'],
        ['мозаїка', 'masonry'],
        ['карусель', 'carousel'],
        ['стрічка', 'strip'],
        ['сітка', 'grid'],
        ['Пошук по структурі', 'Structure search'],
        ['Пошук постів', 'Search posts'],
        ['Знайти', 'Search'],
        ['Введіть запит, щоб знайти пости за назвою, описом, категорією або текстом їхньої структури (під-постів).', 'Enter a query to find posts by title, description, category, or structure text (sub-posts).'],
        ['Нічого не знайдено', 'Nothing found'],
        ['Резервні копії', 'Backups'],
        ['Версії створюються автоматично перед кожною зміною редакторів. Тут вказано точний час створення кожної копії.', 'Versions are created automatically before each editor change. Exact creation time is shown here.'],
        ['Автор зміни:', 'Changed by:'],
        ['Відкотити', 'Roll back'],
        ['Попередніх версій поки немає.', 'No previous versions yet.'],
        ['Мій внесок', 'My contribution'],
        ['Мої пости', 'My posts'],
        ['Ви ще не створили жодного поста', 'You have not created any posts yet'],
        ['Почніть наповнювати вікі — створіть свій перший пост.', 'Start filling the wiki — create your first post.'],
        ['Створити перший пост', 'Create first post'],
        ['ПРОФІЛЬ КОРИСТУВАЧА', 'USER PROFILE'],
        ['Публікації', 'Publications'],
        ['Тут поки немає публічних постів', 'There are no public posts here yet'],
        ['Цей користувач ще не опублікував жодного поста.', 'This user has not published any posts yet.'],
        ['Доступ заборонено', 'Access denied'],
        ['Сторінку не знайдено', 'Page not found'],
        ['Помилка сервера', 'Server error'],
        ['Сталася помилка', 'An error occurred'],
        ['На головну', 'Home'],
    ]);

    const toUk = new Map([
        ['Change password', 'Змінити пароль'],
        ['SECURITY', 'БЕЗПЕКА'],
        ['Current password', 'Поточний пароль'],
        ['New password', 'Новий пароль'],
        ['Confirm password', 'Підтвердіть пароль'],
        ['Update password', 'Оновити пароль'],
        ['Back to profile', 'До профілю'],
        ['Email confirmation', 'Підтвердження email'],
        ['Email confirmed', 'Email підтверджено'],
        ['Confirmation failed', 'Підтвердження не вдалося'],
        ['Go to sign in', 'Перейти до входу'],
        ['Reset password', 'Скидання пароля'],
        ['ACCOUNT RECOVERY', 'ВІДНОВЛЕННЯ АКАУНТА'],
        ['Reset your password', 'Скиньте пароль'],
        ['We’ll email you a secure reset link.', 'Ми надішлемо безпечне посилання для скидання.'],
        ['Send reset link', 'Надіслати посилання для скидання'],
        ['Back to sign in', 'До входу'],
        ['Welcome back', 'З поверненням'],
        ['Sign in to continue creating and discussing.', 'Увійдіть, щоб продовжити створювати й обговорювати.'],
        ['Continue with', 'Продовжити через'],
        ['Continue with Google', 'Продовжити через Google'],
        ['Continue with Facebook', 'Продовжити через Facebook'],
        ['or', 'або'],
        ['Username or email', 'Логін або email'],
        ['Password', 'Пароль'],
        ['Keep me signed in', 'Не виходити з акаунта'],
        ['Forgot password?', 'Забули пароль?'],
        ['Resend confirmation', 'Надіслати підтвердження повторно'],
        ['New here?', 'Вперше тут?'],
        ['Create account', 'Створити акаунт'],
        ['Create your account', 'Створіть акаунт'],
        ['Join the community and start building your game worlds.', 'Приєднайтеся до спільноти й почніть створювати свої ігрові світи.'],
        ['Sign up with', 'Зареєструватися через'],
        ['Sign up with Google', 'Зареєструватися через Google'],
        ['Sign up with Facebook', 'Зареєструватися через Facebook'],
        ['Already have an account?', 'Вже маєте акаунт?'],
        ['Sign in', 'Увійти'],
        ['Username', 'Логін'],
        ['My profile', 'Мій профіль'],
        ['MY ACCOUNT', 'МІЙ АКАУНТ'],
        ['Member since', 'На сайті з'],
        ['Profile details', 'Дані профілю'],
        ['Email verified', 'Email підтверджено'],
        ['Email not verified', 'Email не підтверджено'],
        ['Save changes', 'Зберегти зміни'],
        ['Security', 'Безпека'],
        ['Keep your account secure with a unique password.', 'Захистіть акаунт унікальним паролем.'],
        ['Account role', 'Роль акаунта'],
    ]);

    const dictionary = lang === 'en' ? toEn : toUk;
    if (!dictionary.size) return;

    const translateValue = (value) => {
        if (!value) return value;
        const trimmed = value.trim();
        const translated = dictionary.get(trimmed);
        return translated ? value.replace(trimmed, translated) : value;
    };

    const excluded = new Set(['SCRIPT', 'STYLE', 'TEXTAREA', 'CODE', 'PRE']);
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, {
        acceptNode(node) {
            const parent = node.parentElement;
            if (!parent || excluded.has(parent.tagName)) return NodeFilter.FILTER_REJECT;
            if (!node.nodeValue || !node.nodeValue.trim()) return NodeFilter.FILTER_REJECT;
            return NodeFilter.FILTER_ACCEPT;
        }
    });

    const nodes = [];
    while (walker.nextNode()) nodes.push(walker.currentNode);
    nodes.forEach((node) => {
        node.nodeValue = translateValue(node.nodeValue);
    });

    document.querySelectorAll('[placeholder], [title], [aria-label]').forEach((el) => {
        ['placeholder', 'title', 'aria-label'].forEach((attr) => {
            if (el.hasAttribute(attr)) el.setAttribute(attr, translateValue(el.getAttribute(attr)));
        });
    });
})();

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
