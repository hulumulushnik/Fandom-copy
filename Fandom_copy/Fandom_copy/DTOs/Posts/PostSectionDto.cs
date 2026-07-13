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
        public string? PrimaryImagePath { get; set; }
        public string? IconPath { get; set; }

        /// <summary>Direct children of this section (one level deep by default).</summary>
        public List<PostSectionDto> SubSections { get; set; } = new();
        public List<PostContentBlockDto> ContentBlocks { get; set; } = new();
        public List<FileAttachmentDto> Attachments { get; set; } = new();

        /// <summary>
        /// Breadcrumb from the root of the post down to (but not including) the current section.
        /// Populated only when returning a single section for its detail page.
        /// </summary>
        public List<PostSectionBreadcrumbDto> Breadcrumbs { get; set; } = new();

        public static PostSectionDto FromEntity(PostSection section, bool includeSubSections = true)
        {
            return new PostSectionDto
            {
                Id = section.Id,
                Title = section.Title,
                Text = section.Text,
                Order = section.Order,
                PostId = section.PostId,
                ParentSectionId = section.ParentSectionId,
                PrimaryImagePath = null,
                IconPath = section.IconPath,
                Attachments = section.Files != null
                    ? section.Files
                        .OrderBy(f => f.FileName)
                        .Select(FileAttachmentDto.FromEntity)
                        .ToList()
                    : new List<FileAttachmentDto>(),
                SubSections = includeSubSections && section.SubSections != null
                    ? section.SubSections
                        .OrderBy(s => s.Order)
                        .Select(s => FromEntity(s, includeSubSections: false))
                        .ToList()
                    : new List<PostSectionDto>()
            };
        }
    }

    public class PostSectionBreadcrumbDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class FileAttachmentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }

        public static FileAttachmentDto FromEntity(FileAttachment attachment)
        {
            return new FileAttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                Path = attachment.Path,
                Size = attachment.Size
            };
        }
    }
}
