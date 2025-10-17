using HabitKitClone.DTOs;

namespace HabitKitClone.Services;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetUserCategoriesAsync(string userId);
    Task<IEnumerable<CategoryDto>> GetCategoriesWithHabitsAsync(string userId);
    Task<CategoryDto?> GetCategoryByIdAsync(int categoryId, string userId);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto, string userId);
    Task<CategoryDto?> UpdateCategoryAsync(int categoryId, UpdateCategoryDto updateCategoryDto, string userId);
    Task<bool> DeleteCategoryAsync(int categoryId, string userId);
}
