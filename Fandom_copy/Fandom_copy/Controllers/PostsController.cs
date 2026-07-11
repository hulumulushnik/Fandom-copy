using System.Security.Claims;
using Fandom_copy.Data;
using Fandom_copy.DTOs.Posts;
using Fandom_copy.Models;
using Fandom_copy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        private readonly IPostImageStorage _imageStorage;

        public PostsController(IPostService postService, ApplicationDbContext db, IPostImageStorage imageStorage)
        {
            _postService = postService;
            _db = db;
            _imageStorage = imageStorage;
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

            ViewBag.IsSaved = userId is not null &&
                await _db.SavedPosts.AnyAsync(s => s.PostId == id && s.UserId == userId.Value);

            return View(result.Data);
        }

        // GET /posts/search?q=...
        // Живий пошук постів по назві, опису, категорії та тексту/заголовках
        // усіх під-постів (структури) цього поста.
        [HttpGet("search")]
        public async Task<IActionResult> Search(string? q)
        {
            q = (q ?? string.Empty).Trim();
            ViewBag.Query = q;

            var userId = TryGetCurrentUserId();

            var visible = _db.Posts.Where(p => !p.IsDeleted);
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

            var results = new List<PostSearchResultDto>();

            if (q.Length > 0)
            {
                var lowered = q.ToLower();

                var matches = await visible
                    .Include(p => p.Category)
                    .Include(p => p.Sections)
                    .Where(p =>
                        p.Title.ToLower().Contains(lowered) ||
                        p.Description.ToLower().Contains(lowered) ||
                        p.Category.Name.ToLower().Contains(lowered) ||
                        p.Sections.Any(s => s.Title.ToLower().Contains(lowered) || s.Text.ToLower().Contains(lowered)))
                    .OrderByDescending(p => p.UpdatedAt)
                    .Take(40)
                    .ToListAsync();

                foreach (var p in matches)
                {
                    var dto = new PostSearchResultDto
                    {
                        Id = p.Id,
                        Title = p.Title,
                        CategoryName = p.Category?.Name,
                        IsPublic = p.IsPublic,
                        UpdatedAt = p.UpdatedAt
                    };

                    if (p.Title.ToLower().Contains(lowered))
                    {
                        dto.MatchedIn = "Title";
                        dto.Snippet = p.Description;
                    }
                    else if (p.Description.ToLower().Contains(lowered))
                    {
                        dto.MatchedIn = "Description";
                        dto.Snippet = p.Description;
                    }
                    else
                    {
                        var section = p.Sections.FirstOrDefault(s =>
                            s.Title.ToLower().Contains(lowered) || s.Text.ToLower().Contains(lowered));

                        if (section is not null)
                        {
                            dto.MatchedIn = "Section";
                            dto.MatchedSectionId = section.Id;
                            dto.MatchedSectionTitle = section.Title;
                            dto.Snippet = section.Text;
                        }
                        else
                        {
                            dto.Snippet = p.Description;
                        }
                    }

                    dto.Snippet = MakeSnippet(dto.Snippet, q);
                    results.Add(dto);
                }
            }

            return View(results);
        }

        // -----------------------------------------------------------------
        //  Saved posts
        // -----------------------------------------------------------------

        // POST /posts/{id}/save
        [HttpPost("{id:guid}/save")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSave(Guid id)
        {
            var userId = GetCurrentUserId();

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (post is null)
            {
                TempData["Error"] = "Пост не знайдено";
                return RedirectToAction(nameof(Index));
            }

            var existing = await _db.SavedPosts
                .FirstOrDefaultAsync(s => s.PostId == id && s.UserId == userId);

            if (existing is null)
            {
                _db.SavedPosts.Add(new SavedPost
                {
                    Id = Guid.NewGuid(),
                    PostId = id,
                    UserId = userId,
                    SavedAt = DateTime.UtcNow
                });
                TempData["Message"] = "Додано у Збережене";
            }
            else
            {
                _db.SavedPosts.Remove(existing);
                TempData["Message"] = "Видалено із Збереженого";
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET /posts/saved
        [HttpGet("saved")]
        [Authorize]
        public async Task<IActionResult> Saved()
        {
            var userId = GetCurrentUserId();

            var savedIds = await _db.SavedPosts
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SavedAt)
                .Select(s => s.PostId)
                .ToListAsync();

            var posts = await _db.Posts
                .Include(p => p.Category)
                .Where(p => savedIds.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync();

            var ordered = savedIds
                .Select(id => posts.FirstOrDefault(p => p.Id == id))
                .Where(p => p is not null)
                .Select(p => PostDto.FromEntity(p!))
                .ToList();

            return View(ordered);
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
        public async Task<IActionResult> Create(CreatePostDto dto, List<IFormFile>? imageFiles, string? imageCaption, IFormFile? iconFile)
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

            if (imageFiles is { Count: > 0 })
            {
                var imageResult = await AppendRootImagesAsync(result.Data!.Id, imageFiles, imageCaption);
                if (!imageResult.Success)
                    TempData["Error"] = imageResult.Error;
            }

            if (iconFile is { Length: > 0 })
            {
                var iconResult = await SetPostIconAsync(result.Data!.Id, iconFile, removeIcon: false);
                if (!iconResult.Success)
                    TempData["Error"] = iconResult.Error;
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
            ViewBag.CurrentIconPath = result.Data!.IconPath;
            ViewBag.RootImages = result.Data.ContentBlocks
                .Where(b => b.Type == PostContentBlockType.Image)
                .ToList();
            return View(dto);
        }

        // POST /posts/{id}/edit
        [HttpPost("{id:guid}/edit")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UpdatePostDto dto, List<IFormFile>? imageFiles, string? imageCaption, IFormFile? iconFile, bool removeIcon = false)
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

            if (imageFiles is { Count: > 0 })
            {
                var imageResult = await AppendRootImagesAsync(id, imageFiles, imageCaption);
                if (!imageResult.Success)
                    TempData["Error"] = imageResult.Error;
            }

            if (removeIcon || iconFile is { Length: > 0 })
            {
                var iconResult = await SetPostIconAsync(id, iconFile, removeIcon);
                if (!iconResult.Success)
                    TempData["Error"] = iconResult.Error;
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

        private static string MakeSnippet(string text, string query, int radius = 90)
        {
            text = (text ?? string.Empty).Trim();
            if (text.Length == 0) return string.Empty;

            var idx = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return text.Length > radius * 2 ? text[..(radius * 2)] + "…" : text;

            var start = Math.Max(0, idx - radius);
            var end = Math.Min(text.Length, idx + query.Length + radius);
            var snippet = text[start..end];

            if (start > 0) snippet = "…" + snippet;
            if (end < text.Length) snippet += "…";
            return snippet;
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

        private async Task<ServiceResult> SetPostIconAsync(Guid postId, IFormFile? iconFile, bool removeIcon)
        {
            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
            if (post is null)
                return ServiceResult.Fail("Пост не знайдено");

            if (removeIcon)
            {
                if (!string.IsNullOrWhiteSpace(post.IconPath))
                    _imageStorage.Delete(post.IconPath);

                post.IconPath = null;
                post.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return ServiceResult.Ok();
            }

            var saved = await _imageStorage.SaveAsync(postId, iconFile);
            if (!saved.Success)
                return ServiceResult.Fail(saved.Error ?? "Не вдалося завантажити іконку");

            if (!string.IsNullOrWhiteSpace(post.IconPath))
                _imageStorage.Delete(post.IconPath);

            post.IconPath = saved.RelativePath;
            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        private async Task<ServiceResult> AppendRootImagesAsync(Guid postId, List<IFormFile> imageFiles, string? caption)
        {
            var files = imageFiles.Where(f => f is not null && f.Length > 0).ToList();
            if (files.Count == 0)
                return ServiceResult.Ok();

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
            if (post is null)
                return ServiceResult.Fail("Пост не знайдено");

            var nextOrder = await _db.PostContentBlocks
                .Where(b => b.PostId == postId && b.ContainerSectionId == null)
                .Select(b => (int?)b.Order)
                .MaxAsync() ?? -1;

            foreach (var file in files)
            {
                var saved = await _imageStorage.SaveAsync(postId, file);
                if (!saved.Success)
                    return ServiceResult.Fail(saved.Error ?? "Не удалось загрузить изображение");

                _db.PostContentBlocks.Add(new PostContentBlock
                {
                    Id = Guid.NewGuid(),
                    PostId = postId,
                    ContainerSectionId = null,
                    Type = PostContentBlockType.Image,
                    ImagePath = saved.RelativePath!,
                    ImageCaption = (caption ?? string.Empty).Trim(),
                    Order = ++nextOrder
                });
            }

            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }
    }
}
