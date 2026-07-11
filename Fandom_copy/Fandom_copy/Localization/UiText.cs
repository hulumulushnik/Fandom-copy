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
            ("de", "Deutsch"),
            ("ja", "日本語"),
        };

        public static readonly (string Code, string Label)[] SupportedThemes =
        {
            ("dark", "Темна"),
            ("light", "Світла"),
            ("sunset", "Sunset"),
        };

        private static readonly Dictionary<string, Dictionary<string, string>> ThemeLabels = new()
        {
            ["uk"] = new() { ["dark"] = "Темна", ["light"] = "Світла", ["sunset"] = "Sunset" },
            ["en"] = new() { ["dark"] = "Dark", ["light"] = "Light", ["sunset"] = "Sunset" },
            ["de"] = new() { ["dark"] = "Dunkel", ["light"] = "Hell", ["sunset"] = "Sunset" },
            ["ja"] = new() { ["dark"] = "ダーク", ["light"] = "ライト", ["sunset"] = "サンセット" },
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
            ["uk"] = new()
            {
                ["nav.search.placeholder"] = "Пошук постів, категорій, розділів...",
                ["nav.login"] = "Увійти",
                ["nav.register"] = "Реєстрація",
                ["nav.logout"] = "Вийти",
                ["sidebar.home"] = "Головна",
                ["sidebar.posts"] = "Пости",
                ["sidebar.saved"] = "Збережено",
                ["sidebar.rules"] = "Правила",
                ["sidebar.settings"] = "Налаштування",
                ["footer.rights"] = "Усі права захищено",
                ["settings.kicker"] = "Персоналізація",
                ["settings.title"] = "Налаштування",
                ["settings.intro"] = "Оберіть мову інтерфейсу та тему оформлення сайту.",
                ["settings.lang.title"] = "Мова сайту",
                ["settings.lang.hint"] = "Впливає на текст меню, кнопок та підписів інтерфейсу.",
                ["settings.theme.title"] = "Тема сайту",
                ["settings.theme.hint"] = "Кольорова схема, у якій відображається весь сайт.",
                ["settings.save"] = "Зберегти налаштування",
                ["settings.saved"] = "Налаштування збережено.",
            },
            ["en"] = new()
            {
                ["nav.search.placeholder"] = "Search posts, categories, sections...",
                ["nav.login"] = "Log in",
                ["nav.register"] = "Sign up",
                ["nav.logout"] = "Log out",
                ["sidebar.home"] = "Home",
                ["sidebar.posts"] = "Posts",
                ["sidebar.saved"] = "Saved",
                ["sidebar.rules"] = "Rules",
                ["sidebar.settings"] = "Settings",
                ["footer.rights"] = "All rights reserved",
                ["settings.kicker"] = "Personalization",
                ["settings.title"] = "Settings",
                ["settings.intro"] = "Choose the interface language and the site's color theme.",
                ["settings.lang.title"] = "Site language",
                ["settings.lang.hint"] = "Affects the text of the menu, buttons, and interface labels.",
                ["settings.theme.title"] = "Site theme",
                ["settings.theme.hint"] = "The color scheme used across the whole site.",
                ["settings.save"] = "Save settings",
                ["settings.saved"] = "Settings saved.",
            },
            ["de"] = new()
            {
                ["nav.search.placeholder"] = "Beiträge, Kategorien, Abschnitte durchsuchen...",
                ["nav.login"] = "Anmelden",
                ["nav.register"] = "Registrieren",
                ["nav.logout"] = "Abmelden",
                ["sidebar.home"] = "Startseite",
                ["sidebar.posts"] = "Beiträge",
                ["sidebar.saved"] = "Gespeichert",
                ["sidebar.rules"] = "Regeln",
                ["sidebar.settings"] = "Einstellungen",
                ["footer.rights"] = "Alle Rechte vorbehalten",
                ["settings.kicker"] = "Personalisierung",
                ["settings.title"] = "Einstellungen",
                ["settings.intro"] = "Wähle die Interfacesprache und das Farbthema der Seite.",
                ["settings.lang.title"] = "Sprache der Seite",
                ["settings.lang.hint"] = "Wirkt sich auf Menü-, Button- und Beschriftungstexte aus.",
                ["settings.theme.title"] = "Thema der Seite",
                ["settings.theme.hint"] = "Das Farbschema, in dem die gesamte Seite angezeigt wird.",
                ["settings.save"] = "Einstellungen speichern",
                ["settings.saved"] = "Einstellungen gespeichert.",
            },
            ["ja"] = new()
            {
                ["nav.search.placeholder"] = "投稿、カテゴリー、セクションを検索...",
                ["nav.login"] = "ログイン",
                ["nav.register"] = "新規登録",
                ["nav.logout"] = "ログアウト",
                ["sidebar.home"] = "ホーム",
                ["sidebar.posts"] = "投稿",
                ["sidebar.saved"] = "保存済み",
                ["sidebar.rules"] = "ルール",
                ["sidebar.settings"] = "設定",
                ["footer.rights"] = "全著作権所有",
                ["settings.kicker"] = "パーソナライズ",
                ["settings.title"] = "設定",
                ["settings.intro"] = "インターフェースの言語とサイトのカラーテーマを選択してください。",
                ["settings.lang.title"] = "サイトの言語",
                ["settings.lang.hint"] = "メニュー、ボタン、インターフェースのラベルの表示に影響します。",
                ["settings.theme.title"] = "サイトのテーマ",
                ["settings.theme.hint"] = "サイト全体に適用される配色です。",
                ["settings.save"] = "設定を保存",
                ["settings.saved"] = "設定を保存しました。",
            },
        };

        public static string Normalize(string? lang)
        {
            if (string.IsNullOrWhiteSpace(lang) || !Strings.ContainsKey(lang))
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
    }
}
