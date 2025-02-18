using System.Text.Json;
using DeepNotes.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DeepNotes.Database.SqlServer;

public class DeepNotesDbContext : DbContext
{
    public DbSet<DocumentMetadata> Documents { get; set; } = null!;

    public DeepNotesDbContext(DbContextOptions<DeepNotesDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentMetadata>(entity =>
        {
            entity.ToTable("Documents");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Source).IsRequired();
            entity.Property(e => e.SourceType).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>();

            // Store Properties as JSON
            entity.Property(e => e.Properties)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonSerializerOptions.Default) ??
                         new Dictionary<string, string>()
                );
        });
    }
}