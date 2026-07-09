namespace Fandom_copy.Models
{
    public class Images
    {
        public Guid Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }

        // Зв'язок
        public Guid PostSectionId { get; set; }
        public PostSection PostSection { get; set; } = null!;
    }
}
