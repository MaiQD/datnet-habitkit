using HabitKitClone.Models;

namespace HabitKitClone.DTOs;

public class HabitDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty; // For display purposes
    public string Color { get; set; } = "#3B82F6";
    public string Icon { get; set; } = "📝";
    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;
    public int TargetCount { get; set; } = 1;
    public TimeOnly? ReminderTime { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public double CompletionRate { get; set; }
    public int TotalCompletions { get; set; }
}

public class CreateHabitDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string Color { get; set; } = "#3B82F6";
    public string Icon { get; set; } = "📝";
    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;
    public int TargetCount { get; set; } = 1;
    public TimeOnly? ReminderTime { get; set; }
}

public class UpdateHabitDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string Color { get; set; } = "#3B82F6";
    public string Icon { get; set; } = "📝";
    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;
    public int TargetCount { get; set; } = 1;
    public TimeOnly? ReminderTime { get; set; }
    public bool IsActive { get; set; } = true;
}
