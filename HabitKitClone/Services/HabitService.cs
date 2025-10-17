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
            .Include(h => h.Category)
            .Where(h => h.UserId == userId && h.IsActive)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();

        var habitDtos = new List<HabitDto>();
        
        foreach (var habit in habits)
        {
            var habitDto = _mapper.Map<HabitDto>(habit);
            // Use pre-calculated statistics from database
            habitDto.CurrentStreak = habit.CurrentStreak;
            habitDto.LongestStreak = habit.BestStreak;
            habitDto.TotalCompletions = habit.TotalCompletions;
            // Calculate completion rate on-demand
            habitDto.CompletionRate = await CalculateCompletionRateAsync(habit.Id, userId);
            habitDtos.Add(habitDto);
        }

        return habitDtos;
    }

    public async Task<HabitDto?> GetHabitByIdAsync(int habitId, string userId)
    {
        var habit = await _context.Habits
            .Include(h => h.Category)
            .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);

        if (habit == null) return null;

        var habitDto = _mapper.Map<HabitDto>(habit);
        // Use pre-calculated statistics from database
        habitDto.CurrentStreak = habit.CurrentStreak;
        habitDto.LongestStreak = habit.BestStreak;
        habitDto.TotalCompletions = habit.TotalCompletions;
        // Calculate completion rate on-demand
        habitDto.CompletionRate = await CalculateCompletionRateAsync(habit.Id, userId);

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

    public async Task<int> IncrementHabitCompletionAsync(int habitId, DateOnly date, string userId)
    {
        var existingCompletion = await _context.HabitCompletions
            .FirstOrDefaultAsync(hc => hc.HabitId == habitId && hc.CompletionDate == date && hc.UserId == userId);

        if (existingCompletion != null)
        {
            existingCompletion.Count++;
            existingCompletion.CompletedAt = DateTime.UtcNow;
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
        
        // Update habit statistics
        await RecalculateHabitStatisticsAsync(habitId, userId);
        
        // Return the new count
        var updatedCompletion = await _context.HabitCompletions
            .FirstOrDefaultAsync(hc => hc.HabitId == habitId && hc.CompletionDate == date && hc.UserId == userId);
        
        return updatedCompletion?.Count ?? 0;
    }

    public async Task<int> DecrementHabitCompletionAsync(int habitId, DateOnly date, string userId)
    {
        var existingCompletion = await _context.HabitCompletions
            .FirstOrDefaultAsync(hc => hc.HabitId == habitId && hc.CompletionDate == date && hc.UserId == userId);

        if (existingCompletion != null)
        {
            existingCompletion.Count--;
            if (existingCompletion.Count <= 0)
            {
                _context.HabitCompletions.Remove(existingCompletion);
                await _context.SaveChangesAsync();
                // Update habit statistics
                await RecalculateHabitStatisticsAsync(habitId, userId);
                return 0;
            }
            else
            {
                await _context.SaveChangesAsync();
                // Update habit statistics
                await RecalculateHabitStatisticsAsync(habitId, userId);
                return existingCompletion.Count;
            }
        }

        return 0; // No completion exists, so count remains 0
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

        var habitId = completion.HabitId;
        _context.HabitCompletions.Remove(completion);
        await _context.SaveChangesAsync();

        // Update habit statistics
        await RecalculateHabitStatisticsAsync(habitId, userId);

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

        // Use pre-calculated statistics from database
        var completionRate = await CalculateCompletionRateAsync(habitId, userId);

        return new Dictionary<string, object>
        {
            ["CurrentStreak"] = habit.CurrentStreak,
            ["LongestStreak"] = habit.BestStreak,
            ["CompletionRate"] = completionRate,
            ["TotalCompletions"] = habit.TotalCompletions
        };
    }

    private async Task<Dictionary<string, object>> CalculateHabitStatisticsAsync(int habitId, string userId)
    {
        return await GetHabitStatisticsAsync(habitId, userId);
    }

    private async Task RecalculateHabitStatisticsAsync(int habitId, string userId)
    {
        var habit = await _context.Habits
            .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);

        if (habit == null) return;

        var completions = await _context.HabitCompletions
            .Where(hc => hc.HabitId == habitId && hc.UserId == userId)
            .OrderBy(hc => hc.CompletionDate)
            .ToListAsync();

        // Calculate total completions
        habit.TotalCompletions = completions.Sum(hc => hc.Count);

        // Calculate streaks
        var (currentStreak, bestStreak) = await CalculateStreaksAsync(habitId, userId);
        habit.CurrentStreak = currentStreak;
        habit.BestStreak = Math.Max(habit.BestStreak, bestStreak); // Never decrease best streak

        await _context.SaveChangesAsync();
    }

    private async Task<double> CalculateCompletionRateAsync(int habitId, string userId)
    {
        var habit = await _context.Habits
            .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);

        if (habit == null) return 0.0;

        var startDate = habit.CreatedAt.Date;
        var endDate = DateTime.UtcNow.Date;
        var totalDays = (endDate - startDate).Days + 1;

        if (totalDays <= 0) return 0.0;

        var completedDays = await _context.HabitCompletions
            .Where(hc => hc.HabitId == habitId && hc.UserId == userId)
            .CountAsync();

        return Math.Round((double)completedDays / totalDays * 100, 1);
    }

    private async Task<(int currentStreak, int bestStreak)> CalculateStreaksAsync(int habitId, string userId)
    {
        var completions = await _context.HabitCompletions
            .Where(hc => hc.HabitId == habitId && hc.UserId == userId)
            .OrderBy(hc => hc.CompletionDate)
            .ToListAsync();

        if (!completions.Any()) return (0, 0);

        // Calculate current streak
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentStreak = 0;
        var currentDate = today;

        while (true)
        {
            var hasCompletion = completions.Any(c => c.CompletionDate == currentDate);
            
            if (hasCompletion)
            {
                currentStreak++;
                currentDate = currentDate.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        // Calculate best streak
        var sortedCompletions = completions.OrderBy(c => c.CompletionDate).ToList();
        var bestStreak = 0;
        var currentBestStreak = 1;

        for (int i = 1; i < sortedCompletions.Count; i++)
        {
            var daysDifference = (sortedCompletions[i].CompletionDate.ToDateTime(TimeOnly.MinValue) - 
                                sortedCompletions[i - 1].CompletionDate.ToDateTime(TimeOnly.MinValue)).Days;

            if (daysDifference == 1)
            {
                currentBestStreak++;
            }
            else
            {
                bestStreak = Math.Max(bestStreak, currentBestStreak);
                currentBestStreak = 1;
            }
        }

        bestStreak = Math.Max(bestStreak, currentBestStreak);

        return (currentStreak, bestStreak);
    }
}
