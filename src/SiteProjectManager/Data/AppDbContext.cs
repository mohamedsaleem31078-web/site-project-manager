using Microsoft.EntityFrameworkCore;
using SiteProjectManager.Models;

namespace SiteProjectManager.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Snag> Snags => Set<Snag>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Letter> Letters => Set<Letter>();
    public DbSet<LetterTemplate> LetterTemplates => Set<LetterTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<Project>().HasMany(x => x.Snags).WithOne(x => x.Project)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Snag>().Property(x => x.Title).HasMaxLength(250).IsRequired();
        modelBuilder.Entity<Snag>().HasMany(x => x.Attachments).WithOne(x => x.Snag)
            .HasForeignKey(x => x.SnagId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Report>().HasOne(x => x.Project).WithMany()
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Letter>().HasOne(x => x.Project).WithMany()
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}
