using System.ComponentModel.DataAnnotations;
using HabitKitClone.Data;

namespace HabitKitClone.Models;

public class Habit
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public int CategoryId { get; set; } // Foreign key to Category
    public Category Category { get; set; } = null!; // Navigation property
    
    public string Color { get; set; } = "#3B82F6"; // Default blue color
    
    public string Icon { get; set; } = "📝"; // Default icon
    
    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;
    
    public int TargetCount { get; set; } = 1; // How many times per day/week/month
    
    public TimeOnly? ReminderTime { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Pre-calculated statistics
    public int TotalCompletions { get; set; } = 0;
    public int BestStreak { get; set; } = 0;
    public int CurrentStreak { get; set; } = 0;
    
    // Navigation properties
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    public ICollection<HabitCompletion> Completions { get; set; } = new List<HabitCompletion>();
}

public enum HabitFrequency
{
    Daily,
    Weekly,
    Monthly
}
