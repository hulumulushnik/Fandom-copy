using System.Security.Claims;
using Fandom_copy.Data;
using Fandom_copy.DTOs.Admin;
using Fandom_copy.Models;
using Fandom_copy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IUserService _users;
        private readonly IPostService _posts;

        public AdminController(ApplicationDbContext db, IUserService users, IPostService posts)
        {
            _db = db;
            _users = users;
            _posts = posts;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardDto
            {
                UserCount = await _db.Users.CountAsync(),
                PostCount = await _db.Posts.CountAsync(),
                CategoryCount = await _db.Categories.CountAsync()
            };

            return View(model);
        }

        [HttpGet("users")]
        public async Task<IActionResult> Users()
        {
            var users = await _db.Users
                .OrderByDescending(u => u.RegistrationDate)
                .Select(u => new AdminUserListItemDto
                {
                    Id = u.Id,
                    Login = u.Login,
                    Email = u.Email,
                    GlobalRole = u.GlobalRole,
                    IsBanned = u.IsBanned,
                    RegistrationDate = u.RegistrationDate
                })
                .ToListAsync();

            return View(users);
        }

        [HttpPost("users/{id:guid}/ban")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBan(Guid id)
        {
            if (id == CurrentUserId())
            {
                TempData["Error"] = "Не можна заблокувати власний акаунт.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
            {
                TempData["Error"] = "Користувача не знайдено.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _users.SetBanStatusAsync(id, !user.IsBanned);
            if (!result.Success)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Users));
            }

            TempData["Message"] = user.IsBanned ? "Користувача розбанено." : "Користувача забанено.";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet("categories")]
        public async Task<IActionResult> Categories()
        {
            return View(await BuildCategoriesViewModelAsync(new AdminCategoryFormDto()));
        }

        [HttpPost("categories/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory([Bind(Prefix = "Form")] AdminCategoryFormDto form)
        {
            if (!ModelState.IsValid)
                return View("Categories", await BuildCategoriesViewModelAsync(form));

            var name = form.Name.Trim();
            var exists = await _db.Categories.AnyAsync(c => c.Name == name);
            if (exists)
            {
                ModelState.AddModelError("Form.Name", "Категорія з такою назвою вже існує.");
                return View("Categories", await BuildCategoriesViewModelAsync(form));
            }

            _db.Categories.Add(new Category
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = form.Description?.Trim() ?? string.Empty
            });

            await _db.SaveChangesAsync();
            TempData["Message"] = "Категорію створено.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet("categories/{id:guid}/edit")]
        public async Task<IActionResult> EditCategory(Guid id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category is null)
            {
                TempData["Error"] = "Категорію не знайдено.";
                return RedirectToAction(nameof(Categories));
            }

            return View(new AdminCategoryFormDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            });
        }

        [HttpPost("categories/{id:guid}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(Guid id, AdminCategoryFormDto form)
        {
            form.Id = id;
            if (!ModelState.IsValid)
                return View(form);

            var category = await _db.Categories.FindAsync(id);
            if (category is null)
            {
                TempData["Error"] = "Категорію не знайдено.";
                return RedirectToAction(nameof(Categories));
            }

            var name = form.Name.Trim();
            var exists = await _db.Categories.AnyAsync(c => c.Id != id && c.Name == name);
            if (exists)
            {
                ModelState.AddModelError(nameof(form.Name), "Категорія з такою назвою вже існує.");
                return View(form);
            }

            category.Name = name;
            category.Description = form.Description?.Trim() ?? string.Empty;
            await _db.SaveChangesAsync();

            TempData["Message"] = "Категорію оновлено.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost("categories/{id:guid}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category is null)
            {
                TempData["Error"] = "Категорію не знайдено.";
                return RedirectToAction(nameof(Categories));
            }

            var hasPosts = await _db.Posts.AnyAsync(p => p.CategoryId == id);
            if (hasPosts)
            {
                TempData["Error"] = "Не можна видалити категорію, у якій є пости.";
                return RedirectToAction(nameof(Categories));
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Категорію видалено.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet("posts")]
        public async Task<IActionResult> Posts()
        {
            var posts = await _db.Posts
                .OrderByDescending(p => p.UpdatedAt)
                .Select(p => new AdminPostListItemDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    CategoryName = p.Category.Name,
                    OwnerLogin = p.Members
                        .Where(m => m.Role == PostRole.Owner)
                        .Select(m => m.User.Login)
                        .FirstOrDefault(),
                    IsPublic = p.IsPublic,
                    IsDeleted = p.IsDeleted,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return View(posts);
        }

        [HttpPost("posts/{id:guid}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(Guid id)
        {
            var result = await _posts.DeleteAsAdminAsync(id, CurrentUserId());
            if (!result.Success)
                TempData["Error"] = result.Error;
            else
                TempData["Message"] = "Пост видалено.";

            return RedirectToAction(nameof(Posts));
        }

        private async Task<AdminCategoriesViewModel> BuildCategoriesViewModelAsync(AdminCategoryFormDto form)
        {
            var categories = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new AdminCategoryListItemDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    PostCount = c.Posts.Count()
                })
                .ToListAsync();

            return new AdminCategoriesViewModel
            {
                Categories = categories,
                Form = form
            };
        }

        private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
