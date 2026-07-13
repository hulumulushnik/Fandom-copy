namespace Fandom_copy.Models
{
    /// <summary>
    /// Image belonging to a <see cref="PostContentBlockType.Gallery"/> block.
    /// Stored in a separate table so images can be reordered inside the gallery
    /// without touching the parent block row and so that version snapshots can
    /// capture the whole gallery cleanly.
    /// </summary>
    public class PostGalleryImage
    {
        public Guid Id { get; set; }
        public Guid PostContentBlockId { get; set; }
        public PostContentBlock? Block { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
