using System.ComponentModel.DataAnnotations;
using HabitKitClone.Data;

namespace HabitKitClone.Models;

public class Category
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(7)]
    public string Color { get; set; } = "#3B82F6"; // Hex color code
    
    [StringLength(10)]
    public string Icon { get; set; } = "📝"; // Emoji or icon identifier
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    public ICollection<Habit> Habits { get; set; } = new List<Habit>();
}
