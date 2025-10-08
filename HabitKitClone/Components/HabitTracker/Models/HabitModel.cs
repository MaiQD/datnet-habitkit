namespace HabitKitClone.Components.HabitTracker;

public class HabitModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "study";
    public string Icon { get; set; } = "📝";
    public string Color { get; set; } = "#238636";
    public bool IsCompletedToday { get; set; }
    public int StreakCount { get; set; }
    public List<ContributionDay> ContributionData { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastCompletedAt { get; set; }
}

public class ContributionDay
{
    public DateTime Date { get; set; }
    public int Level { get; set; } // 0-4, where 4 is highest activity
    public string Color { get; set; } = "#238636";
    public int Count { get; set; } = 0;
}
