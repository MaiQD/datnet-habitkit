using HabitKitClone.DTOs;

namespace HabitKitClone.Services;

public interface IHabitService
{
    Task<IEnumerable<HabitDto>> GetUserHabitsAsync(string userId);
    Task<HabitDto?> GetHabitByIdAsync(int habitId, string userId);
    Task<HabitDto> CreateHabitAsync(CreateHabitDto createHabitDto, string userId);
    Task<HabitDto?> UpdateHabitAsync(int habitId, UpdateHabitDto updateHabitDto, string userId);
    Task<bool> DeleteHabitAsync(int habitId, string userId);
    Task<bool> ToggleHabitCompletionAsync(int habitId, DateOnly date, string userId);
    Task<HabitCompletionDto?> GetHabitCompletionAsync(int habitId, DateOnly date, string userId);
    Task<HabitCompletionDto> CreateHabitCompletionAsync(CreateHabitCompletionDto createCompletionDto, string userId);
    Task<HabitCompletionDto?> UpdateHabitCompletionAsync(int completionId, UpdateHabitCompletionDto updateCompletionDto, string userId);
    Task<bool> DeleteHabitCompletionAsync(int completionId, string userId);
    Task<Dictionary<DateOnly, List<HabitCompletionDto>>> GetHabitCompletionsForMonthAsync(int year, int month, string userId);
    Task<Dictionary<DateOnly, List<HabitCompletionDto>>> GetHabitCompletionsForYearAsync(string userId);
    Task<Dictionary<string, object>> GetHabitStatisticsAsync(int habitId, string userId);
}
