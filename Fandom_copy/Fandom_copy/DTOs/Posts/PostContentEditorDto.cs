using System.ComponentModel.DataAnnotations;

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
    }

    public class PostSectionPickerDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}
