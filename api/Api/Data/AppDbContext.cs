using Microsoft.EntityFrameworkCore;
using Api.Models.Entities;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ThreadDraft> ThreadDrafts => Set<ThreadDraft>();
    public DbSet<BrandGuideline> BrandGuidelines => Set<BrandGuideline>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Roles).HasMaxLength(512);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.Property(t => t.TokenHash).IsRequired();
            entity.Property(t => t.ExpiresAt).IsRequired();

            entity.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ThreadDraft>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.ClientId);

            entity.Property(t => t.ClientId).IsRequired().HasMaxLength(128);
            entity.Property(t => t.PromptJson).IsRequired().HasColumnType("jsonb");
            entity.Property(t => t.OutputJson).IsRequired().HasColumnType("jsonb");
            entity.Property(t => t.Provider).IsRequired().HasMaxLength(64);
            entity.Property(t => t.Model).IsRequired().HasMaxLength(128);
            entity.Property(t => t.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<BrandGuideline>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Text).IsRequired().HasMaxLength(1500);
            entity.Property(b => b.UpdatedAt).IsRequired();
        });
    }
}
