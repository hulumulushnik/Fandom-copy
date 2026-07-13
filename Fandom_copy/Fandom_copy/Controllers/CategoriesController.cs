using System.Security.Claims;
using Fandom_copy.Data;
using Fandom_copy.DTOs.Categories;
using Fandom_copy.DTOs.Posts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Controllers
{
    [Route("categories")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET /categories
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var categories = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryListItemDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    PostCount = c.Posts.Count(p => !p.IsDeleted && p.IsPublic)
                })
                .ToListAsync();

            return View(categories);
        }

        // GET /categories/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category is null)
            {
                TempData["Error"] = "Категорію не знайдено";
                return RedirectToAction(nameof(Index));
            }

            var userId = TryGetCurrentUserId();

            var visible = _db.Posts
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted && p.CategoryId == id);

            if (userId is null)
            {
                visible = visible.Where(p => p.IsPublic);
            }
            else
            {
                var uid = userId.Value;
                visible = visible.Where(p =>
                    p.IsPublic || _db.PostMembers.Any(m => m.PostId == p.Id && m.UserId == uid));
            }

            var posts = await visible
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();

            var dto = new CategoryDetailsDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Posts = posts.Select(p => PostDto.FromEntity(p)).ToList()
            };

            return View(dto);
        }

        private Guid? TryGetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }
}
