using System.Security.Claims;
using Fandom_copy.Data;
using Fandom_copy.DTOs.Posts;
using Fandom_copy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Controllers
{
    [Route("posts")]
    public class PostsController : Controller
    {
        private readonly IPostService _postService;
        private readonly ApplicationDbContext _db;

        public PostsController(IPostService postService, ApplicationDbContext db)
        {
            _postService = postService;
            _db = db;
        }

        // GET /posts
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = TryGetCurrentUserId();
            var result = await _postService.GetAllAsync(userId);
            return View(result.Data ?? new List<PostDto>());
        }

        // GET /posts/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var userId = TryGetCurrentUserId();
            var result = await _postService.GetByIdAsync(id, userId);
            if (!result.Success)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Index));
            }
            return View(result.Data);
        }

        // GET /posts/create
        [HttpGet("create")]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            await LoadCategoriesAsync();
            return View(new CreatePostDto());
        }

        // POST /posts/create
        [HttpPost("create")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePostDto dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View(dto);
            }

            var userId = GetCurrentUserId();
            var result = await _postService.CreateAsync(dto, userId);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Помилка створення");
                await LoadCategoriesAsync();
                return View(dto);
            }

            return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
        }

        // GET /posts/{id}/edit
        [HttpGet("{id:guid}/edit")]
        [Authorize]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _postService.GetByIdAsync(id, userId);
            if (!result.Success)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Index));
            }

            await LoadCategoriesAsync(result.Data!.CategoryId);

            var dto = new UpdatePostDto
            {
                Title = result.Data!.Title,
                Description = result.Data.Description,
                CategoryId = result.Data.CategoryId,
                IsPublic = result.Data.IsPublic
            };

            ViewBag.PostId = id;
            return View(dto);
        }

        // POST /posts/{id}/edit
        [HttpPost("{id:guid}/edit")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UpdatePostDto dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(dto.CategoryId);
                ViewBag.PostId = id;
                return View(dto);
            }

            var userId = GetCurrentUserId();
            var result = await _postService.UpdateAsync(id, dto, userId);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Помилка оновлення");
                await LoadCategoriesAsync(dto.CategoryId);
                ViewBag.PostId = id;
                return View(dto);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /posts/{id}/delete
        [HttpPost("{id:guid}/delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _postService.DeleteAsync(id, userId);

            if (!result.Success)
                TempData["Error"] = result.Error;
            else
                TempData["Message"] = "Пост видалено";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCategoriesAsync(Guid? selectedId = null)
        {
            var categories = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedId);
        }

        private Guid GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(idClaim!);
        }

        private Guid? TryGetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }
}
