namespace Fandom_copy.Models
{
    public class Tag
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Зв'язок
        public List<Post> Posts { get; set; } = new();
    }
}
