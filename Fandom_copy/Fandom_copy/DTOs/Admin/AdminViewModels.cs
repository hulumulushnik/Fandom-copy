using System.ComponentModel.DataAnnotations;
using Fandom_copy.Models;

namespace Fandom_copy.DTOs.Admin
{
    public class AdminDashboardDto
    {
        public int UserCount { get; set; }
        public int PostCount { get; set; }
        public int CategoryCount { get; set; }
    }

    public class AdminUserListItemDto
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public GlobalRole GlobalRole { get; set; }
        public bool IsBanned { get; set; }
        public DateTime RegistrationDate { get; set; }
    }

    public class AdminCategoryListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PostCount { get; set; }
    }

    public class AdminCategoryFormDto
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Вкажіть назву категорії")]
        [StringLength(120, ErrorMessage = "Назва має бути не довшою за 120 символів")]
        public string Name { get; set; } = string.Empty;

        [StringLength(600, ErrorMessage = "Опис має бути не довшим за 600 символів")]
        public string Description { get; set; } = string.Empty;
    }

    public class AdminCategoriesViewModel
    {
        public List<AdminCategoryListItemDto> Categories { get; set; } = new();
        public AdminCategoryFormDto Form { get; set; } = new();
    }

    public class AdminPostListItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? OwnerLogin { get; set; }
        public bool IsPublic { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
