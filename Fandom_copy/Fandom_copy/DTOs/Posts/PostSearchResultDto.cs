namespace Fandom_copy.DTOs.Posts
{
    /// <summary>
    /// A single row in the site-wide post search: the post itself plus,
    /// if the match came from inside its structure, the sub-section that matched.
    /// </summary>
    public class PostSearchResultDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public bool IsPublic { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Snippet { get; set; } = string.Empty;

        public Guid? MatchedSectionId { get; set; }
        public string? MatchedSectionTitle { get; set; }
        public string MatchedIn { get; set; } = "Title";
    }
}
