namespace HabitKitClone.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F6";
    public string Icon { get; set; } = "📝";
    public bool IsActive { get; set; } = true;
    public int HabitCount { get; set; } = 0; // Number of habits in this category
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F6";
    public string Icon { get; set; } = "📝";
}

public class UpdateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F6";
    public string Icon { get; set; } = "📝";
    public bool IsActive { get; set; } = true;
}
