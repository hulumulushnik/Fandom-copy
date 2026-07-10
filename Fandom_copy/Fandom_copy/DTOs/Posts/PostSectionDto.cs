using Fandom_copy.Models;

namespace Fandom_copy.DTOs.Posts
{
    public class PostSectionDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
        public Guid PostId { get; set; }
        public Guid? ParentSectionId { get; set; }
        public List<PostSectionDto> SubSections { get; set; } = new();

        public static PostSectionDto FromEntity(PostSection section)
        {
            return new PostSectionDto
            {
                Id = section.Id,
                Title = section.Title,
                Text = section.Text,
                Order = section.Order,
                PostId = section.PostId,
                ParentSectionId = section.ParentSectionId,
                SubSections = section.SubSections?
                    .OrderBy(s => s.Order)
                    .Select(FromEntity)
                    .ToList() ?? new List<PostSectionDto>()
            };
        }
    }
}
