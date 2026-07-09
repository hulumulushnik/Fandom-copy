namespace Fandom_copy.Models
{
    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Зв'язок
        public List<Post> Posts { get; set; } = new();
    }
}
