using HabitKitClone.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HabitKitClone.Data;

public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DataSeeder(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        // Ensure database is created and migrations are applied
        await _context.Database.EnsureCreatedAsync();

        // Create demo user if it doesn't exist
        var demoUser = await _userManager.FindByEmailAsync("demo@habitkit.com");
        if (demoUser == null)
        {
            demoUser = new ApplicationUser
            {
                UserName = "demo@habitkit.com",
                Email = "demo@habitkit.com",
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(demoUser, "Demo123!");
        }

        // Create demo categories if they don't exist
        if (!await _context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new Category
                {
                    Name = "Health & Fitness",
                    Color = "#10B981",
                    Icon = "💪",
                    UserId = demoUser.Id
                },
                new Category
                {
                    Name = "Learning",
                    Color = "#3B82F6",
                    Icon = "📚",
                    UserId = demoUser.Id
                },
                new Category
                {
                    Name = "Productivity",
                    Color = "#F59E0B",
                    Icon = "⚡",
                    UserId = demoUser.Id
                },
                new Category
                {
                    Name = "Mindfulness",
                    Color = "#8B5CF6",
                    Icon = "🧘",
                    UserId = demoUser.Id
                }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();
        }

        // Get the first category for demo habits
        var defaultCategory = await _context.Categories.FirstAsync();

        // Create demo habits if they don't exist
        if (!await _context.Habits.AnyAsync())
        {
            var habits = new List<Habit>
            {
                new Habit
                {
                    Name = "Drink Water",
                    Description = "Drink 8 glasses of water daily",
                    Color = "#3B82F6",
                    Icon = "💧",
                    Frequency = HabitFrequency.Daily,
                    TargetCount = 8,
                    ReminderTime = new TimeOnly(9, 0),
                    IsActive = true,
                    UserId = demoUser.Id,
                    CategoryId = defaultCategory.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Habit
                {
                    Name = "Exercise",
                    Description = "30 minutes of physical activity",
                    Color = "#10B981",
                    Icon = "🏃",
                    Frequency = HabitFrequency.Daily,
                    TargetCount = 1,
                    ReminderTime = new TimeOnly(18, 0),
                    IsActive = true,
                    UserId = demoUser.Id,
                    CategoryId = defaultCategory.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-25)
                },
                new Habit
                {
                    Name = "Read Books",
                    Description = "Read for at least 30 minutes",
                    Color = "#F59E0B",
                    Icon = "📚",
                    Frequency = HabitFrequency.Daily,
                    TargetCount = 1,
                    ReminderTime = new TimeOnly(20, 0),
                    IsActive = true,
                    UserId = demoUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                new Habit
                {
                    Name = "Meditation",
                    Description = "10 minutes of mindfulness practice",
                    Color = "#8B5CF6",
                    Icon = "🧘",
                    Frequency = HabitFrequency.Daily,
                    TargetCount = 1,
                    ReminderTime = new TimeOnly(7, 0),
                    IsActive = true,
                    UserId = demoUser.Id,
                    CategoryId = defaultCategory.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new Habit
                {
                    Name = "Journal Writing",
                    Description = "Write in personal journal",
                    Color = "#EF4444",
                    Icon = "📝",
                    Frequency = HabitFrequency.Daily,
                    TargetCount = 1,
                    ReminderTime = new TimeOnly(21, 0),
                    IsActive = true,
                    UserId = demoUser.Id,
                    CategoryId = defaultCategory.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            };

            _context.Habits.AddRange(habits);
            await _context.SaveChangesAsync();

            // Create some demo completions
            var habitIds = habits.Select(h => h.Id).ToList();
            var completions = new List<HabitCompletion>();

            for (int i = 0; i < 30; i++)
            {
                var date = DateOnly.FromDateTime(DateTime.Today.AddDays(-i));
                
                foreach (var habitId in habitIds)
                {
                    // Randomly complete habits (70% chance)
                    if (Random.Shared.NextDouble() < 0.7)
                    {
                        completions.Add(new HabitCompletion
                        {
                            HabitId = habitId,
                            CompletionDate = date,
                            Count = 1,
                            UserId = demoUser.Id,
                            CompletedAt = DateTime.UtcNow.AddDays(-i).AddHours(Random.Shared.Next(6, 22))
                        });
                    }
                }
            }

            _context.HabitCompletions.AddRange(completions);
            await _context.SaveChangesAsync();
        }

        // Create user settings if they don't exist
        if (!await _context.UserSettings.AnyAsync())
        {
            var userSettings = new UserSettings
            {
                UserId = demoUser.Id,
                Language = "en",
                Theme = "light",
                EmailNotifications = true,
                InAppNotifications = true,
                DailyReminderTime = "09:00",
                CreatedAt = DateTime.UtcNow
            };

            _context.UserSettings.Add(userSettings);
            await _context.SaveChangesAsync();
        }
    }
}
