using Fandom_copy.DTOs.Posts;

namespace Fandom_copy.Views.Posts
{
    /// <summary>Small view-model shared between Posts/Details and PostSections/Details for rendering content blocks.</summary>
    public class ContentBlocksModel
    {
        public List<PostContentBlockDto> Blocks { get; set; } = new();
        public Dictionary<Guid, PostSectionDto> SectionLookup { get; set; } = new();
        public Guid PostId { get; set; }
        public string FallbackTitle { get; set; } = string.Empty;
    }
}
