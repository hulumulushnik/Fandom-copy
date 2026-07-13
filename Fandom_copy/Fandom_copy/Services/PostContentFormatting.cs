using Fandom_copy.Models;

namespace Fandom_copy.Services;

/// <summary>
/// Whitelist helpers for rich-text and template rendering.
/// Any user-supplied formatting value passes through this helper so
/// no raw HTML, JavaScript URL, or unknown class ever reaches the view.
/// </summary>
public static class PostContentFormatting
{
    // Small palette – names are used as CSS class suffixes, never emitted as raw HTML.
    public static readonly IReadOnlyList<string> AllowedTextColors = new[]
    {
        "default", "muted", "accent", "primary", "success", "warning", "danger", "info"
    };

    public static string SanitizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return string.Empty;
        var normalized = color.Trim().ToLowerInvariant();
        return AllowedTextColors.Contains(normalized) ? normalized : string.Empty;
    }

    public static string TextSizeClass(PostTextSize size) => size switch
    {
        PostTextSize.Small => "wiki-text-size-small",
        PostTextSize.Large => "wiki-text-size-large",
        PostTextSize.Heading => "wiki-text-size-heading",
        _ => "wiki-text-size-normal"
    };

    public static string TextAlignClass(PostTextAlign align) => align switch
    {
        PostTextAlign.Center => "wiki-text-align-center",
        PostTextAlign.Right => "wiki-text-align-right",
        _ => "wiki-text-align-left"
    };

    public static string TextStyleClass(PostTextStyle style) => style switch
    {
        PostTextStyle.BulletList => "wiki-text-style-bullet",
        PostTextStyle.NumberedList => "wiki-text-style-number",
        PostTextStyle.Quote => "wiki-text-style-quote",
        _ => "wiki-text-style-paragraph"
    };

    public static string TextColorClass(string color)
    {
        var safe = SanitizeColor(color);
        return string.IsNullOrEmpty(safe) ? string.Empty : $"wiki-text-color-{safe}";
    }

    public static string TemplateClass(PostBlockTemplateType type) => type switch
    {
        PostBlockTemplateType.InfoBox => "wiki-template wiki-template-infobox",
        PostBlockTemplateType.Warning => "wiki-template wiki-template-warning",
        PostBlockTemplateType.Quote => "wiki-template wiki-template-quote",
        PostBlockTemplateType.Divider => "wiki-template wiki-template-divider",
        PostBlockTemplateType.FactCard => "wiki-template wiki-template-fact",
        PostBlockTemplateType.LoreBlock => "wiki-template wiki-template-lore",
        PostBlockTemplateType.CharacterStats => "wiki-template wiki-template-stats",
        _ => "wiki-template"
    };

    public static string TemplateLabel(PostBlockTemplateType type) => type switch
    {
        PostBlockTemplateType.InfoBox => "Инфо",
        PostBlockTemplateType.Warning => "Внимание",
        PostBlockTemplateType.Quote => "Цитата",
        PostBlockTemplateType.Divider => "Разделитель",
        PostBlockTemplateType.FactCard => "Факт",
        PostBlockTemplateType.LoreBlock => "Лор",
        PostBlockTemplateType.CharacterStats => "Характеристики",
        _ => "Шаблон"
    };

    public static string GalleryClass(PostGalleryStyle style) => style switch
    {
        PostGalleryStyle.Masonry => "wiki-gallery wiki-gallery-masonry",
        PostGalleryStyle.Carousel => "wiki-gallery wiki-gallery-carousel",
        PostGalleryStyle.Strip => "wiki-gallery wiki-gallery-strip",
        _ => "wiki-gallery wiki-gallery-grid"
    };

    public static string SectionDisplayClass(PostSectionDisplayStyle style) => style switch
    {
        PostSectionDisplayStyle.CenteredCard => "wiki-topic-card wiki-topic-centered",
        PostSectionDisplayStyle.LeftCard => "wiki-topic-card wiki-topic-left",
        PostSectionDisplayStyle.RightCard => "wiki-topic-card wiki-topic-right",
        PostSectionDisplayStyle.TextLink => "wiki-topic-textlink",
        PostSectionDisplayStyle.CompactRow => "wiki-topic-compact",
        _ => "wiki-topic-card"
    };
}
