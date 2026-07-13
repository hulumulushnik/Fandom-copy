using Fandom_copy.Models;

namespace Fandom_copy.DTOs.Posts
{
    public class PostGalleryImageDto
    {
        public Guid Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public int Order { get; set; }

        public static PostGalleryImageDto FromEntity(PostGalleryImage img) => new()
        {
            Id = img.Id,
            ImagePath = img.ImagePath,
            Caption = img.Caption,
            Order = img.Order
        };
    }

    public class PostContentBlockDto
    {
        public Guid Id { get; set; }
        public PostContentBlockType Type { get; set; }
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
        public Guid? SectionId { get; set; }
        public string? SectionTitle { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string ImageCaption { get; set; } = string.Empty;

        // Rich-text formatting.
        public bool TextBold { get; set; }
        public bool TextItalic { get; set; }
        public bool TextUnderline { get; set; }
        public bool TextStrike { get; set; }
        public PostTextSize TextSize { get; set; }
        public PostTextAlign TextAlign { get; set; }
        public PostTextStyle TextStyle { get; set; }
        public string TextColor { get; set; } = string.Empty;

        // Sub-post display.
        public PostSectionDisplayStyle SectionDisplayStyle { get; set; }
        public string SectionLinkText { get; set; } = string.Empty;
        public string? SectionIconPath { get; set; }

        // Templates.
        public PostBlockTemplateType TemplateType { get; set; }

        // Gallery.
        public PostGalleryStyle GalleryStyle { get; set; }
        public string GalleryCaption { get; set; } = string.Empty;
        public List<PostGalleryImageDto> GalleryImages { get; set; } = new();

        public static PostContentBlockDto FromEntity(PostContentBlock block) => new()
        {
            Id = block.Id,
            Type = block.Type,
            Text = block.Text,
            Order = block.Order,
            SectionId = block.SectionId,
            SectionTitle = block.Section?.Title,
            SectionIconPath = block.Section?.IconPath,
            ImagePath = block.ImagePath,
            ImageCaption = block.ImageCaption,
            TextBold = block.TextBold,
            TextItalic = block.TextItalic,
            TextUnderline = block.TextUnderline,
            TextStrike = block.TextStrike,
            TextSize = block.TextSize,
            TextAlign = block.TextAlign,
            TextStyle = block.TextStyle,
            TextColor = block.TextColor,
            SectionDisplayStyle = block.SectionDisplayStyle,
            SectionLinkText = block.SectionLinkText,
            TemplateType = block.TemplateType,
            GalleryStyle = block.GalleryStyle,
            GalleryCaption = block.GalleryCaption,
            GalleryImages = (block.GalleryImages ?? new List<PostGalleryImage>())
                .OrderBy(g => g.Order)
                .Select(PostGalleryImageDto.FromEntity)
                .ToList()
        };
    }
}
