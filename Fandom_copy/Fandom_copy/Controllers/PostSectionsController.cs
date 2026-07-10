using System.Security.Claims;
using Fandom_copy.DTOs.Posts;
using Fandom_copy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fandom_copy.Controllers
{
    [Route("posts/{postId:guid}/sections")]
    [Authorize]
    public class PostSectionsController : Controller
    {
        private readonly IPostSectionService _sectionService;

        public PostSectionsController(IPostSectionService sectionService)
        {
            _sectionService = sectionService;
        }

        // GET /posts/{postId}/sections/create?parentSectionId=...
        [HttpGet("create")]
        public IActionResult Create(Guid postId, Guid? parentSectionId)
        {
            var dto = new CreatePostSectionDto
            {
                PostId = postId,
                ParentSectionId = parentSectionId
            };
            ViewBag.PostId = postId;
            return View(dto);
        }

        // POST /posts/{postId}/sections/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid postId, CreatePostSectionDto dto)
        {
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

            return RedirectToAction("Details", "Posts", new { id = postId });
        }

        // GET /posts/{postId}/sections/{id}/edit
        [HttpGet("{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid postId, Guid id)
        {
            var result = await _sectionService.GetByIdAsync(id);
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

            return RedirectToAction("Details", "Posts", new { id = postId });
        }

        // POST /posts/{postId}/sections/{id}/delete
        [HttpPost("{id:guid}/delete")]
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
    }
}
