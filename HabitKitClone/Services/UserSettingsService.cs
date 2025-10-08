using HabitKitClone.Data;
using HabitKitClone.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitKitClone.Services
{
    public class UserSettingsService : IUserSettingsService
    {
        private readonly ApplicationDbContext _context;

        public UserSettingsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserSettings?> GetUserSettingsAsync(string userId)
        {
            return await _context.UserSettings
                .FirstOrDefaultAsync(us => us.UserId == userId);
        }

        public async Task<UserSettings> CreateOrUpdateUserSettingsAsync(string userId, UserSettings settings)
        {
            Console.WriteLine($"UserSettingsService.CreateOrUpdateUserSettingsAsync called - userId: {userId}");
            
            var existingSettings = await GetUserSettingsAsync(userId);
            Console.WriteLine($"UserSettingsService - existingSettings found: {existingSettings != null}");
            
            if (existingSettings == null)
            {
                Console.WriteLine("UserSettingsService - Creating new settings");
                settings.UserId = userId;
                settings.CreatedAt = DateTime.UtcNow;
                _context.UserSettings.Add(settings);
            }
            else
            {
                Console.WriteLine("UserSettingsService - Updating existing settings");
                existingSettings.Language = settings.Language;
                existingSettings.Theme = settings.Theme;
                existingSettings.EmailNotifications = settings.EmailNotifications;
                existingSettings.InAppNotifications = settings.InAppNotifications;
                existingSettings.DailyReminderTime = settings.DailyReminderTime;
                existingSettings.UpdatedAt = DateTime.UtcNow;
            }

            Console.WriteLine("UserSettingsService - Saving changes to database");
            await _context.SaveChangesAsync();
            Console.WriteLine("UserSettingsService - Changes saved successfully");
            
            return existingSettings ?? settings;
        }

        public async Task<bool> UpdateLanguageAsync(string userId, string language)
        {
            var settings = await GetUserSettingsAsync(userId);
            if (settings == null)
            {
                settings = new UserSettings
                {
                    UserId = userId,
                    Language = language,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.Language = language;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateThemeAsync(string userId, string theme)
        {
            var settings = await GetUserSettingsAsync(userId);
            if (settings == null)
            {
                settings = new UserSettings
                {
                    UserId = userId,
                    Theme = theme,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.Theme = theme;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateEmailNotificationsAsync(string userId, bool enabled)
        {
            var settings = await GetUserSettingsAsync(userId);
            if (settings == null)
            {
                settings = new UserSettings
                {
                    UserId = userId,
                    EmailNotifications = enabled,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.EmailNotifications = enabled;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateInAppNotificationsAsync(string userId, bool enabled)
        {
            var settings = await GetUserSettingsAsync(userId);
            if (settings == null)
            {
                settings = new UserSettings
                {
                    UserId = userId,
                    InAppNotifications = enabled,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.InAppNotifications = enabled;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateDailyReminderTimeAsync(string userId, string time)
        {
            var settings = await GetUserSettingsAsync(userId);
            if (settings == null)
            {
                settings = new UserSettings
                {
                    UserId = userId,
                    DailyReminderTime = time,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.DailyReminderTime = time;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserSettingsAsync(string userId)
        {
            var settings = await GetUserSettingsAsync(userId);
            if (settings != null)
            {
                _context.UserSettings.Remove(settings);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
