using Microsoft.EntityFrameworkCore;
using TaskManagementApi.Models;

namespace TaskManagementApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.Property(item => item.Name).HasMaxLength(100).IsRequired();
        user.Property(item => item.Email).HasMaxLength(255).IsRequired();
        user.HasIndex(item => item.Email).IsUnique();

        var task = modelBuilder.Entity<TaskItem>();
        task.Property(item => item.Title).HasMaxLength(100).IsRequired();
        task.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        task.Property(item => item.Priority).HasConversion<string>().HasMaxLength(20);
        task.HasOne(item => item.User)
            .WithMany(owner => owner.Tasks)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
