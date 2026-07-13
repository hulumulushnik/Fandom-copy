namespace Fandom_copy.DTOs.Categories
{
    /// <summary>
    /// A single row on the "/categories" listing page: the category plus the
    /// number of publicly visible, non-deleted posts inside it.
    /// </summary>
    public class CategoryListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PostCount { get; set; }
    }
}
