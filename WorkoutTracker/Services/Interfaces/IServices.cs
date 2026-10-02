using WorkoutTracker.Models.DTOs;

namespace WorkoutTracker.Services.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto> CreateAsync(CreateCategoryRequest req);
    Task<(bool Ok, string? Error)> DeleteAsync(int id);
}

public interface IExerciseService
{
    Task<List<ExerciseDto>> GetAllAsync(int? categoryId = null);
    Task<ExerciseDto> CreateAsync(CreateExerciseRequest req);
    Task<(bool Ok, string? Error, int WorkoutCount)> DeleteAsync(int id);
}

public interface IWorkoutService
{
    Task<List<WorkoutDto>> GetByYearMonthAsync(int year, int? month = null);
    Task<WorkoutDto?> GetByIdAsync(int id);
    Task<(bool Ok, string? Error, WorkoutDto? Data)> CreateOrUpdateTodayAsync(CreateWorkoutRequest req);
    Task<(bool Ok, string? Error)> UpdateCategoriesAsync(int id, UpdateWorkoutRequest req);
    Task<(bool Ok, string? Error)> DeleteAsync(int id);
    Task<List<int>> GetAvailableYearsAsync();
    Task<(bool Ok, string? Error, WorkoutExerciseDto? Data)> AddSetAsync(int workoutId, AddSetRequest req);
    Task<(bool Ok, string? Error, WorkoutExerciseDto? Data)> UpdateSetAsync(int workoutId, int weId, UpdateSetRequest req);
    Task<(bool Ok, string? Error)> RemoveSetAsync(int workoutId, int weId);
}

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(int year);
}

public interface IProgressService
{
    Task<PrDataDto> GetPrDataAsync(int exerciseId, int year);
    Task<FrequencyDto> GetFrequencyAsync(int year);
    Task<CardioDto> GetCardioAsync(int year);
}
