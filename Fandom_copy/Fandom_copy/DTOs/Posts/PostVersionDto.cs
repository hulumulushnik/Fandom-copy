using Fandom_copy.Models;

namespace Fandom_copy.DTOs.Posts
{
    public class PostVersionDto
    {
        public Guid Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string UserLogin { get; set; } = string.Empty;

        public static PostVersionDto FromEntity(PostVersion version)
        {
            return new PostVersionDto
            {
                Id = version.Id,
                Action = version.Action,
                CreatedAt = version.CreatedAt,
                UserLogin = version.User?.Login ?? "Unknown"
            };
        }
    }
}
