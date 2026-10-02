using WorkoutTracker.Models;
using WorkoutTracker.Models.DTOs;

namespace WorkoutTracker.Repositories.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<Category> AddAsync(Category category);
    Task DeleteAsync(Category category);
    Task<int> GetExerciseCountAsync(int categoryId);
    Task<bool> HasExercisesAsync(int categoryId);
}

public interface IExerciseRepository
{
    Task<List<Exercise>> GetAllAsync(int? categoryId = null);
    Task<Exercise?> GetByIdAsync(int id);
    Task<Exercise> AddAsync(Exercise exercise);
    Task DeleteAsync(Exercise exercise);
    Task<int> GetWorkoutUsageCountAsync(int exerciseId);
}

public interface IWorkoutRepository
{
    Task<List<Workout>> GetByYearMonthAsync(int year, int? month = null);
    Task<Workout?> GetByIdAsync(int id);
    Task<Workout?> GetByDateAsync(DateTime date);
    Task<Workout> AddAsync(Workout workout);
    Task DeleteAsync(Workout workout);
    Task<List<int>> GetAvailableYearsAsync();
    Task ReplaceCategoriesAsync(int workoutId, List<int> categoryIds);
    Task<WorkoutExercise?> FindMatchingSetAsync(int workoutId, int exerciseId, int? reps, decimal? weightKg, decimal? duration, string? durationUnit, decimal? distanceKm);
    Task<WorkoutExercise?> GetSetByIdAsync(int id);
    Task<WorkoutExercise> AddSetAsync(WorkoutExercise we);
    Task UpdateSetAsync(WorkoutExercise we);
    Task DeleteSetAsync(WorkoutExercise we);
}

public interface IDashboardRepository
{
    Task<int> GetTotalWorkoutsAsync(int year);
    Task<List<WorkoutDateDto>> GetWorkoutDatesAsync(int year);
    Task<List<CategorySetsDto>> GetSetsByCategoryAsync(int year);
}

public interface IProgressRepository
{
    Task<List<WorkoutExercise>> GetExerciseSetsForYearAsync(int exerciseId, int year);
    Task<List<Workout>> GetWorkoutsForYearAsync(int year);
    Task<List<WorkoutExercise>> GetCardioSetsForYearAsync(int year);
}
