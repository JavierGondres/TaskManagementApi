using Microsoft.EntityFrameworkCore;
using TaskManagementApi.Models;

namespace TaskManagementApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var task = modelBuilder.Entity<TaskItem>();

        task.Property(item => item.Title).HasMaxLength(100).IsRequired();
        task.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        task.Property(item => item.Priority).HasConversion<string>().HasMaxLength(20);
    }
}
