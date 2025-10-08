using HabitKitClone.Models;

namespace HabitKitClone.Services
{
    public interface IUserSettingsService
    {
        Task<UserSettings?> GetUserSettingsAsync(string userId);
        Task<UserSettings> CreateOrUpdateUserSettingsAsync(string userId, UserSettings settings);
        Task<bool> UpdateLanguageAsync(string userId, string language);
        Task<bool> UpdateThemeAsync(string userId, string theme);
        Task<bool> UpdateEmailNotificationsAsync(string userId, bool enabled);
        Task<bool> UpdateInAppNotificationsAsync(string userId, bool enabled);
        Task<bool> UpdateDailyReminderTimeAsync(string userId, string time);
        Task<bool> DeleteUserSettingsAsync(string userId);
    }
}
