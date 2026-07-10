using System.ComponentModel.DataAnnotations;

namespace Fandom_copy.DTOs.Posts
{
    public class UpdatePostSectionDto
    {
        [Required(ErrorMessage = "Заголовок обов'язковий")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Заголовок має бути від 1 до 200 символів")]
        public string Title { get; set; } = string.Empty;

        [StringLength(20000, ErrorMessage = "Текст не може перевищувати 20000 символів")]
        public string Text { get; set; } = string.Empty;

        public int Order { get; set; }
    }
}
