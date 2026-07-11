using System.Security.Claims;
using Fandom_copy.Data;
using Fandom_copy.DTOs.Posts;
using Fandom_copy.Models;
using Fandom_copy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Controllers;

[Authorize]
[Route("posts/{postId:guid}/content")]
public class PostContentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IPostService _posts;
    private readonly IPostImageStorage _imageStorage;

    public PostContentController(ApplicationDbContext db, IPostService posts, IPostImageStorage imageStorage)
    {
        _db = db;
        _posts = posts;
        _imageStorage = imageStorage;
    }

    [HttpGet("")]
    [HttpGet("sections/{sectionId:guid}")]
    public async Task<IActionResult> Index(Guid postId, Guid? sectionId)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });
        if (sectionId is not null && !await _db.PostSections.AnyAsync(s => s.Id == sectionId && s.PostId == postId)) return NotFound();
        return View(await BuildModel(postId, sectionId, post.Title));
    }

    [HttpPost("text")]
    [HttpPost("sections/{sectionId:guid}/text")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddText(Guid postId, Guid? sectionId, PostContentEditorDto dto)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });
        if (string.IsNullOrWhiteSpace(dto.NewText))
        {
            TempData["Error"] = "Введите текст блока.";
            return RedirectToEditor(postId, sectionId);
        }

        _db.PostContentBlocks.Add(new PostContentBlock
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            ContainerSectionId = sectionId,
            Type = PostContentBlockType.Text,
            Text = dto.NewText.Trim(),
            Order = await GetNextOrderAsync(postId, sectionId)
        });

        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToEditor(postId, sectionId);
    }

    [HttpPost("image")]
    [HttpPost("sections/{sectionId:guid}/image")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddImage(Guid postId, Guid? sectionId, PostContentEditorDto dto, IFormFile? imageFile, List<IFormFile>? imageFiles)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });

        var files = imageFiles?.Where(f => f is not null && f.Length > 0).ToList() ?? new List<IFormFile>();
        if (imageFile is not null && imageFile.Length > 0)
            files.Insert(0, imageFile);

        if (files.Count == 0)
        {
            TempData["Error"] = "Выберите изображение.";
            return RedirectToEditor(postId, sectionId);
        }

        var nextOrder = await GetNextOrderAsync(postId, sectionId) - 1;
        foreach (var file in files)
        {
            var saved = await _imageStorage.SaveAsync(postId, file);
            if (!saved.Success)
            {
                TempData["Error"] = saved.Error;
                return RedirectToEditor(postId, sectionId);
            }

            _db.PostContentBlocks.Add(new PostContentBlock
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                ContainerSectionId = sectionId,
                Type = PostContentBlockType.Image,
                ImagePath = saved.RelativePath!,
                ImageCaption = (dto.NewImageCaption ?? string.Empty).Trim(),
                Order = ++nextOrder
            });
        }

        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToEditor(postId, sectionId);
    }

    [HttpPost("section")]
    [HttpPost("sections/{sectionId:guid}/section")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExistingSection(Guid postId, Guid? sectionId, Guid linkedSectionId)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });

        var section = await _db.PostSections
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == linkedSectionId && s.PostId == postId && s.ParentSectionId == sectionId);

        if (section is null)
        {
            TempData["Error"] = "Этот под-пост нельзя вставить в выбранную структуру.";
            return RedirectToEditor(postId, sectionId);
        }

        var alreadyLinked = await _db.PostContentBlocks.AnyAsync(b =>
            b.PostId == postId &&
            b.ContainerSectionId == sectionId &&
            b.Type == PostContentBlockType.Section &&
            b.SectionId == linkedSectionId);

        if (alreadyLinked)
        {
            TempData["Error"] = "Этот под-пост уже есть в структуре.";
            return RedirectToEditor(postId, sectionId);
        }

        _db.PostContentBlocks.Add(new PostContentBlock
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            ContainerSectionId = sectionId,
            Type = PostContentBlockType.Section,
            SectionId = linkedSectionId,
            Order = await GetNextOrderAsync(postId, sectionId)
        });

        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToEditor(postId, sectionId);
    }

    [HttpPost("{id:guid}/text")]
    [HttpPost("sections/{sectionId:guid}/{id:guid}/text")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateText(Guid postId, Guid? sectionId, Guid id, string text)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });
        if (string.IsNullOrWhiteSpace(text))
        {
            TempData["Error"] = "Текстовый блок не может быть пустым.";
            return RedirectToEditor(postId, sectionId);
        }

        var block = await _db.PostContentBlocks.FirstOrDefaultAsync(b =>
            b.Id == id && b.PostId == postId && b.ContainerSectionId == sectionId && b.Type == PostContentBlockType.Text);

        if (block is not null)
        {
            block.Text = text.Trim();
            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToEditor(postId, sectionId);
    }

    [HttpPost("{id:guid}/image-caption")]
    [HttpPost("sections/{sectionId:guid}/{id:guid}/image-caption")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateImageCaption(Guid postId, Guid? sectionId, Guid id, string imageCaption)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });

        var block = await _db.PostContentBlocks.FirstOrDefaultAsync(b =>
            b.Id == id && b.PostId == postId && b.ContainerSectionId == sectionId && b.Type == PostContentBlockType.Image);

        if (block is not null)
        {
            block.ImageCaption = (imageCaption ?? string.Empty).Trim();
            if (block.ImageCaption.Length > 240)
                block.ImageCaption = block.ImageCaption[..240];
            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToEditor(postId, sectionId);
    }

    [HttpPost("{id:guid}/move")]
    [HttpPost("sections/{sectionId:guid}/{id:guid}/move")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(Guid postId, Guid? sectionId, Guid id, int direction)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });

        var blocks = await _db.PostContentBlocks
            .Where(b => b.PostId == postId && b.ContainerSectionId == sectionId)
            .OrderBy(b => b.Order)
            .ToListAsync();

        var index = blocks.FindIndex(b => b.Id == id);
        var target = index + (direction < 0 ? -1 : 1);

        if (index >= 0 && target >= 0 && target < blocks.Count)
        {
            (blocks[index].Order, blocks[target].Order) = (blocks[target].Order, blocks[index].Order);
            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToEditor(postId, sectionId);
    }

    [HttpPost("{id:guid}/delete")]
    [HttpPost("sections/{sectionId:guid}/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid postId, Guid? sectionId, Guid id)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });

        var block = await _db.PostContentBlocks.FirstOrDefaultAsync(b =>
            b.Id == id && b.PostId == postId && b.ContainerSectionId == sectionId);

        if (block is not null)
        {
            if (block.Type == PostContentBlockType.Image)
                _imageStorage.Delete(block.ImagePath);

            _db.PostContentBlocks.Remove(block);
            await NormalizeOrdersAsync(postId, sectionId);
            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToEditor(postId, sectionId);
    }

    private async Task<Post?> GetEditablePost(Guid postId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _posts.GetByIdAsync(postId, userId);
        if (!result.Success || !result.Data!.CanEdit)
        {
            TempData["Error"] = "У вас нет прав на изменение структуры публикации.";
            return null;
        }

        return await _db.Posts.FirstAsync(p => p.Id == postId);
    }

    private async Task<PostContentEditorDto> BuildModel(Guid postId, Guid? sectionId, string postTitle)
    {
        var blocks = await _db.PostContentBlocks
            .Include(b => b.Section)
            .Where(b => b.PostId == postId && b.ContainerSectionId == sectionId)
            .OrderBy(b => b.Order)
            .ToListAsync();

        var title = sectionId is null
            ? postTitle
            : (await _db.PostSections.AsNoTracking().FirstAsync(s => s.Id == sectionId)).Title;

        var linkedSectionIds = blocks
            .Where(b => b.Type == PostContentBlockType.Section && b.SectionId is not null)
            .Select(b => b.SectionId!.Value)
            .ToHashSet();

        var availableSections = await _db.PostSections
            .AsNoTracking()
            .Where(s => s.PostId == postId && s.ParentSectionId == sectionId && !linkedSectionIds.Contains(s.Id))
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Title)
            .Select(s => new PostSectionPickerDto { Id = s.Id, Title = s.Title })
            .ToListAsync();

        return new PostContentEditorDto
        {
            PostId = postId,
            ContainerSectionId = sectionId,
            Title = title,
            CanEdit = true,
            Blocks = blocks.Select(PostContentBlockDto.FromEntity).ToList(),
            AvailableSections = availableSections
        };
    }

    private async Task<int> GetNextOrderAsync(Guid postId, Guid? sectionId)
    {
        var last = await _db.PostContentBlocks
            .Where(b => b.PostId == postId && b.ContainerSectionId == sectionId)
            .Select(b => (int?)b.Order)
            .MaxAsync();

        return (last ?? -1) + 1;
    }

    private async Task NormalizeOrdersAsync(Guid postId, Guid? sectionId)
    {
        var blocks = await _db.PostContentBlocks
            .Where(b => b.PostId == postId && b.ContainerSectionId == sectionId)
            .OrderBy(b => b.Order)
            .ToListAsync();

        for (var i = 0; i < blocks.Count; i++)
            blocks[i].Order = i;
    }

    private IActionResult RedirectToEditor(Guid postId, Guid? sectionId) =>
        sectionId is null
            ? RedirectToAction(nameof(Index), new { postId })
            : RedirectToAction(nameof(Index), new { postId, sectionId });
}
