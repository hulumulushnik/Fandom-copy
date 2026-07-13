using System.Text.Json;
using Fandom_copy.Data;
using Fandom_copy.DTOs.Posts;
using Fandom_copy.Models;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Services;

public sealed class PostVersionService : IPostVersionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ApplicationDbContext _db;

    public PostVersionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult> CaptureAsync(Guid postId, Guid userId, string action)
    {
        var snapshot = await BuildSnapshotAsync(postId);
        if (snapshot is null)
            return ServiceResult.Fail("Пост не знайдено для створення резервної версії");

        _db.PostVersions.Add(new PostVersion
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            UserId = userId,
            Action = action,
            SnapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions),
            CreatedAt = DateTime.UtcNow
        });

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<List<PostVersionDto>>> GetVersionsAsync(Guid postId, Guid userId, int take = 10)
    {
        if (!await IsOwnerAsync(postId, userId))
            return ServiceResult<List<PostVersionDto>>.Fail("Лише власник може переглядати версії поста");

        if (take <= 0) take = 10;
        if (take > 50) take = 50;

        var versions = await _db.PostVersions
            .Include(v => v.User)
            .Where(v => v.PostId == postId)
            .OrderByDescending(v => v.CreatedAt)
            .Take(take)
            .ToListAsync();

        return ServiceResult<List<PostVersionDto>>.Ok(versions.Select(PostVersionDto.FromEntity).ToList());
    }

    public async Task<ServiceResult> RestoreAsync(Guid postId, Guid versionId, Guid userId)
    {
        if (!await IsOwnerAsync(postId, userId))
            return ServiceResult.Fail("Лише власник може відкотити пост");

        var version = await _db.PostVersions
            .FirstOrDefaultAsync(v => v.Id == versionId && v.PostId == postId);

        if (version is null)
            return ServiceResult.Fail("Версію не знайдено");

        var snapshot = JsonSerializer.Deserialize<PostSnapshot>(version.SnapshotJson, JsonOptions);
        if (snapshot is null)
            return ServiceResult.Fail("Не вдалося прочитати збережену версію");

        var currentSnapshot = await CaptureAsync(postId, userId, "BeforeRestore");
        if (!currentSnapshot.Success)
            return currentSnapshot;

        var post = await _db.Posts
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post is null)
            return ServiceResult.Fail("Пост не знайдено");

        await _db.Attachments
            .Where(a => _db.PostSections
                .Where(s => s.PostId == postId)
                .Select(s => s.Id)
                .Contains(a.PostSectionId))
            .ExecuteDeleteAsync();

        await _db.PostContentBlocks
            .Where(b => b.PostId == postId)
            .ExecuteDeleteAsync();

        var remainingSections = await _db.PostSections
            .AsNoTracking()
            .Where(s => s.PostId == postId)
            .Select(s => new { s.Id, s.ParentSectionId })
            .ToListAsync();

        while (remainingSections.Count > 0)
        {
            var parentIds = remainingSections
                .Where(s => s.ParentSectionId is not null)
                .Select(s => s.ParentSectionId!.Value)
                .ToHashSet();

            var leafIds = remainingSections
                .Where(s => !parentIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToList();

            if (leafIds.Count == 0)
                leafIds = remainingSections.Select(s => s.Id).ToList();

            await _db.PostSections
                .Where(s => leafIds.Contains(s.Id))
                .ExecuteDeleteAsync();

            remainingSections.RemoveAll(s => leafIds.Contains(s.Id));
        }

        post.Title = snapshot.Title;
        post.Description = snapshot.Description;
        post.IsPublic = snapshot.IsPublic;
        post.IconPath = snapshot.IconPath;
        post.IsDeleted = false;
        post.UpdatedAt = DateTime.UtcNow;

        if (await _db.Categories.AnyAsync(c => c.Id == snapshot.CategoryId))
            post.CategoryId = snapshot.CategoryId;

        await ApplyTagsAsync(post, snapshot.Tags);

        _db.PostSections.AddRange(OrderSectionsForInsert(snapshot.Sections).Select(s => new PostSection
        {
            Id = s.Id,
            Title = s.Title,
            Text = s.Text,
            Order = s.Order,
            PostId = postId,
            IconPath = s.IconPath,
            ParentSectionId = s.ParentSectionId
        }));

        _db.PostContentBlocks.AddRange(snapshot.ContentBlocks.Select(b => new PostContentBlock
        {
            Id = b.Id,
            PostId = postId,
            ContainerSectionId = b.ContainerSectionId,
            Type = b.Type,
            Text = b.Text,
            ImagePath = b.ImagePath,
            ImageCaption = b.ImageCaption,
            SectionId = b.SectionId,
            Order = b.Order
        }));

        _db.Attachments.AddRange(snapshot.Attachments.Select(a => new FileAttachment
        {
            Id = a.Id,
            FileName = a.FileName,
            Path = a.Path,
            Size = a.Size,
            PostSectionId = a.PostSectionId
        }));

        _db.PostHistories.Add(new PostHistory
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            UserId = userId,
            Action = "Restored",
            Date = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    private async Task<PostSnapshot?> BuildSnapshotAsync(Guid postId)
    {
        var post = await _db.Posts
            .AsNoTracking()
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post is null)
            return null;

        var sections = await _db.PostSections
            .AsNoTracking()
            .Where(s => s.PostId == postId)
            .OrderBy(s => s.ParentSectionId)
            .ThenBy(s => s.Order)
            .ToListAsync();

        var sectionIds = sections.Select(s => s.Id).ToList();

        var blocks = await _db.PostContentBlocks
            .AsNoTracking()
            .Where(b => b.PostId == postId)
            .OrderBy(b => b.ContainerSectionId)
            .ThenBy(b => b.Order)
            .ToListAsync();

        var attachments = sectionIds.Count == 0
            ? new List<FileAttachment>()
            : await _db.Attachments
                .AsNoTracking()
                .Where(a => sectionIds.Contains(a.PostSectionId))
                .OrderBy(a => a.FileName)
                .ToListAsync();

        return new PostSnapshot
        {
            Title = post.Title,
            Description = post.Description,
            CategoryId = post.CategoryId,
            IsPublic = post.IsPublic,
            IconPath = post.IconPath,
            Tags = post.Tags.OrderBy(t => t.Name).Select(t => t.Name).ToList(),
            Sections = sections.Select(s => new SectionSnapshot
            {
                Id = s.Id,
                Title = s.Title,
                Text = s.Text,
                Order = s.Order,
                IconPath = s.IconPath,
                ParentSectionId = s.ParentSectionId
            }).ToList(),
            ContentBlocks = blocks.Select(b => new ContentBlockSnapshot
            {
                Id = b.Id,
                ContainerSectionId = b.ContainerSectionId,
                Type = b.Type,
                Text = b.Text,
                ImagePath = b.ImagePath,
                ImageCaption = b.ImageCaption,
                SectionId = b.SectionId,
                Order = b.Order
            }).ToList(),
            Attachments = attachments.Select(a => new AttachmentSnapshot
            {
                Id = a.Id,
                FileName = a.FileName,
                Path = a.Path,
                Size = a.Size,
                PostSectionId = a.PostSectionId
            }).ToList()
        };
    }

    private async Task<bool> IsOwnerAsync(Guid postId, Guid userId)
    {
        return await _db.PostMembers
            .AnyAsync(m => m.PostId == postId && m.UserId == userId && m.Role == PostRole.Owner);
    }

    private async Task ApplyTagsAsync(Post post, List<string> tagNames)
    {
        post.Tags.Clear();

        var names = tagNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();

        if (names.Count == 0)
            return;

        var loweredNames = names.Select(n => n.ToLower()).ToList();
        var existingTags = await _db.Tags
            .Where(t => loweredNames.Contains(t.Name.ToLower()))
            .ToListAsync();

        foreach (var name in names)
        {
            var tag = existingTags.FirstOrDefault(t =>
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

            if (tag is null)
            {
                tag = new Tag
                {
                    Id = Guid.NewGuid(),
                    Name = name
                };

                _db.Tags.Add(tag);
                existingTags.Add(tag);
            }

            post.Tags.Add(tag);
        }
    }

    private static List<SectionSnapshot> OrderSectionsForInsert(List<SectionSnapshot> sections)
    {
        var remaining = sections.ToList();
        var ordered = new List<SectionSnapshot>();
        var inserted = new HashSet<Guid>();

        while (remaining.Count > 0)
        {
            var ready = remaining
                .Where(s => s.ParentSectionId is null || inserted.Contains(s.ParentSectionId.Value))
                .OrderBy(s => s.Order)
                .ThenBy(s => s.Title)
                .ToList();

            if (ready.Count == 0)
                ready = remaining.OrderBy(s => s.Order).ThenBy(s => s.Title).ToList();

            foreach (var section in ready)
            {
                ordered.Add(section);
                inserted.Add(section.Id);
                remaining.Remove(section);
            }
        }

        return ordered;
    }

    private sealed class PostSnapshot
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public bool IsPublic { get; set; }
        public string? IconPath { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<SectionSnapshot> Sections { get; set; } = new();
        public List<ContentBlockSnapshot> ContentBlocks { get; set; } = new();
        public List<AttachmentSnapshot> Attachments { get; set; } = new();
    }

    private sealed class SectionSnapshot
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
        public string? IconPath { get; set; }
        public Guid? ParentSectionId { get; set; }
    }

    private sealed class ContentBlockSnapshot
    {
        public Guid Id { get; set; }
        public Guid? ContainerSectionId { get; set; }
        public PostContentBlockType Type { get; set; }
        public string Text { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string ImageCaption { get; set; } = string.Empty;
        public Guid? SectionId { get; set; }
        public int Order { get; set; }
    }

    private sealed class AttachmentSnapshot
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public Guid PostSectionId { get; set; }
    }
}
