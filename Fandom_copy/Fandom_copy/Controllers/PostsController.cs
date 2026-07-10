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

            if (!result.Data!.CanEdit)
            {
                TempData["Error"] = "Немає прав на редагування цього поста";
                return RedirectToAction(nameof(Details), new { id });
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

        // -----------------------------------------------------------------
        //  Members
        // -----------------------------------------------------------------

        // GET /posts/{id}/members
        [HttpGet("{id:guid}/members")]
        [Authorize]
        public async Task<IActionResult> Members(Guid id)
        {
            var userId = GetCurrentUserId();

            var postResult = await _postService.GetByIdAsync(id, userId);
            if (!postResult.Success)
            {
                TempData["Error"] = postResult.Error;
                return RedirectToAction(nameof(Index));
            }

            if (!postResult.Data!.CanManageMembers)
            {
                TempData["Error"] = "Лише власник може керувати учасниками";
                return RedirectToAction(nameof(Details), new { id });
            }

            var membersResult = await _postService.GetMembersAsync(id, userId);
            if (!membersResult.Success)
            {
                TempData["Error"] = membersResult.Error;
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.Post = postResult.Data;
            return View(membersResult.Data);
        }

        // POST /posts/{id}/members/add
        [HttpPost("{id:guid}/members/add")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(Guid id, AddPostMemberDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _postService.AddMemberAsync(id, dto, userId);

            if (!result.Success)
                TempData["Error"] = result.Error;
            else
                TempData["Message"] = $"Додано: {result.Data!.Login} ({result.Data.Role})";

            return RedirectToAction(nameof(Members), new { id });
        }

        // POST /posts/{id}/members/{memberId}/role
        [HttpPost("{id:guid}/members/{memberId:guid}/role")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMemberRole(Guid id, Guid memberId, UpdatePostMemberRoleDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _postService.UpdateMemberRoleAsync(id, memberId, dto, userId);

            if (!result.Success)
                TempData["Error"] = result.Error;
            else
                TempData["Message"] = $"Роль оновлено: {result.Data!.Role}";

            return RedirectToAction(nameof(Members), new { id });
        }

        // POST /posts/{id}/members/{memberId}/delete
        [HttpPost("{id:guid}/members/{memberId:guid}/delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
        {
            var userId = GetCurrentUserId();
            var result = await _postService.RemoveMemberAsync(id, memberId, userId);

            if (!result.Success)
                TempData["Error"] = result.Error;
            else
                TempData["Message"] = "Учасника видалено";

            return RedirectToAction(nameof(Members), new { id });
        }

        // GET /posts/{id}/users/search?q=...
        // Використовується JS-контролом на сторінці Members для живого пошуку користувачів у БД.
        [HttpGet("{id:guid}/users/search")]
        [Authorize]
        public async Task<IActionResult> SearchUsers(Guid id, string q)
        {
            var userId = GetCurrentUserId();
            var result = await _postService.SearchUsersAsync(id, q ?? string.Empty, userId);

            if (!result.Success)
                return Forbid();

            return Json(result.Data);
        }

        // -----------------------------------------------------------------
        //  Helpers
        // -----------------------------------------------------------------

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
