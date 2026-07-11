namespace Fandom_copy.DTOs.Home
{
    public class HomeCardDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? CoverImagePath { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class HomeCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PostCount { get; set; }
        public string? CoverImagePath { get; set; }
    }

    public class HomeViewModel
    {
        /// <summary>
        /// "Продовжити перегляд" — the user's saved posts, falling back
        /// to the most recently updated posts for guests / users with nothing saved.
        /// </summary>
        public List<HomeCardDto> JumpBackIn { get; set; } = new();
        public bool JumpBackInIsSaved { get; set; }

        /// <summary>Latest published posts, shown as the "Recent" strip of circles.</summary>
        public List<HomeCardDto> RecentPosts { get; set; } = new();

        /// <summary>Categories to discover, with a post count and cover image.</summary>
        public List<HomeCategoryDto> Categories { get; set; } = new();
    }
}
