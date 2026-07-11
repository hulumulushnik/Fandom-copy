namespace Fandom_copy.Models
{
    public enum PostContentBlockType
    {
        Text = 0,
        Section = 1,
        Image = 2
    }

    // A block belongs either to the post itself or to one of its sections.
    // Keeping the order here allows text and section links to be interleaved.
    public class PostContentBlock
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Guid? ContainerSectionId { get; set; }
        public PostContentBlockType Type { get; set; }
        public string Text { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string ImageCaption { get; set; } = string.Empty;
        public Guid? SectionId { get; set; }
        public PostSection? Section { get; set; }
        public int Order { get; set; }
    }
}
