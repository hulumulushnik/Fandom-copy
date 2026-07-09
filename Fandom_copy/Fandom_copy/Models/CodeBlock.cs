namespace Fandom_copy.Models
{
    public class CodeBlock
    {
        public Guid Id { get; set; }
        public string Language { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        // Зв'язок
        public Guid PostSectionId { get; set; }
        public PostSection PostSection { get; set; } = null!;
    }
}
