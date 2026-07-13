namespace Fandom_copy.Models
{
    public enum PostContentBlockType
    {
        Text = 0,
        Section = 1,
        Image = 2,
        Template = 3,
        Gallery = 4
    }

    /// <summary>
    /// Predefined text-formatting size levels used by the rich editor.
    /// The renderer maps these to CSS classes, so no raw HTML is stored.
    /// </summary>
    public enum PostTextSize
    {
        Normal = 0,
        Small = 1,
        Large = 2,
        Heading = 3
    }

    /// <summary>Text alignment options for text blocks.</summary>
    public enum PostTextAlign
    {
        Left = 0,
        Center = 1,
        Right = 2
    }

    /// <summary>
    /// Extra rendering styles applied to text blocks that behave as lists or quotes.
    /// Kept as separate flag rather than raw HTML for safety.
    /// </summary>
    public enum PostTextStyle
    {
        Paragraph = 0,
        BulletList = 1,
        NumberedList = 2,
        Quote = 3
    }

    /// <summary>How a linked sub-section (Section-type block) is rendered.</summary>
    public enum PostSectionDisplayStyle
    {
        CardWithIcon = 0,
        CenteredCard = 1,
        LeftCard = 2,
        RightCard = 3,
        TextLink = 4,
        CompactRow = 5
    }

    /// <summary>Known template kinds that render special styled text blocks.</summary>
    public enum PostBlockTemplateType
    {
        None = 0,
        InfoBox = 1,
        Warning = 2,
        Quote = 3,
        Divider = 4,
        FactCard = 5,
        LoreBlock = 6,
        CharacterStats = 7
    }

    /// <summary>Layout style for a gallery block.</summary>
    public enum PostGalleryStyle
    {
        Grid = 0,
        Masonry = 1,
        Carousel = 2,
        Strip = 3
    }

    // A block belongs either to the post itself or to one of its sections.
    // Keeping the order here allows text and section links to be interleaved.
    public class PostContentBlock
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Guid? ContainerSectionId { get; set; }
        public PostContentBlockType Type { get; set; }

        // Text-related fields (used by Text, Template, and gallery caption).
        public string Text { get; set; } = string.Empty;
        public bool TextBold { get; set; }
        public bool TextItalic { get; set; }
        public bool TextUnderline { get; set; }
        public bool TextStrike { get; set; }
        public PostTextSize TextSize { get; set; } = PostTextSize.Normal;
        public PostTextAlign TextAlign { get; set; } = PostTextAlign.Left;
        public PostTextStyle TextStyle { get; set; } = PostTextStyle.Paragraph;
        public string TextColor { get; set; } = string.Empty; // whitelist-checked short color name

        // Image single-block fields.
        public string ImagePath { get; set; } = string.Empty;
        public string ImageCaption { get; set; } = string.Empty;

        // Linked sub-post fields.
        public Guid? SectionId { get; set; }
        public PostSection? Section { get; set; }
        public PostSectionDisplayStyle SectionDisplayStyle { get; set; } = PostSectionDisplayStyle.CardWithIcon;
        public string SectionLinkText { get; set; } = string.Empty;

        // Template block metadata.
        public PostBlockTemplateType TemplateType { get; set; } = PostBlockTemplateType.None;

        // Gallery metadata (images stored in PostGalleryImages).
        public PostGalleryStyle GalleryStyle { get; set; } = PostGalleryStyle.Grid;
        public string GalleryCaption { get; set; } = string.Empty;
        public List<PostGalleryImage> GalleryImages { get; set; } = new();

        public int Order { get; set; }
    }
}
