using AutoMapper;
using HabitKitClone.Data;
using HabitKitClone.DTOs;
using HabitKitClone.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitKitClone.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CategoryService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CategoryDto>> GetUserCategoriesAsync(string userId)
    {
        var categories = await _context.Categories
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesWithHabitsAsync(string userId)
    {
        var categories = await _context.Categories
            .Where(c => c.UserId == userId && c.IsActive)
            .Where(c => c.Habits.Any(h => h.UserId == userId && h.IsActive))
            .OrderBy(c => c.Name)
            .ToListAsync();

        var categoryDtos = new List<CategoryDto>();
        
        foreach (var category in categories)
        {
            var categoryDto = _mapper.Map<CategoryDto>(category);
            categoryDto.HabitCount = await _context.Habits
                .CountAsync(h => h.CategoryId == category.Id && h.UserId == userId && h.IsActive);
            categoryDtos.Add(categoryDto);
        }

        return categoryDtos;
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int categoryId, string userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

        if (category == null) return null;

        var categoryDto = _mapper.Map<CategoryDto>(category);
        categoryDto.HabitCount = await _context.Habits
            .CountAsync(h => h.CategoryId == category.Id && h.UserId == userId && h.IsActive);
        
        return categoryDto;
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto, string userId)
    {
        var category = _mapper.Map<Category>(createCategoryDto);
        category.UserId = userId;
        category.CreatedAt = DateTime.UtcNow;

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(int categoryId, UpdateCategoryDto updateCategoryDto, string userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

        if (category == null) return null;

        _mapper.Map(updateCategoryDto, category);

        await _context.SaveChangesAsync();

        var categoryDto = _mapper.Map<CategoryDto>(category);
        categoryDto.HabitCount = await _context.Habits
            .CountAsync(h => h.CategoryId == category.Id && h.UserId == userId && h.IsActive);
        
        return categoryDto;
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId, string userId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

        if (category == null) return false;

        // Check if category has habits
        var hasHabits = await _context.Habits
            .AnyAsync(h => h.CategoryId == categoryId && h.UserId == userId && h.IsActive);

        if (hasHabits)
        {
            // Soft delete - just mark as inactive
            category.IsActive = false;
        }
        else
        {
            // Hard delete if no habits
            _context.Categories.Remove(category);
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
