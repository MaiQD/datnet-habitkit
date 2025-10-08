using System.ComponentModel.DataAnnotations;
using HabitKitClone.Data;

namespace HabitKitClone.Models;

public class HabitCompletion
{
    public int Id { get; set; }
    
    public int HabitId { get; set; }
    public Habit Habit { get; set; } = null!;
    
    public DateOnly CompletionDate { get; set; }
    
    public int Count { get; set; } = 1; // How many times completed on this date
    
    public string? Notes { get; set; }
    
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}
