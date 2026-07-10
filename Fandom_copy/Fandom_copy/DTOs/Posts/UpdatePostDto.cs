using System.ComponentModel.DataAnnotations;

namespace Fandom_copy.DTOs.Posts
{
    public class UpdatePostDto
    {
        [Required(ErrorMessage = "Назва обов'язкова")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Назва має бути від 3 до 200 символів")]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000, ErrorMessage = "Опис не може перевищувати 4000 символів")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Категорія обов'язкова")]
        public Guid CategoryId { get; set; }

        public bool IsPublic { get; set; }
    }
}
