using System.ComponentModel.DataAnnotations;
using Fandom_copy.Models;

namespace Fandom_copy.DTOs.Posts
{
    public class PostContentEditorDto
    {
        public Guid PostId { get; set; }
        public Guid? ContainerSectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool CanEdit { get; set; }
        public List<PostContentBlockDto> Blocks { get; set; } = new();
        public List<PostSectionPickerDto> AvailableSections { get; set; } = new();

        [StringLength(20000)]
        public string NewText { get; set; } = string.Empty;

        [StringLength(240)]
        public string NewImageCaption { get; set; } = string.Empty;

        // New text-formatting inputs (used by AddText).
        public bool NewTextBold { get; set; }
        public bool NewTextItalic { get; set; }
        public bool NewTextUnderline { get; set; }
        public bool NewTextStrike { get; set; }
        public PostTextSize NewTextSize { get; set; } = PostTextSize.Normal;
        public PostTextAlign NewTextAlign { get; set; } = PostTextAlign.Left;
        public PostTextStyle NewTextStyle { get; set; } = PostTextStyle.Paragraph;
        [StringLength(32)]
        public string NewTextColor { get; set; } = string.Empty;

        // Sub-section display picker (for AddExistingSection).
        public PostSectionDisplayStyle NewSectionDisplayStyle { get; set; } = PostSectionDisplayStyle.CardWithIcon;
        [StringLength(240)]
        public string NewSectionLinkText { get; set; } = string.Empty;

        // Gallery inputs.
        [StringLength(240)]
        public string NewGalleryCaption { get; set; } = string.Empty;
        public PostGalleryStyle NewGalleryStyle { get; set; } = PostGalleryStyle.Grid;
    }

    public class PostSectionPickerDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>Payload for the drag-and-drop reorder endpoint.</summary>
    public class ReorderBlocksDto
    {
        public List<Guid> OrderedIds { get; set; } = new();
    }
}
