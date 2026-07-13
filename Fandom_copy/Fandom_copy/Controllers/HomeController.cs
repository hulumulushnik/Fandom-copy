using System.Diagnostics;
using System.Security.Claims;
using Fandom_copy.Data;
using Fandom_copy.DTOs.Home;
using Fandom_copy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var userId = TryGetCurrentUserId();
            var vm = new HomeViewModel();

            var visiblePosts = _db.Posts.Where(p => !p.IsDeleted);
            if (userId is null)
            {
                visiblePosts = visiblePosts.Where(p => p.IsPublic);
            }
            else
            {
                var uid = userId.Value;
                visiblePosts = visiblePosts.Where(p =>
                    p.IsPublic || _db.PostMembers.Any(m => m.PostId == p.Id && m.UserId == uid));
            }

            // "Продовжити перегляд" — the signed-in user's saved posts, most recent first.
            if (userId is not null)
            {
                var uid = userId.Value;
                var savedIds = await _db.SavedPosts
                    .Where(s => s.UserId == uid)
                    .OrderByDescending(s => s.SavedAt)
                    .Select(s => s.PostId)
                    .Take(6)
                    .ToListAsync();

                if (savedIds.Count > 0)
                {
                    var savedCards = await BuildCardsAsync(visiblePosts.Where(p => savedIds.Contains(p.Id)));
                    vm.JumpBackIn = savedIds
                        .Select(id => savedCards.FirstOrDefault(c => c.Id == id))
                        .Where(c => c is not null)
                        .Select(c => c!)
                        .ToList();
                    vm.JumpBackInIsSaved = true;
                }
            }

            if (vm.JumpBackIn.Count == 0)
            {
                vm.JumpBackIn = await BuildCardsAsync(
                    visiblePosts.OrderByDescending(p => p.UpdatedAt).Take(6));
                vm.JumpBackInIsSaved = false;
            }

            vm.RecentPosts = await BuildCardsAsync(
                visiblePosts.OrderByDescending(p => p.CreatedAt).Take(10));

            vm.Categories = await _db.Categories
                .Select(c => new HomeCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    PostCount = c.Posts.Count(p => !p.IsDeleted && p.IsPublic),
                    CoverImagePath = c.Posts
                        .Where(p => !p.IsDeleted && p.IsPublic && !string.IsNullOrEmpty(p.IconPath))
                        .OrderByDescending(p => p.UpdatedAt)
                        .Select(p => p.IconPath)
                        .FirstOrDefault()
                        ?? c.Posts
                        .Where(p => !p.IsDeleted && p.IsPublic)
                        .SelectMany(p => p.ContentBlocks)
                        .Where(b => b.Type == PostContentBlockType.Image)
                        .OrderBy(b => b.Order)
                        .Select(b => b.ImagePath)
                        .FirstOrDefault()
                })
                .OrderByDescending(c => c.PostCount)
                .Take(8)
                .ToListAsync();

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Route("Home/StatusCode/{code:int}")]
        public IActionResult StatusCode(int code)
        {
            if (code >= 400 && code <= 599)
            {
                Response.StatusCode = code;
            }

            ViewBag.StatusCode = code;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<List<HomeCardDto>> BuildCardsAsync(IQueryable<Post> query)
        {
            return await query
                .Select(p => new HomeCardDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    CategoryName = p.Category.Name,
                    UpdatedAt = p.UpdatedAt,
                    CoverImagePath = !string.IsNullOrEmpty(p.IconPath)
                        ? p.IconPath
                        : p.ContentBlocks
                            .Where(b => b.Type == PostContentBlockType.Image)
                            .OrderBy(b => b.Order)
                            .Select(b => b.ImagePath)
                            .FirstOrDefault()
                })
                .ToListAsync();
        }

        private Guid? TryGetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }
}
