using static System.Net.Mime.MediaTypeNames;

namespace Fandom_copy.Models
{
    public class PostSection
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }

        public Guid PostId { get; set; }
        public Post Post { get; set; } = null!;

        /// <summary>
        /// Optional custom icon/thumbnail for this sub-post, set independently from
        /// any images inserted into its content. When empty, the UI falls back to
        /// the first content image and then to a placeholder.
        /// </summary>
        public string? IconPath { get; set; }
        public Guid? ParentSectionId { get; set; }
        public PostSection? ParentSection { get; set; }
        public List<PostSection> SubSections { get; set; } = new();
        public List<Images> Images { get; set; } = new();
        public List<FileAttachment> Files { get; set; } = new();
        public List<CodeBlock> CodeBlocks { get; set; } = new();
    }
}
