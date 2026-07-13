using System.Security.Claims;
using Fandom_copy.Data;
using Fandom_copy.DTOs.Posts;
using Fandom_copy.Models;
using Fandom_copy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Controllers
{
    [Route("posts/{postId:guid}/sections")]
    public class PostSectionsController : Controller
    {
        private readonly IPostSectionService _sectionService;
        private readonly IPostService _postService;
        private readonly ApplicationDbContext _db;
        private readonly IPostImageStorage _imageStorage;
        private readonly IPostFileStorage _fileStorage;
        private readonly IPostVersionService _versions;

        public PostSectionsController(IPostSectionService sectionService, IPostService postService, ApplicationDbContext db, IPostImageStorage imageStorage, IPostFileStorage fileStorage, IPostVersionService versions)
        {
            _sectionService = sectionService;
            _postService = postService;
            _db = db;
            _imageStorage = imageStorage;
            _fileStorage = fileStorage;
            _versions = versions;
        }

        // GET /posts/{postId}/sections/{id}
        // Окрема сторінка-стаття підпоста (Fandom-стиль): заголовок, текст,
        // хлібні крихти до кореня, посилання на вкладені підпости.
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Details(Guid postId, Guid id)
        {
            var userId = TryGetCurrentUserId();
            var sectionResult = await _sectionService.GetByIdAsync(id, userId);
            if (!sectionResult.Success)
            {
                TempData["Error"] = sectionResult.Error;
                return RedirectToAction("Details", "Posts", new { id = postId });
            }

            // Отримуємо пост, щоб знати CanEdit / CanManageMembers для UI.
            var postResult = await _postService.GetByIdAsync(postId, userId);
            if (!postResult.Success)
            {
                TempData["Error"] = postResult.Error;
                return RedirectToAction("Index", "Posts");
            }

            ViewBag.Post = postResult.Data;
            return View(sectionResult.Data);
        }

        // GET /posts/{postId}/sections/create?parentSectionId=...
        [HttpGet("create")]
        [Authorize]
        public async Task<IActionResult> Create(Guid postId, Guid? parentSectionId)
        {
            var userId = GetCurrentUserId();
            var postResult = await _postService.GetByIdAsync(postId, userId);
            if (!postResult.Success)
            {
                TempData["Error"] = postResult.Error;
                return RedirectToAction("Index", "Posts");
            }

            if (!postResult.Data!.CanEdit)
            {
                TempData["Error"] = "Немає прав на створення підпостів у цьому пості";
                return RedirectToAction("Details", "Posts", new { id = postId });
            }

            // Якщо задано parent — переконуємось, що він взагалі існує і належить цьому посту.
            string? parentTitle = null;
            if (parentSectionId is not null)
            {
                var parentResult = await _sectionService.GetByIdAsync(parentSectionId.Value, userId);
                if (!parentResult.Success || parentResult.Data!.PostId != postId)
                {
                    TempData["Error"] = "Батьківський підпост недоступний";
                    return RedirectToAction("Details", "Posts", new { id = postId });
                }
                parentTitle = parentResult.Data.Title;
            }

            var dto = new CreatePostSectionDto
            {
                PostId = postId,
                ParentSectionId = parentSectionId
            };

            ViewBag.PostId = postId;
            ViewBag.ParentSectionTitle = parentTitle;
            return View(dto);
        }

        // POST /posts/{postId}/sections/create
        [HttpPost("create")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid postId, CreatePostSectionDto dto, List<IFormFile>? imageFiles, string? imageCaption, IFormFile? iconFile, List<IFormFile>? attachmentFiles)
        {
            // ЗАВЖДИ виставляємо PostId з роуту, щоб не довіряти прихованому полю.
            dto.PostId = postId;

            if (!ModelState.IsValid)
            {
                ViewBag.PostId = postId;
                return View(dto);
            }

            var userId = GetCurrentUserId();
            var result = await _sectionService.CreateAsync(dto, userId);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Помилка створення");
                ViewBag.PostId = postId;
                return View(dto);
            }

            if (imageFiles is { Count: > 0 })
            {
                var imageResult = await AppendSectionImagesAsync(postId, result.Data!.Id, imageFiles, imageCaption);
                if (!imageResult.Success)
                    TempData["Error"] = imageResult.Error;
            }

            if (iconFile is { Length: > 0 })
            {
                var iconResult = await SetSectionIconAsync(postId, result.Data!.Id, iconFile, removeIcon: false);
                if (!iconResult.Success)
                    TempData["Error"] = iconResult.Error;
            }

            if (attachmentFiles is { Count: > 0 })
            {
                var fileResult = await AppendSectionFilesAsync(postId, result.Data!.Id, attachmentFiles);
                if (!fileResult.Success)
                    TempData["Error"] = fileResult.Error;
            }

            // Після створення відкриваємо сторінку самого підпоста, як у Fandom.
            return RedirectToAction(nameof(Details), new { postId, id = result.Data!.Id });
        }

        // GET /posts/{postId}/sections/{id}/edit
        [HttpGet("{id:guid}/edit")]
        [Authorize]
        public async Task<IActionResult> Edit(Guid postId, Guid id)
        {
            var userId = GetCurrentUserId();

            var postResult = await _postService.GetByIdAsync(postId, userId);
            if (!postResult.Success || !postResult.Data!.CanEdit)
            {
                TempData["Error"] = postResult.Success ? "Немає прав на редагування" : postResult.Error;
                return RedirectToAction("Details", "Posts", new { id = postId });
            }

            var result = await _sectionService.GetByIdAsync(id, userId);
            if (!result.Success)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction("Details", "Posts", new { id = postId });
            }

            var dto = new UpdatePostSectionDto
            {
                Title = result.Data!.Title,
                Text = result.Data.Text,
                Order = result.Data.Order
            };

            ViewBag.PostId = postId;
            ViewBag.SectionId = id;
            ViewBag.CurrentIconPath = result.Data!.IconPath;
            ViewBag.SectionImages = result.Data.ContentBlocks
                .Where(b => b.Type == PostContentBlockType.Image)
                .ToList();
            ViewBag.SectionFiles = result.Data.Attachments;
            return View(dto);
        }

        // POST /posts/{postId}/sections/{id}/edit
        [HttpPost("{id:guid}/edit")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid postId, Guid id, UpdatePostSectionDto dto, List<IFormFile>? imageFiles, string? imageCaption, IFormFile? iconFile, List<IFormFile>? attachmentFiles, bool removeIcon = false)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.PostId = postId;
                ViewBag.SectionId = id;
                return View(dto);
            }

            var userId = GetCurrentUserId();
            var result = await _sectionService.UpdateAsync(id, dto, userId);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Помилка оновлення");
                ViewBag.PostId = postId;
                ViewBag.SectionId = id;
                return View(dto);
            }

            if (imageFiles is { Count: > 0 })
            {
                var imageResult = await AppendSectionImagesAsync(postId, id, imageFiles, imageCaption);
                if (!imageResult.Success)
                    TempData["Error"] = imageResult.Error;
            }

            if (removeIcon || iconFile is { Length: > 0 })
            {
                var iconResult = await SetSectionIconAsync(postId, id, iconFile, removeIcon);
                if (!iconResult.Success)
                    TempData["Error"] = iconResult.Error;
            }

            if (attachmentFiles is { Count: > 0 })
            {
                var fileResult = await AppendSectionFilesAsync(postId, id, attachmentFiles);
                if (!fileResult.Success)
                    TempData["Error"] = fileResult.Error;
            }

            return RedirectToAction(nameof(Details), new { postId, id });
        }

        // POST /posts/{postId}/sections/{id}/delete
        [HttpPost("{id:guid}/delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid postId, Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _sectionService.DeleteAsync(id, userId);

            if (!result.Success)
                TempData["Error"] = result.Error;
            else
                TempData["Message"] = "Підпост видалено";

            return RedirectToAction("Details", "Posts", new { id = postId });
        }

        // POST /posts/{postId}/sections/{id}/attachments/{attachmentId}/delete
        [HttpPost("{id:guid}/attachments/{attachmentId:guid}/delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(Guid postId, Guid id, Guid attachmentId)
        {
            var userId = GetCurrentUserId();
            var postResult = await _postService.GetByIdAsync(postId, userId);
            if (!postResult.Success || !postResult.Data!.CanEdit)
            {
                TempData["Error"] = postResult.Success ? "Немає прав на редагування" : postResult.Error;
                return RedirectToAction(nameof(Details), new { postId, id });
            }

            var attachment = await _db.Attachments
                .Include(a => a.PostSection)
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.PostSectionId == id && a.PostSection.PostId == postId);

            if (attachment is null)
            {
                TempData["Error"] = "Файл не знайдено";
                return RedirectToAction(nameof(Edit), new { postId, id });
            }

            var snapshot = await _versions.CaptureAsync(postId, userId, "AttachmentDeleted");
            if (!snapshot.Success)
            {
                TempData["Error"] = snapshot.Error;
                return RedirectToAction(nameof(Edit), new { postId, id });
            }

            _db.Attachments.Remove(attachment);

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
            if (post is not null)
                post.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Message"] = "Файл видалено";
            return RedirectToAction(nameof(Edit), new { postId, id });
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

        private async Task<ServiceResult> SetSectionIconAsync(Guid postId, Guid sectionId, IFormFile? iconFile, bool removeIcon)
        {
            var section = await _db.PostSections.FirstOrDefaultAsync(s => s.Id == sectionId && s.PostId == postId);
            if (section is null)
                return ServiceResult.Fail("Підпост не знайдено");

            if (removeIcon)
            {
                section.IconPath = null;
                await _db.SaveChangesAsync();
                return ServiceResult.Ok();
            }

            var saved = await _imageStorage.SaveAsync(postId, iconFile);
            if (!saved.Success)
                return ServiceResult.Fail(saved.Error ?? "Не вдалося завантажити іконку");

            section.IconPath = saved.RelativePath;
            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        private async Task<ServiceResult> AppendSectionImagesAsync(Guid postId, Guid sectionId, List<IFormFile> imageFiles, string? caption)
        {
            var files = imageFiles.Where(f => f is not null && f.Length > 0).ToList();
            if (files.Count == 0)
                return ServiceResult.Ok();

            var sectionExists = await _db.PostSections.AnyAsync(s => s.Id == sectionId && s.PostId == postId);
            if (!sectionExists)
                return ServiceResult.Fail("Підпост не знайдено");

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
            if (post is null)
                return ServiceResult.Fail("Пост не знайдено");

            var nextOrder = await _db.PostContentBlocks
                .Where(b => b.PostId == postId && b.ContainerSectionId == sectionId)
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
                    ContainerSectionId = sectionId,
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

        private async Task<ServiceResult> AppendSectionFilesAsync(Guid postId, Guid sectionId, List<IFormFile> attachmentFiles)
        {
            var files = attachmentFiles.Where(f => f is not null && f.Length > 0).ToList();
            if (files.Count == 0)
                return ServiceResult.Ok();

            var sectionExists = await _db.PostSections.AnyAsync(s => s.Id == sectionId && s.PostId == postId);
            if (!sectionExists)
                return ServiceResult.Fail("Підпост не знайдено");

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
            if (post is null)
                return ServiceResult.Fail("Пост не знайдено");

            foreach (var file in files)
            {
                var saved = await _fileStorage.SaveAsync(postId, file);
                if (!saved.Success)
                    return ServiceResult.Fail(saved.Error ?? "Не вдалося завантажити файл");

                _db.Attachments.Add(new FileAttachment
                {
                    Id = Guid.NewGuid(),
                    FileName = GetSafeOriginalFileName(file.FileName),
                    Path = saved.RelativePath!,
                    Size = file.Length,
                    PostSectionId = sectionId
                });
            }

            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        private static string GetSafeOriginalFileName(string fileName)
        {
            var normalized = (fileName ?? string.Empty).Replace('\\', '/');
            var safeName = Path.GetFileName(normalized);

            if (!string.IsNullOrWhiteSpace(safeName))
                return safeName;

            var extension = Path.GetExtension(fileName);
            return $"attachment{extension.ToLowerInvariant()}";
        }
    }
}
