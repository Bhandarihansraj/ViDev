using Microsoft.EntityFrameworkCore;
using ViDev.Api.Data.Entities;

namespace ViDev.Api.Data;

/// <summary>
/// EF Core DbContext for ViDev.
/// Uses PostgreSQL with JSONB columns for AST and wiring data.
/// </summary>
public class ViDevDbContext : DbContext
{
    public ViDevDbContext(DbContextOptions<ViDevDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<GenerationJob> GenerationJobs => Set<GenerationJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- User ---
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.AuthProviderId).IsUnique();
        });

        // --- Template ---
        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasIndex(t => t.Name);
            entity.HasIndex(t => t.AuthorId);

            // PostgreSQL text array for tags
            entity.Property(t => t.Tags)
                  .HasColumnType("text[]");

            // JSONB columns for AST and wiring
            entity.Property(t => t.AstJson)
                  .HasColumnType("jsonb");
            entity.Property(t => t.WiringJson)
                  .HasColumnType("jsonb");

            // Relationship
            entity.HasOne(t => t.Author)
                  .WithMany(u => u.Templates)
                  .HasForeignKey(t => t.AuthorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- GenerationJob ---
        modelBuilder.Entity<GenerationJob>(entity =>
        {
            entity.HasIndex(j => j.TemplateId);

            entity.HasOne(j => j.Template)
                  .WithMany()
                  .HasForeignKey(j => j.TemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
