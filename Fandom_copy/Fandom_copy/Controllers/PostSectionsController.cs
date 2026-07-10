using System.Security.Claims;
using Fandom_copy.DTOs.Posts;
using Fandom_copy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fandom_copy.Controllers
{
    [Route("posts/{postId:guid}/sections")]
    public class PostSectionsController : Controller
    {
        private readonly IPostSectionService _sectionService;
        private readonly IPostService _postService;

        public PostSectionsController(IPostSectionService sectionService, IPostService postService)
        {
            _sectionService = sectionService;
            _postService = postService;
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
        public async Task<IActionResult> Create(Guid postId, CreatePostSectionDto dto)
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
            return View(dto);
        }

        // POST /posts/{postId}/sections/{id}/edit
        [HttpPost("{id:guid}/edit")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid postId, Guid id, UpdatePostSectionDto dto)
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
