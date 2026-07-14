namespace Fandom_copy.Localization
{
    /// <summary>
    /// Small static dictionary that translates the fixed interface "chrome"
    /// (navigation, buttons, labels) into the languages the site supports.
    /// This intentionally does NOT translate user-authored content (post
    /// titles, texts, section names) — that is data the users themselves
    /// wrote, so it is shown exactly as entered regardless of the selected
    /// interface language.
    /// </summary>
    public static class UiText
    {
        public const string DefaultLang = "uk";

        public static readonly (string Code, string Label)[] SupportedLanguages =
        {
            ("uk", "Українська"),
            ("en", "English"),
        };

        private static readonly HashSet<string> SupportedLanguageCodes = new(SupportedLanguages.Select(l => l.Code));

        public static readonly (string Code, string Label)[] SupportedThemes =
        {
            ("dark", "Темна"),
            ("light", "Світла"),
            ("sunset", "Sunset"),
        };

        private static readonly Dictionary<string, Dictionary<string, string>> ThemeLabels = new()
        {
            ["uk"] = new() { ["dark"] = "Темна",  ["light"] = "Світла", ["sunset"] = "Захід сонця" },
            ["en"] = new() { ["dark"] = "Dark",   ["light"] = "Light",  ["sunset"] = "Sunset" },
            ["de"] = new() { ["dark"] = "Dunkel", ["light"] = "Hell",   ["sunset"] = "Sonnenuntergang" },
            ["ja"] = new() { ["dark"] = "ダーク",  ["light"] = "ライト",  ["sunset"] = "サンセット" },
        };

        public static string ThemeLabel(string lang, string themeCode)
        {
            var normalized = Normalize(lang);
            if (ThemeLabels.TryGetValue(normalized, out var table) && table.TryGetValue(themeCode, out var value))
                return value;
            return themeCode;
        }

        private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
        {
            ["uk"] = new(),
            ["en"] = new(),
            ["de"] = new(),
            ["ja"] = new(),
        };

        static UiText()
        {
            InitStrings();
        }

        public static string Normalize(string? lang)
        {
            if (string.IsNullOrWhiteSpace(lang) || !SupportedLanguageCodes.Contains(lang))
                return DefaultLang;
            return lang;
        }

        public static string T(string lang, string key)
        {
            var normalized = Normalize(lang);
            if (Strings.TryGetValue(normalized, out var table) && table.TryGetValue(key, out var value))
                return value;
            return Strings[DefaultLang].TryGetValue(key, out var fallback) ? fallback : key;
        }

        private static void Add(string key, string uk, string en, string de, string ja)
        {
            Strings["uk"][key] = uk;
            Strings["en"][key] = en;
            Strings["de"][key] = de;
            Strings["ja"][key] = ja;
        }

        private static void InitStrings()
        {
            // ── Navigation / header ────────────────────────────────────────────
            Add("nav.search.placeholder",
                "Пошук постів, категорій, розділів…",
                "Search posts, categories, sections…",
                "Beiträge, Kategorien oder Abschnitte suchen…",
                "投稿・カテゴリー・セクションを検索…");
            Add("nav.login",    "Увійти",           "Log in",   "Anmelden",     "ログイン");
            Add("nav.register", "Зареєструватися",  "Sign up",  "Registrieren", "新規登録");
            Add("nav.logout",   "Вийти",            "Log out",  "Abmelden",     "ログアウト");

            // ── Sidebar ────────────────────────────────────────────────────────
            Add("sidebar.home",       "Головна",     "Home",       "Startseite",     "ホーム");
            Add("sidebar.posts",      "Пости",       "Posts",      "Beiträge",       "投稿");
            Add("sidebar.categories", "Категорії",   "Categories", "Kategorien",     "カテゴリー");
            Add("sidebar.saved",      "Збережене",   "Saved",      "Gespeichert",    "保存済み");
            Add("sidebar.rules",      "Правила",     "Rules",      "Regeln",         "ルール");
            Add("sidebar.settings",   "Налаштування","Settings",   "Einstellungen",  "設定");
            Add("sidebar.admin",      "Адмінпанель", "Admin panel","Administration", "管理画面");

            Add("footer.rights",
                "Усі права захищено",
                "All rights reserved",
                "Alle Rechte vorbehalten",
                "無断転載を禁じます");

            // ── Settings page ──────────────────────────────────────────────────
            Add("settings.kicker",      "Персоналізація", "Personalization",   "Personalisierung",             "パーソナライズ");
            Add("settings.title",       "Налаштування",   "Settings",          "Einstellungen",                "設定");
            Add("settings.intro",
                "Оберіть мову інтерфейсу та колірну тему сайту.",
                "Choose the interface language and the site's color theme.",
                "Wähle die Sprache der Benutzeroberfläche und das Farbschema der Seite.",
                "表示言語とサイトのカラーテーマを選択してください。");
            Add("settings.lang.title",  "Мова сайту",     "Site language",     "Sprache der Benutzeroberfläche", "表示言語");
            Add("settings.lang.hint",
                "Впливає на текст меню, кнопок і підписів інтерфейсу.",
                "Affects menu items, buttons, and interface labels.",
                "Wirkt sich auf Menüs, Schaltflächen und Beschriftungen der Oberfläche aus.",
                "メニュー・ボタン・ラベルの表示に反映されます。");
            Add("settings.theme.title", "Тема сайту",     "Site theme",        "Farbschema",                   "カラーテーマ");
            Add("settings.theme.hint",
                "Колірна схема, у якій відображається весь сайт.",
                "The color scheme applied across the whole site.",
                "Das Farbschema, das auf der gesamten Seite verwendet wird.",
                "サイト全体に適用される配色です。");
            Add("settings.save",  "Зберегти налаштування", "Save settings", "Einstellungen speichern", "設定を保存");
            Add("settings.saved", "Налаштування збережено.", "Settings saved.", "Einstellungen gespeichert.", "設定を保存しました。");

            // ── Home page ──────────────────────────────────────────────────────
            Add("home.title",              "Головна",                    "Home",                       "Startseite",                    "ホーム");
            Add("home.recent",             "Останні оновлення",          "Recent updates",             "Neueste Aktualisierungen",       "最新の更新");
            Add("home.jump_back",          "Продовжити перегляд",        "Jump back in",               "Weiterlesen",                    "続きから見る");
            Add("home.all_saved",          "Усе збережене",              "All saved",                  "Alle gespeicherten",             "すべての保存済み");
            Add("home.all_posts",          "Усі пости",                  "All posts",                  "Alle Beiträge",                  "すべての投稿");
            Add("home.empty",              "Постів ще немає.",           "No posts yet.",              "Noch keine Beiträge.",           "まだ投稿はありません。");
            Add("home.create_first",       "Створіть перший пост →",     "Create the first post →",    "Ersten Beitrag erstellen →",     "最初の投稿を作成 →");
            Add("home.recent_posts",       "Останні пости",              "Recent posts",               "Neueste Beiträge",               "最新の投稿");
            Add("home.categories_to_open", "Категорії, які варто відкрити", "Categories worth exploring", "Kategorien zum Entdecken",   "おすすめのカテゴリー");

            // ── Posts index / listing ─────────────────────────────────────────
            Add("posts.community",     "Спільнота FandomHub", "FandomHub community", "FandomHub-Community", "FandomHub コミュニティ");
            Add("posts.title",         "Пости",               "Posts",               "Beiträge",             "投稿");
            Add("posts.create",        "+ Створити пост",     "+ Create post",       "+ Beitrag erstellen",  "+ 投稿を作成");
            Add("posts.public",        "Публічний",           "Public",              "Öffentlich",           "公開");
            Add("posts.private",       "Приватний",           "Private",             "Privat",               "非公開");
            Add("posts.count_suffix",  "пост(ів)",            "post(s)",             "Beitrag/Beiträge",     "件の投稿");
            Add("posts.empty",         "Постів ще немає",     "No posts yet",        "Noch keine Beiträge",  "まだ投稿はありません");
            Add("posts.empty_hint",
                "Станьте першим — створіть пост і почніть наповнювати вікі.",
                "Be the first — create a post and start filling the wiki.",
                "Sei die erste Person und erstelle einen Beitrag, um das Wiki zu füllen.",
                "最初の一人になろう — 投稿を作成して Wiki を育てましょう。");

            // ── Saved ─────────────────────────────────────────────────────────
            Add("saved.kicker", "Ваша бібліотека",         "Your library",   "Deine Bibliothek",           "あなたのライブラリ");
            Add("saved.title",  "Збережене",               "Saved",          "Gespeichert",                "保存済み");
            Add("saved.category_label", "Категорія", "Category", "Kategorie", "カテゴリー");
            Add("saved.remove",         "Прибрати",  "Remove",   "Entfernen", "削除");
            Add("saved.empty",
                "Ви ще нічого не зберегли. Натисніть «Зберегти» на сторінці поста, щоб додати його сюди.",
                "You haven't saved anything yet. Click \"Save\" on a post page to add it here.",
                "Du hast noch nichts gespeichert. Klicke auf einer Beitragsseite auf \u201eSpeichern\u201c, um ihn hier hinzuzufügen.",
                "まだ何も保存していません。投稿ページで「保存」を押すと、ここに追加されます。");

            // ── Categories ────────────────────────────────────────────────────
            Add("categories.title",        "Категорії",                                 "Categories",                            "Kategorien",                                 "カテゴリー");
            Add("categories.default_desc", "Стандартна категорія для постів спільноти.", "Default category for community posts.", "Standardkategorie für Community-Beiträge.",  "コミュニティ投稿の既定カテゴリー。");
            Add("categories.empty",        "Категорій ще немає",                         "No categories yet",                     "Noch keine Kategorien",                      "まだカテゴリーはありません");
            Add("categories.all",           "Усі категорії",                    "All categories",                  "Alle Kategorien",                       "すべてのカテゴリー");
            Add("categories.single",        "Категорія",                        "Category",                        "Kategorie",                             "カテゴリー");
            Add("categories.all_posts_of",  "Усі пости категорії",              "All posts in this category",      "Alle Beiträge dieser Kategorie",        "このカテゴリーの投稿");
            Add("categories.no_posts",      "У цій категорії ще немає постів",  "This category has no posts yet",  "In dieser Kategorie gibt es noch keine Beiträge","このカテゴリーにはまだ投稿がありません");
            Add("categories.no_posts_hint",
                "Станьте першим — створіть пост у цій категорії.",
                "Be the first — create a post in this category.",
                "Sei die erste Person und erstelle einen Beitrag in dieser Kategorie.",
                "最初の一人になろう — このカテゴリーに投稿を作成しましょう。");
            Add("categories.empty_hint",
                "Вони з'являться тут, щойно хтось їх додасть.",
                "They will appear here as soon as someone adds them.",
                "Sie erscheinen hier, sobald jemand welche hinzufügt.",
                "誰かが追加すると、ここに表示されます。");

            // ── Post sections (details view) ──────────────────────────────────
            Add("section.back_to_posts",     "← До постів",         "← Back to posts", "← Zurück zu den Beiträgen", "← 投稿一覧へ");
            Add("section.author",            "Автор поста",         "Post author",     "Autor des Beitrags",        "投稿者");
            Add("section.edit",              "Редагувати",          "Edit",            "Bearbeiten",                "編集");
            Add("section.structure",         "Структура",           "Structure",       "Struktur",                  "構成");
            Add("section.add_subpost",       "+ Підпост",           "+ Sub-post",      "+ Unterbeitrag",            "+ サブ投稿");
            Add("section.save_button",       "Зберегти",            "Save",            "Speichern",                 "保存");
            Add("section.saved_button",      "Збережено",           "Saved",           "Gespeichert",               "保存済み");
            Add("section.owner_tools",       "Інструменти власника","Owner tools",     "Besitzer-Tools",            "所有者ツール");
            Add("section.rollback",          "Відкат поста",        "Rollback post",   "Beitrag zurücksetzen",      "投稿を巻き戻す");
            Add("section.rollback_hint",
                "Версії створюються автоматично перед змінами редакторів.",
                "Versions are created automatically before editor changes.",
                "Versionen werden automatisch vor Änderungen durch Redakteure erstellt.",
                "編集前にバージョンが自動的に作成されます。");
            Add("section.rollback_button",   "Відкотити",           "Rollback",         "Zurücksetzen",             "巻き戻す");
            Add("section.discussion",        "Обговорення",         "Discussion",       "Diskussion",               "ディスカッション");
            Add("section.comments",          "Коментарі",           "Comments",         "Kommentare",               "コメント");
            Add("section.comment_placeholder","Напишіть свій коментар…","Write your comment…","Schreibe deinen Kommentar…","コメントを書いてください…");
            Add("section.send",              "Надіслати",           "Send",             "Senden",                   "送信");
            Add("section.delete",            "Видалити",            "Delete",           "Löschen",                  "削除");
            Add("section.welcome_prefix",    "Ласкаво просимо до вікі", "Welcome to the","Willkommen im",           "ようこそ、");
            Add("section.welcome_suffix",    "",                    "Wiki!",            "-Wiki!",                   "の Wiki へ！");
            Add("section.subpost",           "Підпост",             "Sub-post",         "Unterbeitrag",              "サブ投稿");
            Add("section.post",              "Публікація",          "Post",             "Beitrag",                   "投稿");
            Add("section.no_subposts",       "Поки що немає вкладених підпостів.", "No nested sub-posts yet.", "Noch keine untergeordneten Unterbeiträge.", "まだ入れ子のサブ投稿はありません。");
            Add("section.open",              "Відкрити",            "Open",             "Öffnen",                    "開く");
            Add("section.files_kicker",      "Файли",               "Files",            "Dateien",                   "ファイル");
            Add("section.attached_files",    "Прикріплені файли",   "Attached files",   "Angehängte Dateien",        "添付ファイル");
            Add("section.file_badge",        "ФАЙЛ",                "FILE",             "DATEI",                     "ファイル");
            Add("section.delete_confirm",    "Видалити підпост і всі вкладені?", "Delete this sub-post and everything nested?", "Diesen Unterbeitrag und alle darunter löschen?", "このサブ投稿と入れ子の項目をすべて削除しますか？");
            Add("unit.mb",                   "МБ",                  "MB",               "MB",                        "MB");
            Add("unit.kb",                   "КБ",                  "KB",               "KB",                        "KB");

            // ── Sub-post editor ───────────────────────────────────────────────
            Add("subpost.editor",           "Редактор підпоста",      "Sub-post editor",       "Unterbeitrag-Editor",              "サブ投稿エディター");
            Add("subpost.edit_title",       "Редагувати підпост",     "Edit sub-post",         "Unterbeitrag bearbeiten",          "サブ投稿を編集");
            Add("subpost.create_title",     "Створити підпост",       "Create sub-post",       "Unterbeitrag erstellen",           "サブ投稿を作成");
            Add("subpost.intro",
                "Основний текст і нові фото можна змінювати тут, а точне розташування блоків — у «Структурі».",
                "Edit the main text and new photos here; the exact block layout lives in \"Structure\".",
                "Haupttext und neue Fotos hier bearbeiten; die genaue Anordnung der Blöcke liegt in \u201eStruktur\u201c.",
                "本文と新しい写真はここで、ブロックの配置は「構成」で編集します。");
            Add("subpost.title_field",      "Заголовок",              "Title",                 "Titel",                            "タイトル");
            Add("subpost.base_text",        "Основний текст",         "Base text",             "Haupttext",                        "本文");
            Add("subpost.order",            "Порядок",                "Order",                 "Reihenfolge",                      "並び順");
            Add("subpost.icon",             "Іконка підпоста",        "Sub-post icon",         "Symbol des Unterbeitrags",         "サブ投稿アイコン");
            Add("subpost.icon_hint",
                "Значок картки цього підпоста в структурі — окремо від фото в тексті.",
                "Card icon for this sub-post in the structure — separate from photos in the text.",
                "Kartensymbol dieses Unterbeitrags in der Struktur – getrennt von den Fotos im Text.",
                "構成上のこのサブ投稿のカードアイコン（本文中の写真とは別）。");
            Add("subpost.icon_replace",     "Нове зображення замінить поточну іконку.",
                                            "The new image will replace the current icon.",
                                            "Das neue Bild ersetzt das aktuelle Symbol.",
                                            "新しい画像が現在のアイコンを置き換えます。");
            Add("subpost.add_photo",        "Додати фото",            "Add photo",             "Foto hinzufügen",                  "写真を追加");
            Add("subpost.photo_caption",    "Спільний підпис до нових фото",
                                            "Shared caption for new photos",
                                            "Gemeinsame Bildunterschrift für neue Fotos",
                                            "新しい写真の共通キャプション");
            Add("subpost.photo_hint",
                "Нові фото додаються в кінець структури підпоста.",
                "New photos are appended to the end of the sub-post structure.",
                "Neue Fotos werden am Ende der Unterbeitragsstruktur eingefügt.",
                "新しい写真はサブ投稿構成の末尾に追加されます。");
            Add("subpost.add_files",        "Додати файли",           "Add files",             "Dateien hinzufügen",               "ファイルを追加");
            Add("subpost.files_hint_types",
                "PDF, TXT, ZIP, DOCX, XLSX, PNG або JPG до 15 МБ.",
                "PDF, TXT, ZIP, DOCX, XLSX, PNG or JPG up to 15 MB.",
                "PDF, TXT, ZIP, DOCX, XLSX, PNG oder JPG bis 15 MB.",
                "PDF・TXT・ZIP・DOCX・XLSX・PNG・JPG（15 MB まで）。");
            Add("subpost.files_hint",
                "Нові файли з'являться у списку вкладень підпоста.",
                "New files will appear in the sub-post attachments list.",
                "Neue Dateien erscheinen in der Anhangsliste des Unterbeitrags.",
                "新しいファイルはサブ投稿の添付一覧に表示されます。");
            Add("subpost.files_title",      "Файли підпоста",         "Sub-post files",        "Dateien des Unterbeitrags",        "サブ投稿のファイル");
            Add("subpost.files_empty",      "Файлів поки немає.",     "No files yet.",         "Noch keine Dateien.",              "まだファイルはありません。");
            Add("subpost.photos_title",     "Фото підпоста",          "Sub-post photos",       "Fotos des Unterbeitrags",          "サブ投稿の写真");
            Add("subpost.photos_empty",     "Фото поки немає.",       "No photos yet.",        "Noch keine Fotos.",                "まだ写真はありません。");
            Add("subpost.open_structure",   "Відкрити структуру",     "Open structure",        "Struktur öffnen",                  "構成を開く");
            Add("subpost.save",             "Зберегти",                "Save",                  "Speichern",                        "保存");
            Add("subpost.cancel",           "Скасувати",               "Cancel",                "Abbrechen",                        "キャンセル");
            Add("subpost.current_icon",     "Поточна іконка",         "Current icon",          "Aktuelles Symbol",                 "現在のアイコン");
            Add("subpost.remove_icon",      "Прибрати іконку",         "Remove icon",           "Symbol entfernen",                 "アイコンを削除");
            Add("subpost.new_section",      "Новий розділ сторінки",   "New page section",      "Neuer Seitenabschnitt",            "新しいページセクション");
            Add("subpost.create_intro",
                "Підпост стане окремою Fandom-сторінкою та карткою у структурі батьківського поста.",
                "The sub-post becomes a separate Fandom page and a card in the parent's structure.",
                "Der Unterbeitrag wird zu einer eigenen Fandom-Seite und einer Karte in der Struktur des übergeordneten Beitrags.",
                "サブ投稿は独立した Fandom ページとなり、親投稿の構成にカードとして表示されます。");
            Add("subpost.nested_in",         "Створюється як вкладений підпост", "Being created as a nested sub-post", "Wird als verschachtelter Unterbeitrag erstellt", "入れ子のサブ投稿として作成されます");
            Add("subpost.nested_in_prefix",  "у", "in", "in", "の中に");
            Add("subpost.title_placeholder", "Наприклад: Зброя, Персонажі, Локації", "For example: Weapons, Characters, Locations", "Zum Beispiel: Waffen, Charaktere, Orte", "例：武器、キャラクター、場所");
            Add("subpost.first_text_block",  "Перший текстовий блок", "First text block", "Erster Textblock", "最初のテキストブロック");
            Add("subpost.base_text_placeholder", "Основний текст підпоста…", "Main text of the sub-post…", "Haupttext des Unterbeitrags…", "サブ投稿の本文…");
            Add("subpost.icon_create_hint",
                "Значок картки цього підпоста в структурі. Не потрапляє у текст — окреме зображення.",
                "Card icon for this sub-post in the structure. It doesn't appear in the text — it's a separate image.",
                "Kartensymbol dieses Unterbeitrags in der Struktur. Es erscheint nicht im Text – es ist ein separates Bild.",
                "構成におけるこのサブ投稿のカードアイコン。本文には表示されない別の画像です。");
            Add("subpost.icon_fallback",
                "Якщо не вибрати — буде використано перше фото з тексту або заглушка.",
                "If none is chosen, the first photo from the text or a placeholder is used.",
                "Wenn keines gewählt wird, wird das erste Foto aus dem Text oder ein Platzhalter verwendet.",
                "選択しない場合は、本文の最初の写真またはプレースホルダーが使われます。");
            Add("subpost.photos_create_hint",
                "Вибрані зображення додадуться у структуру цього підпоста одразу після першого тексту.",
                "Selected images will be added to this sub-post's structure right after the first text.",
                "Ausgewählte Bilder werden direkt nach dem ersten Text in die Struktur dieses Unterbeitrags eingefügt.",
                "選択した画像は最初のテキストの直後にこのサブ投稿の構成に追加されます。");
            Add("subpost.photo_caption_short", "Спільний підпис до фото", "Shared photo caption", "Gemeinsame Bildunterschrift", "写真の共通キャプション");
            Add("subpost.icon_priority",
                "Якщо вище задано іконку, вона матиме пріоритет над мініатюрою.",
                "If an icon is set above, it takes priority over the thumbnail.",
                "Wenn oben ein Symbol gesetzt ist, hat es Vorrang vor dem Miniaturbild.",
                "上でアイコンを指定した場合、サムネイルより優先されます。");
            Add("subpost.files_create_hint",
                "Додайте матеріали для завантаження: PDF, TXT, ZIP, DOCX, XLSX, PNG або JPG.",
                "Attach materials for download: PDF, TXT, ZIP, DOCX, XLSX, PNG or JPG.",
                "Füge Materialien zum Herunterladen hinzu: PDF, TXT, ZIP, DOCX, XLSX, PNG oder JPG.",
                "ダウンロード用の資料を追加：PDF・TXT・ZIP・DOCX・XLSX・PNG・JPG。");
            Add("subpost.files_max_size", "Максимальний розмір одного файлу — 15 МБ.", "Maximum file size — 15 MB.", "Maximale Dateigröße — 15 MB.", "1 ファイルの最大サイズは 15 MB です。");
            Add("subpost.create_button",  "Створити", "Create", "Erstellen", "作成");

            // ── Common ────────────────────────────────────────────────────────
            Add("common.select",           "Виділити",         "Select",             "Auswählen",                 "選択");
            Add("common.file_not_chosen",  "Файл не вибрано.", "No file chosen.",    "Keine Datei ausgewählt.",   "ファイルが選択されていません。");
            Add("common.files_not_chosen", "Файли не вибрано.","No files chosen.",   "Keine Dateien ausgewählt.", "ファイルが選択されていません。");
            Add("common.browse",           "Огляд…",            "Browse…",            "Durchsuchen…",              "参照…");
        }
    }
}
