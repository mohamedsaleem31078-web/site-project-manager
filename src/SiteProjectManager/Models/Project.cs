using Microsoft.EntityFrameworkCore;
using SiteProjectManager.Models;

namespace SiteProjectManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Snag> Snags => Set<Snag>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Letter> Letters => Set<Letter>();
    public DbSet<LetterTemplate> LetterTemplates => Set<LetterTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired();
            entity.Property(p => p.ClientName).IsRequired();
        });

        modelBuilder.Entity<Snag>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Title).IsRequired();
            entity.Property(s => s.Priority).HasDefaultValue("Medium");
            entity.HasOne(s => s.Project)
                .WithMany(p => p.Snags)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasOne(a => a.Snag)
                .WithMany(s => s.Attachments)
                .HasForeignKey(a => a.SnagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Letter>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.HasOne(l => l.Project)
                .WithMany()
                .HasForeignKey(l => l.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LetterTemplate>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired();
        });

        modelBuilder.Entity<LetterTemplate>().HasData(
            new LetterTemplate
            {
                Id = 1,
                Name = "Site Observation Letter",
                Subject = "Site Observation Summary",
                Body = "Dear Client,\n\nThis letter summarizes the current site observations and outstanding issues. Please review the attached items and advise on the required actions.\n\nKind regards,\nProject Team",
                IsDefault = true
            });

        base.OnModelCreating(modelBuilder);
    }
}


























































