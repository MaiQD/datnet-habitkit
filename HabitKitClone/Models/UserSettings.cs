using System.ComponentModel.DataAnnotations;
using HabitKitClone.Data;

namespace HabitKitClone.Models;

public class UserSettings
{
    public int Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    public string Language { get; set; } = "en"; // "en" or "vi"
    
    public string Theme { get; set; } = "light"; // "light" or "dark"
    
    public bool EmailNotifications { get; set; } = true;
    
    public bool InAppNotifications { get; set; } = true;
    
    public string DailyReminderTime { get; set; } = "09:00";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}
