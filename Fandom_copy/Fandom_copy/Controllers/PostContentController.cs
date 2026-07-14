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
    private readonly IPostVersionService _versions;

    public PostContentController(ApplicationDbContext db, IPostService posts, IPostImageStorage imageStorage, IPostVersionService versions)
    {
        _db = db;
        _posts = posts;
        _imageStorage = imageStorage;
        _versions = versions;
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
            TempData["Error"] = "Введіть текст блоку.";
            return RedirectToEditor(postId, sectionId);
        }

        var snapshot = await _versions.CaptureAsync(postId, GetCurrentUserId(), "ContentTextAdded");
        if (!snapshot.Success)
        {
            TempData["Error"] = snapshot.Error;
            return RedirectToEditor(postId, sectionId);
        }

        _db.PostContentBlocks.Add(new PostContentBlock
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            ContainerSectionId = sectionId,
            Type = PostContentBlockType.Text,
            Text = dto.NewText.Trim(),
            TextBold = dto.NewTextBold,
            TextItalic = dto.NewTextItalic,
            TextUnderline = dto.NewTextUnderline,
            TextStrike = dto.NewTextStrike,
            TextSize = dto.NewTextSize,
            TextAlign = dto.NewTextAlign,
            TextStyle = dto.NewTextStyle,
            TextColor = PostContentFormatting.SanitizeColor(dto.NewTextColor),
            Order = await GetNextOrderAsync(postId, sectionId)
        });

        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToEditor(postId, sectionId);
    }

    [HttpPost("template")]
    [HttpPost("sections/{sectionId:guid}/template")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTemplate(Guid postId, Guid? sectionId, PostBlockTemplateType templateType, string? text)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });
        if (templateType == PostBlockTemplateType.None)
        {
            TempData["Error"] = "Оберіть шаблон.";
            return RedirectToEditor(postId, sectionId);
        }

        var snapshot = await _versions.CaptureAsync(postId, GetCurrentUserId(), "ContentTemplateAdded");
        if (!snapshot.Success)
        {
            TempData["Error"] = snapshot.Error;
            return RedirectToEditor(postId, sectionId);
        }

        _db.PostContentBlocks.Add(new PostContentBlock
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            ContainerSectionId = sectionId,
            Type = PostContentBlockType.Template,
            TemplateType = templateType,
            Text = (text ?? DefaultTemplateText(templateType)).Trim(),
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
            TempData["Error"] = "Оберіть зображення.";
            return RedirectToEditor(postId, sectionId);
        }

        var snapshot = await _versions.CaptureAsync(postId, GetCurrentUserId(), "ContentImageAdded");
        if (!snapshot.Success)
        {
            TempData["Error"] = snapshot.Error;
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

    [HttpPost("gallery")]
    [HttpPost("sections/{sectionId:guid}/gallery")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGallery(Guid postId, Guid? sectionId, PostContentEditorDto dto, List<IFormFile>? galleryFiles)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });

        var files = galleryFiles?.Where(f => f is not null && f.Length > 0).ToList() ?? new List<IFormFile>();
        if (files.Count == 0)
        {
            TempData["Error"] = "Оберіть хоча б одне зображення для галереї.";
            return RedirectToEditor(postId, sectionId);
        }

        var snapshot = await _versions.CaptureAsync(postId, GetCurrentUserId(), "ContentGalleryAdded");
        if (!snapshot.Success)
        {
            TempData["Error"] = snapshot.Error;
            return RedirectToEditor(postId, sectionId);
        }

        var blockId = Guid.NewGuid();
        var block = new PostContentBlock
        {
            Id = blockId,
            PostId = postId,
            ContainerSectionId = sectionId,
            Type = PostContentBlockType.Gallery,
            GalleryStyle = dto.NewGalleryStyle,
            GalleryCaption = (dto.NewGalleryCaption ?? string.Empty).Trim(),
            Order = await GetNextOrderAsync(postId, sectionId)
        };

        var order = 0;
        foreach (var file in files)
        {
            var saved = await _imageStorage.SaveAsync(postId, file);
            if (!saved.Success)
            {
                TempData["Error"] = saved.Error;
                return RedirectToEditor(postId, sectionId);
            }
            block.GalleryImages.Add(new PostGalleryImage
            {
                Id = Guid.NewGuid(),
                PostContentBlockId = blockId,
                ImagePath = saved.RelativePath!,
                Caption = string.Empty,
                Order = order++
            });
        }

        _db.PostContentBlocks.Add(block);
        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToEditor(postId, sectionId);
    }

    [HttpPost("{id:guid}/gallery")]
    [HttpPost("sections/{sectionId:guid}/{id:guid}/gallery")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGallery(Guid postId, Guid? sectionId, Guid id, string? galleryCaption, PostGalleryStyle galleryStyle, string? orderedImageIds)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });

        var block = await _db.PostContentBlocks
            .Include(b => b.GalleryImages)
            .FirstOrDefaultAsync(b => b.Id == id && b.PostId == postId && b.ContainerSectionId == sectionId && b.Type == PostContentBlockType.Gallery);
        if (block is null) return RedirectToEditor(postId, sectionId);

        var snapshot = await _versions.CaptureAsync(postId, GetCurrentUserId(), "ContentGalleryUpdated");
        if (!snapshot.Success)
        {
            TempData["Error"] = snapshot.Error;
            return RedirectToEditor(postId, sectionId);
        }

        block.GalleryCaption = ((galleryCaption ?? string.Empty).Trim());
        if (block.GalleryCaption.Length > 240) block.GalleryCaption = block.GalleryCaption[..240];
        block.GalleryStyle = galleryStyle;

        if (!string.IsNullOrWhiteSpace(orderedImageIds))
        {
            var ids = orderedImageIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();

            for (var i = 0; i < ids.Count; i++)
            {
                var img = block.GalleryImages.FirstOrDefault(g => g.Id == ids[i]);
                if (img is not null) img.Order = i;
            }
        }

        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToEditor(postId, sectionId);
    }

    [HttpPost("{id:guid}/gallery/{imageId:guid}/delete")]
    [HttpPost("sections/{sectionId:guid}/{id:guid}/gallery/{imageId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGalleryImage(Guid postId, Guid? sectionId, Guid id, Guid imageId)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });

        var image = await _db.PostGalleryImages
            .Include(g => g.Block)
            .FirstOrDefaultAsync(g => g.Id == imageId && g.PostContentBlockId == id);

        if (image?.Block is null || image.Block.PostId != postId || image.Block.ContainerSectionId != sectionId)
            return RedirectToEditor(postId, sectionId);

        var snapshot = await _versions.CaptureAsync(postId, GetCurrentUserId(), "ContentGalleryImageDeleted");
        if (!snapshot.Success)
        {
            TempData["Error"] = snapshot.Error;
            return RedirectToEditor(postId, sectionId);
        }

        _db.PostGalleryImages.Remove(image);
        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToEditor(postId, sectionId);
    }

    [HttpPost("section")]
    [HttpPost("sections/{sectionId:guid}/section")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExistingSection(Guid postId, Guid? sectionId, Guid linkedSectionId, PostSectionDisplayStyle displayStyle, string? linkText)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });

        var section = await _db.PostSections
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == linkedSectionId && s.PostId == postId && s.ParentSectionId == sectionId);

        if (section is null)
        {
            TempData["Error"] = "Цей підпост не можна вставити в обрану структуру.";
            return RedirectToEditor(postId, sectionId);
        }

        var alreadyLinked = await _db.PostContentBlocks.AnyAsync(b =>
            b.PostId == postId &&
            b.ContainerSectionId == sectionId &&
            b.Type == PostContentBlockType.Section &&
            b.SectionId == linkedSectionId);

        if (alreadyLinked)
        {
            TempData["Error"] = "Цей підпост уже є у структурі.";
            return RedirectToEditor(postId, sectionId);
        }

        var snapshot = await _versions.CaptureAsync(postId, GetCurrentUserId(), "ContentSectionLinked");
        if (!snapshot.Success)
        {
            TempData["Error"] = snapshot.Error;
            return RedirectToEditor(postId, sectionId);
        }

        _db.PostContentBlocks.Add(new PostContentBlock
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            ContainerSectionId = sectionId,
            Type = PostContentBlockType.Section,
            SectionId = linkedSectionId,
            SectionDisplayStyle = displayStyle,
            SectionLinkText = TruncateLinkText(linkText),
            Order = await GetNextOrderAsync(postId, sectionId)
        });

        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToEditor(postId, sectionId);
    }

    [HttpPost("{id:guid}/section-display")]
    [HttpPost("sections/{sectionId:guid}/{id:guid}/section-display")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSectionDisplay(Guid postId, Guid? sectionId, Guid id, PostSectionDisplayStyle displayStyle, string? linkText)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });

        var block = await _db.PostContentBlocks.FirstOrDefaultAsync(b =>
            b.Id == id && b.PostId == postId && b.ContainerSectionId == sectionId && b.Type == PostContentBlockType.Section);

        if (block is not null)
        {
            var snapshot = await _versions.CaptureAsync(postId, GetCurrentUserId(), "ContentSectionDisplayUpdated");
            if (!snapshot.Success)
            {
                TempData["Error"] = snapshot.Error;
                return RedirectToEditor(postId, sectionId);
            }

            block.SectionDisplayStyle = displayStyle;
            block.SectionLinkText = TruncateLinkText(linkText);
            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToEditor(postId, sectionId);
    }

    [HttpPost("{id:guid}/text")]
    [HttpPost("sections/{sectionId:guid}/{id:guid}/text")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateText(Guid postId, Guid? sectionId, Guid id, string text, PostContentEditorDto dto)
    {
        var post = await GetEditablePost(postId);
        if (post is null) return RedirectToAction("Details", "Posts", new { id = postId });
        if (string.IsNullOrWhiteSpace(text))
        {
            TempData["Error"] = "Текстовий блок не може бути порожнім.";
            return RedirectToEditor(postId, sectionId);
        }

        var block = await _db.PostContentBlocks.FirstOrDefaultAsync(b =>
            b.Id == id && b.PostId == postId && b.ContainerSectionId == sectionId &&
            (b.Type == PostContentBlockType.Text || b.Type == PostContentBlockType.Template));

        if (block is not null)
        {
            var snapshot = await _versions.CaptureAsync(postId, GetCurrentUserId(), "ContentTextUpdated");
            if (!snapshot.Success)
            {
                TempData["Error"] = snapshot.Error;
                return RedirectToEditor(postId, sectionId);
            }

            block.Text = text.Trim();
            block.TextBold = dto.NewTextBold;
            block.TextItalic = dto.NewTextItalic;
            block.TextUnderline = dto.NewTextUnderline;
            block.TextStrike = dto.NewTextStrike;
            block.TextSize = dto.NewTextSize;
            block.TextAlign = dto.NewTextAlign;
            block.TextStyle = dto.NewTextStyle;
            block.TextColor = PostContentFormatting.SanitizeColor(dto.NewTextColor);
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
            var snapshot = await _versions.CaptureAsync(postId, GetCurrentUserId(), "ContentImageCaptionUpdated");
            if (!snapshot.Success)
            {
                TempData["Error"] = snapshot.Error;
                return RedirectToEditor(postId, sectionId);
            }

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
            var snapshot = await _versions.CaptureAsync(postId, GetCurrentUserId(), "ContentBlockMoved");
            if (!snapshot.Success)
            {
                TempData["Error"] = snapshot.Error;
                return RedirectToEditor(postId, sectionId);
            }

            (blocks[index].Order, blocks[target].Order) = (blocks[target].Order, blocks[index].Order);
            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToEditor(postId, sectionId);
    }

    [HttpPost("reorder")]
    [HttpPost("sections/{sectionId:guid}/reorder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(Guid postId, Guid? sectionId, [FromForm] string orderedIds)
    {
        var isXhr = string.Equals(Request.Headers["X-Requested-With"].ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        var post = await GetEditablePost(postId);
        if (post is null)
            return isXhr ? Json(new { success = false, error = "Немає прав." }) : RedirectToAction("Details", "Posts", new { id = postId });

        var ids = (orderedIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        var blocks = await _db.PostContentBlocks
            .Where(b => b.PostId == postId && b.ContainerSectionId == sectionId)
            .ToListAsync();

        if (ids.Count == 0 || ids.Count != blocks.Count || !ids.All(id => blocks.Any(b => b.Id == id)))
        {
            TempData["Error"] = "Некоректний порядок блоків.";
            return isXhr ? Json(new { success = false, error = TempData["Error"] }) : RedirectToEditor(postId, sectionId);
        }

        var snapshot = await _versions.CaptureAsync(postId, GetCurrentUserId(), "ContentBlocksReordered");
        if (!snapshot.Success)
        {
            TempData["Error"] = snapshot.Error;
            return isXhr ? Json(new { success = false, error = snapshot.Error }) : RedirectToEditor(postId, sectionId);
        }

        for (var i = 0; i < ids.Count; i++)
        {
            var block = blocks.First(b => b.Id == ids[i]);
            block.Order = i;
        }

        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (isXhr)
            return Json(new { success = true });
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
            var snapshot = await _versions.CaptureAsync(postId, GetCurrentUserId(), "ContentBlockDeleted");
            if (!snapshot.Success)
            {
                TempData["Error"] = snapshot.Error;
                return RedirectToEditor(postId, sectionId);
            }

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
            TempData["Error"] = "У вас немає прав на зміну структури публікації.";
            return null;
        }

        return await _db.Posts.FirstAsync(p => p.Id == postId);
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private async Task<PostContentEditorDto> BuildModel(Guid postId, Guid? sectionId, string postTitle)
    {
        var blocks = await _db.PostContentBlocks
            .Include(b => b.Section)
            .Include(b => b.GalleryImages)
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

    private static string TruncateLinkText(string? text)
    {
        var value = (text ?? string.Empty).Trim();
        return value.Length > 240 ? value[..240] : value;
    }

    private static string DefaultTemplateText(PostBlockTemplateType type) => type switch
    {
        PostBlockTemplateType.InfoBox => "Інформація. Коротке пояснення або контекст.",
        PostBlockTemplateType.Warning => "Увага! Опишіть важливе попередження.",
        PostBlockTemplateType.Quote => "«Впишіть цитату сюди». — Автор",
        PostBlockTemplateType.Divider => "---",
        PostBlockTemplateType.FactCard => "Факт: краткое утверждение, которое стоит запомнить.",
        PostBlockTemplateType.LoreBlock => "Лор: опис історії або передісторії світу.",
        PostBlockTemplateType.CharacterStats => "Ім’я: —\nРаса: —\nКлас: —\nСила: —\nСпритність: —\nІнтелект: —",
        _ => string.Empty
    };

    private IActionResult RedirectToEditor(Guid postId, Guid? sectionId) =>
        sectionId is null
            ? RedirectToAction(nameof(Index), new { postId })
            : RedirectToAction(nameof(Index), new { postId, sectionId });
}
