using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HabitKitClone.Models;

namespace HabitKitClone.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Habit> Habits { get; set; }
    public DbSet<HabitCompletion> HabitCompletions { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Habit entity
        builder.Entity<Habit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Color).HasMaxLength(7).HasDefaultValue("#3B82F6");
            entity.Property(e => e.Icon).HasMaxLength(10).HasDefaultValue("📝");
            entity.Property(e => e.Frequency).HasConversion<string>();
            entity.Property(e => e.TargetCount).HasDefaultValue(1);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure HabitCompletion entity
        builder.Entity<HabitCompletion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Count).HasDefaultValue(1);
            entity.Property(e => e.CompletedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Habit)
                .WithMany(h => h.Completions)
                .HasForeignKey(e => e.HabitId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Unique constraint for one completion per habit per date per user
            entity.HasIndex(e => new { e.HabitId, e.CompletionDate, e.UserId })
                .IsUnique();
        });

        // Configure UserSettings entity
        builder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Language).HasMaxLength(2).HasDefaultValue("en");
            entity.Property(e => e.Theme).HasMaxLength(10).HasDefaultValue("light");
            entity.Property(e => e.EmailNotifications).HasDefaultValue(true);
            entity.Property(e => e.InAppNotifications).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // One settings per user
            entity.HasIndex(e => e.UserId).IsUnique();
        });
    }
}
