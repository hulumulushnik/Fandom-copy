using Fandom_copy.Models;

namespace Fandom_copy.DTOs.Posts
{
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

        public static PostContentBlockDto FromEntity(PostContentBlock block) => new()
        {
            Id = block.Id,
            Type = block.Type,
            Text = block.Text,
            Order = block.Order,
            SectionId = block.SectionId,
            SectionTitle = block.Section?.Title,
            ImagePath = block.ImagePath,
            ImageCaption = block.ImageCaption
        };
    }
}
