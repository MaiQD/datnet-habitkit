using AutoMapper;
using HabitKitClone.Models;
using HabitKitClone.DTOs;

namespace HabitKitClone.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Habit mappings
        CreateMap<Habit, HabitDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.CurrentStreak, opt => opt.Ignore())
            .ForMember(dest => dest.LongestStreak, opt => opt.Ignore())
            .ForMember(dest => dest.CompletionRate, opt => opt.Ignore())
            .ForMember(dest => dest.TotalCompletions, opt => opt.Ignore());
            
        CreateMap<CreateHabitDto, Habit>();
        CreateMap<UpdateHabitDto, Habit>();
        
        // HabitCompletion mappings
        CreateMap<HabitCompletion, HabitCompletionDto>();
        CreateMap<CreateHabitCompletionDto, HabitCompletion>();
        CreateMap<UpdateHabitCompletionDto, HabitCompletion>();
        
        // Category mappings
        CreateMap<Category, CategoryDto>()
            .ForMember(dest => dest.HabitCount, opt => opt.Ignore());
        CreateMap<CreateCategoryDto, Category>();
        CreateMap<UpdateCategoryDto, Category>();
    }
}
