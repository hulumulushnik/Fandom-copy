using System.Reflection.Emit;
using Fandom_copy.Models;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<PostSection> PostSections => Set<PostSection>();
        public DbSet<PostContentBlock> PostContentBlocks => Set<PostContentBlock>();
        public DbSet<PostGalleryImage> PostGalleryImages => Set<PostGalleryImage>();
        public DbSet<PostMember> PostMembers => Set<PostMember>();
        public DbSet<SavedPost> SavedPosts => Set<SavedPost>();
        public DbSet<PostHistory> PostHistories => Set<PostHistory>();
        public DbSet<PostVersion> PostVersions => Set<PostVersion>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<Images> Images => Set<Images>();
        public DbSet<FileAttachment> Attachments => Set<FileAttachment>();
        public DbSet<CodeBlock> CodeBlocks => Set<CodeBlock>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Login).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Post>()
                .HasMany(p => p.Tags)
                .WithMany(t => t.Posts)
                .UsingEntity<Dictionary<string, object>>(
                    "PostTags",
                    right => right
                        .HasOne<Tag>()
                        .WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade),
                    left => left
                        .HasOne<Post>()
                        .WithMany()
                        .HasForeignKey("PostId")
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.ToTable("PostTags");
                        join.HasKey("PostId", "TagId");
                    });

            modelBuilder.Entity<PostSection>()
                .HasOne(s => s.ParentSection)
                .WithMany(s => s.SubSections)
                .HasForeignKey(s => s.ParentSectionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostContentBlock>()
                .HasOne(b => b.Section)
                .WithMany()
                .HasForeignKey(b => b.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostContentBlock>()
                .HasIndex(b => new { b.PostId, b.ContainerSectionId, b.Order });

            modelBuilder.Entity<PostContentBlock>()
                .HasMany(b => b.GalleryImages)
                .WithOne(g => g.Block!)
                .HasForeignKey(g => g.PostContentBlockId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostGalleryImage>()
                .HasIndex(g => new { g.PostContentBlockId, g.Order });

            modelBuilder.Entity<FileAttachment>(entity =>
            {
                entity.ToTable("Attachments");

                entity.HasOne(a => a.PostSection)
                    .WithMany(s => s.Files)
                    .HasForeignKey(a => a.PostSectionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(a => a.PostSectionId);
            });

            modelBuilder.Entity<PostMember>()
                .HasOne(pm => pm.User)
                .WithMany()
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostHistory>()
                .HasOne(h => h.User)
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostVersion>(entity =>
            {
                entity.HasIndex(v => new { v.PostId, v.CreatedAt });

                entity.HasOne(v => v.Post)
                    .WithMany()
                    .HasForeignKey(v => v.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(v => v.User)
                    .WithMany()
                    .HasForeignKey(v => v.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SavedPost>(entity =>
            {
                entity.HasIndex(s => new { s.UserId, s.PostId }).IsUnique();

                entity.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Post)
                    .WithMany()
                    .HasForeignKey(s => s.PostId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
