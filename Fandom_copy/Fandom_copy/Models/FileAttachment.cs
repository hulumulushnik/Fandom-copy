namespace Fandom_copy.Models
{
    public class FileAttachment
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }

        // Зв'язок
        public Guid PostSectionId { get; set; }
        public PostSection PostSection { get; set; } = null!;
    }
}
