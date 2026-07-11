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
        public DbSet<PostMember> PostMembers => Set<PostMember>();
        public DbSet<SavedPost> SavedPosts => Set<SavedPost>();
        public DbSet<PostHistory> PostHistories => Set<PostHistory>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<Images> Images => Set<Images>();
        public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();
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
                .WithMany(t => t.Posts);

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
