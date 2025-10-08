using AutoMapper;
using HabitKitClone.Data;
using HabitKitClone.DTOs;
using HabitKitClone.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitKitClone.Services;

public class HabitService : IHabitService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public HabitService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<HabitDto>> GetUserHabitsAsync(string userId)
    {
        var habits = await _context.Habits
            .Where(h => h.UserId == userId && h.IsActive)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();

        var habitDtos = new List<HabitDto>();
        
        foreach (var habit in habits)
        {
            var habitDto = _mapper.Map<HabitDto>(habit);
            var stats = await CalculateHabitStatisticsAsync(habit.Id, userId);
            habitDto.CurrentStreak = (int)stats["CurrentStreak"];
            habitDto.LongestStreak = (int)stats["LongestStreak"];
            habitDto.CompletionRate = (double)stats["CompletionRate"];
            habitDto.TotalCompletions = (int)stats["TotalCompletions"];
            habitDtos.Add(habitDto);
        }

        return habitDtos;
    }

    public async Task<HabitDto?> GetHabitByIdAsync(int habitId, string userId)
    {
        var habit = await _context.Habits
            .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);

        if (habit == null) return null;

        var habitDto = _mapper.Map<HabitDto>(habit);
        var stats = await CalculateHabitStatisticsAsync(habit.Id, userId);
        habitDto.CurrentStreak = (int)stats["CurrentStreak"];
        habitDto.LongestStreak = (int)stats["LongestStreak"];
        habitDto.CompletionRate = (double)stats["CompletionRate"];
        habitDto.TotalCompletions = (int)stats["TotalCompletions"];

        return habitDto;
    }

    public async Task<HabitDto> CreateHabitAsync(CreateHabitDto createHabitDto, string userId)
    {
        var habit = _mapper.Map<Habit>(createHabitDto);
        habit.UserId = userId;
        habit.CreatedAt = DateTime.UtcNow;

        _context.Habits.Add(habit);
        await _context.SaveChangesAsync();

        return _mapper.Map<HabitDto>(habit);
    }

    public async Task<HabitDto?> UpdateHabitAsync(int habitId, UpdateHabitDto updateHabitDto, string userId)
    {
        var habit = await _context.Habits
            .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);

        if (habit == null) return null;

        _mapper.Map(updateHabitDto, habit);
        habit.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return _mapper.Map<HabitDto>(habit);
    }

    public async Task<bool> DeleteHabitAsync(int habitId, string userId)
    {
        var habit = await _context.Habits
            .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);

        if (habit == null) return false;

        _context.Habits.Remove(habit);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ToggleHabitCompletionAsync(int habitId, DateOnly date, string userId)
    {
        var existingCompletion = await _context.HabitCompletions
            .FirstOrDefaultAsync(hc => hc.HabitId == habitId && hc.CompletionDate == date && hc.UserId == userId);

        if (existingCompletion != null)
        {
            _context.HabitCompletions.Remove(existingCompletion);
        }
        else
        {
            var completion = new HabitCompletion
            {
                HabitId = habitId,
                CompletionDate = date,
                UserId = userId,
                Count = 1,
                CompletedAt = DateTime.UtcNow
            };
            _context.HabitCompletions.Add(completion);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<HabitCompletionDto?> GetHabitCompletionAsync(int habitId, DateOnly date, string userId)
    {
        var completion = await _context.HabitCompletions
            .FirstOrDefaultAsync(hc => hc.HabitId == habitId && hc.CompletionDate == date && hc.UserId == userId);

        return completion != null ? _mapper.Map<HabitCompletionDto>(completion) : null;
    }

    public async Task<HabitCompletionDto> CreateHabitCompletionAsync(CreateHabitCompletionDto createCompletionDto, string userId)
    {
        var completion = _mapper.Map<HabitCompletion>(createCompletionDto);
        completion.UserId = userId;
        completion.CompletedAt = DateTime.UtcNow;

        _context.HabitCompletions.Add(completion);
        await _context.SaveChangesAsync();

        return _mapper.Map<HabitCompletionDto>(completion);
    }

    public async Task<HabitCompletionDto?> UpdateHabitCompletionAsync(int completionId, UpdateHabitCompletionDto updateCompletionDto, string userId)
    {
        var completion = await _context.HabitCompletions
            .FirstOrDefaultAsync(hc => hc.Id == completionId && hc.UserId == userId);

        if (completion == null) return null;

        _mapper.Map(updateCompletionDto, completion);
        await _context.SaveChangesAsync();

        return _mapper.Map<HabitCompletionDto>(completion);
    }

    public async Task<bool> DeleteHabitCompletionAsync(int completionId, string userId)
    {
        var completion = await _context.HabitCompletions
            .FirstOrDefaultAsync(hc => hc.Id == completionId && hc.UserId == userId);

        if (completion == null) return false;

        _context.HabitCompletions.Remove(completion);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<Dictionary<DateOnly, List<HabitCompletionDto>>> GetHabitCompletionsForMonthAsync(int year, int month, string userId)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var completions = await _context.HabitCompletions
            .Where(hc => hc.UserId == userId && 
                        hc.CompletionDate >= startDate && 
                        hc.CompletionDate <= endDate)
            .Include(hc => hc.Habit)
            .ToListAsync();

        return completions
            .GroupBy(hc => hc.CompletionDate)
            .ToDictionary(
                g => g.Key,
                g => g.Select(_mapper.Map<HabitCompletionDto>).ToList()
            );
    }

    public async Task<Dictionary<DateOnly, List<HabitCompletionDto>>> GetHabitCompletionsForYearAsync(string userId)
    {
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-365));
        var endDate = DateOnly.FromDateTime(DateTime.Today);

        var completions = await _context.HabitCompletions
            .Where(hc => hc.UserId == userId && 
                        hc.CompletionDate >= startDate && 
                        hc.CompletionDate <= endDate)
            .Include(hc => hc.Habit)
            .ToListAsync();

        return completions
            .GroupBy(hc => hc.CompletionDate)
            .ToDictionary(
                g => g.Key,
                g => g.Select(_mapper.Map<HabitCompletionDto>).ToList()
            );
    }

    public async Task<Dictionary<string, object>> GetHabitStatisticsAsync(int habitId, string userId)
    {
        var habit = await _context.Habits
            .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);

        if (habit == null)
        {
            return new Dictionary<string, object>
            {
                ["CurrentStreak"] = 0,
                ["LongestStreak"] = 0,
                ["CompletionRate"] = 0.0,
                ["TotalCompletions"] = 0
            };
        }

        var completions = await _context.HabitCompletions
            .Where(hc => hc.HabitId == habitId && hc.UserId == userId)
            .OrderBy(hc => hc.CompletionDate)
            .ToListAsync();

        var totalCompletions = completions.Sum(hc => hc.Count);
        var completionRate = CalculateCompletionRate(habit, completions);
        var currentStreak = CalculateCurrentStreak(habit, completions);
        var longestStreak = CalculateLongestStreak(habit, completions);

        return new Dictionary<string, object>
        {
            ["CurrentStreak"] = currentStreak,
            ["LongestStreak"] = longestStreak,
            ["CompletionRate"] = completionRate,
            ["TotalCompletions"] = totalCompletions
        };
    }

    private async Task<Dictionary<string, object>> CalculateHabitStatisticsAsync(int habitId, string userId)
    {
        return await GetHabitStatisticsAsync(habitId, userId);
    }

    private double CalculateCompletionRate(Habit habit, List<HabitCompletion> completions)
    {
        var startDate = habit.CreatedAt.Date;
        var endDate = DateTime.UtcNow.Date;
        var totalDays = (endDate - startDate).Days + 1;

        if (totalDays <= 0) return 0.0;

        var completedDays = completions.Count;
        return Math.Round((double)completedDays / totalDays * 100, 1);
    }

    private int CalculateCurrentStreak(Habit habit, List<HabitCompletion> completions)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var streak = 0;
        var currentDate = today;

        while (true)
        {
            var hasCompletion = completions.Any(c => c.CompletionDate == currentDate);
            
            if (hasCompletion)
            {
                streak++;
                currentDate = currentDate.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        return streak;
    }

    private int CalculateLongestStreak(Habit habit, List<HabitCompletion> completions)
    {
        if (!completions.Any()) return 0;

        var sortedCompletions = completions.OrderBy(c => c.CompletionDate).ToList();
        var longestStreak = 0;
        var currentStreak = 1;

        for (int i = 1; i < sortedCompletions.Count; i++)
        {
            var daysDifference = (sortedCompletions[i].CompletionDate.ToDateTime(TimeOnly.MinValue) - 
                                sortedCompletions[i - 1].CompletionDate.ToDateTime(TimeOnly.MinValue)).Days;

            if (daysDifference == 1)
            {
                currentStreak++;
            }
            else
            {
                longestStreak = Math.Max(longestStreak, currentStreak);
                currentStreak = 1;
            }
        }

        return Math.Max(longestStreak, currentStreak);
    }
}
