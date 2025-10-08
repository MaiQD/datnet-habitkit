namespace HabitKitClone.DTOs;

public class HabitCompletionDto
{
    public int Id { get; set; }
    public int HabitId { get; set; }
    public DateOnly CompletionDate { get; set; }
    public int Count { get; set; } = 1;
    public string? Notes { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class CreateHabitCompletionDto
{
    public int HabitId { get; set; }
    public DateOnly CompletionDate { get; set; }
    public int Count { get; set; } = 1;
    public string? Notes { get; set; }
}

public class UpdateHabitCompletionDto
{
    public int Count { get; set; } = 1;
    public string? Notes { get; set; }
}
