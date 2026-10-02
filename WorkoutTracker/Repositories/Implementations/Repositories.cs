using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Data;
using WorkoutTracker.Models;
using WorkoutTracker.Models.DTOs;
using WorkoutTracker.Repositories.Interfaces;

namespace WorkoutTracker.Repositories.Implementations;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _db;
    public CategoryRepository(ApplicationDbContext db) => _db = db;
    public Task<List<Category>> GetAllAsync() => _db.Categories.OrderBy(c => c.Name).ToListAsync();
    public Task<Category?> GetByIdAsync(int id) => _db.Categories.FindAsync(id).AsTask();
    public async Task<Category> AddAsync(Category c) { _db.Categories.Add(c); await _db.SaveChangesAsync(); return c; }
    public async Task DeleteAsync(Category c) { _db.Categories.Remove(c); await _db.SaveChangesAsync(); }
    public Task<int> GetExerciseCountAsync(int id) => _db.Exercises.CountAsync(e => e.CategoryId == id);
    public Task<bool> HasExercisesAsync(int id) => _db.Exercises.AnyAsync(e => e.CategoryId == id);
}

public class ExerciseRepository : IExerciseRepository
{
    private readonly ApplicationDbContext _db;
    public ExerciseRepository(ApplicationDbContext db) => _db = db;
    public Task<List<Exercise>> GetAllAsync(int? categoryId = null)
    {
        var q = _db.Exercises.Include(e => e.Category).AsQueryable();
        if (categoryId.HasValue) q = q.Where(e => e.CategoryId == categoryId.Value);
        return q.OrderBy(e => e.Name).ToListAsync();
    }
    public Task<Exercise?> GetByIdAsync(int id) => _db.Exercises.Include(e => e.Category).FirstOrDefaultAsync(e => e.Id == id);
    public async Task<Exercise> AddAsync(Exercise e) { _db.Exercises.Add(e); await _db.SaveChangesAsync(); return e; }
    public async Task DeleteAsync(Exercise e) { _db.Exercises.Remove(e); await _db.SaveChangesAsync(); }
    public Task<int> GetWorkoutUsageCountAsync(int exerciseId) =>
        _db.WorkoutExercises.Where(we => we.ExerciseId == exerciseId).Select(we => we.WorkoutId).Distinct().CountAsync();
}

public class WorkoutRepository : IWorkoutRepository
{
    private readonly ApplicationDbContext _db;
    public WorkoutRepository(ApplicationDbContext db) => _db = db;

    private IQueryable<Workout> WithIncludes() =>
        _db.Workouts
            .Include(w => w.WorkoutCategories).ThenInclude(wc => wc.Category)
            .Include(w => w.WorkoutExercises).ThenInclude(we => we.Exercise).ThenInclude(e => e.Category);

    public Task<List<Workout>> GetByYearMonthAsync(int year, int? month = null)
    {
        var q = WithIncludes().Where(w => w.Date.Year == year);
        if (month.HasValue) q = q.Where(w => w.Date.Month == month.Value);
        return q.OrderByDescending(w => w.Date).ToListAsync();
    }

    public Task<Workout?> GetByIdAsync(int id) => WithIncludes().FirstOrDefaultAsync(w => w.Id == id);

    public Task<Workout?> GetByDateAsync(DateTime date)
    {
        var d = date.Date;
        return WithIncludes().FirstOrDefaultAsync(w => w.Date.Date == d);
    }

    public async Task<Workout> AddAsync(Workout w) { _db.Workouts.Add(w); await _db.SaveChangesAsync(); return w; }
    public async Task DeleteAsync(Workout w) { _db.Workouts.Remove(w); await _db.SaveChangesAsync(); }

    // Change #1: only current year + years with data — no +1
    public async Task<List<int>> GetAvailableYearsAsync()
    {
        var cur = DateTime.Today.Year;
        var years = await _db.Workouts.Select(w => w.Date.Year).Distinct().ToListAsync();
        years.Add(cur); // always include current year even if no workouts yet
        return years.Distinct().OrderBy(y => y).ToList();
    }

    public async Task ReplaceCategoriesAsync(int workoutId, List<int> categoryIds)
    {
        var existing = await _db.WorkoutCategories.Where(wc => wc.WorkoutId == workoutId).ToListAsync();
        _db.WorkoutCategories.RemoveRange(existing);
        await _db.SaveChangesAsync();
        if (categoryIds.Count > 0)
        {
            _db.WorkoutCategories.AddRange(categoryIds.Select(cid => new WorkoutCategory { WorkoutId = workoutId, CategoryId = cid }));
            await _db.SaveChangesAsync();
        }
    }

    public async Task<WorkoutExercise?> FindMatchingSetAsync(int workoutId, int exerciseId,
        int? reps, decimal? weightKg, decimal? duration, string? durationUnit, decimal? distanceKm)
    {
        var candidates = await _db.WorkoutExercises
            .Where(we => we.WorkoutId == workoutId && we.ExerciseId == exerciseId).ToListAsync();
        foreach (var we in candidates)
        {
            if (reps == null ? we.Reps == null : we.Reps == reps)
            if (weightKg == null ? we.WeightKg == null : we.WeightKg == weightKg)
            if (duration == null ? we.Duration == null : we.Duration == duration)
            if (durationUnit == null ? we.DurationUnit == null : we.DurationUnit == durationUnit)
            if (distanceKm == null ? we.DistanceKm == null : we.DistanceKm == distanceKm)
                return we;
        }
        return null;
    }

    public Task<WorkoutExercise?> GetSetByIdAsync(int id) =>
        _db.WorkoutExercises.Include(we => we.Exercise).ThenInclude(e => e.Category).FirstOrDefaultAsync(we => we.Id == id);

    public async Task<WorkoutExercise> AddSetAsync(WorkoutExercise we) { _db.WorkoutExercises.Add(we); await _db.SaveChangesAsync(); return we; }
    public async Task UpdateSetAsync(WorkoutExercise we) { _db.WorkoutExercises.Update(we); await _db.SaveChangesAsync(); }
    public async Task DeleteSetAsync(WorkoutExercise we) { _db.WorkoutExercises.Remove(we); await _db.SaveChangesAsync(); }
}

public class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _db;
    public DashboardRepository(ApplicationDbContext db) => _db = db;
    public Task<int> GetTotalWorkoutsAsync(int year) => _db.Workouts.CountAsync(w => w.Date.Year == year);
    public Task<List<WorkoutDateDto>> GetWorkoutDatesAsync(int year) =>
        _db.Workouts.Where(w => w.Date.Year == year)
            .Select(w => new WorkoutDateDto { Date = w.Date, WorkoutId = w.Id }).ToListAsync();
    public Task<List<CategorySetsDto>> GetSetsByCategoryAsync(int year) =>
        _db.WorkoutExercises
            .Include(we => we.Workout)
            .Include(we => we.Exercise).ThenInclude(e => e.Category)
            .Where(we => we.Workout.Date.Year == year)
            .GroupBy(we => we.Exercise.Category.Name)
            .Select(g => new CategorySetsDto { CategoryName = g.Key, TotalSets = g.Sum(we => we.Sets) })
            .OrderByDescending(x => x.TotalSets).ToListAsync();
}

public class ProgressRepository : IProgressRepository
{
    private readonly ApplicationDbContext _db;
    public ProgressRepository(ApplicationDbContext db) => _db = db;
    public Task<List<WorkoutExercise>> GetExerciseSetsForYearAsync(int exerciseId, int year) =>
        _db.WorkoutExercises.Include(we => we.Workout)
            .Where(we => we.ExerciseId == exerciseId && we.Workout.Date.Year == year)
            .OrderBy(we => we.Workout.Date).ToListAsync();
    public Task<List<Workout>> GetWorkoutsForYearAsync(int year) =>
        _db.Workouts.Where(w => w.Date.Year == year).ToListAsync();
    public Task<List<WorkoutExercise>> GetCardioSetsForYearAsync(int year) =>
        _db.WorkoutExercises
            .Include(we => we.Workout)
            .Include(we => we.Exercise).ThenInclude(e => e.Category)
            .Where(we => we.Workout.Date.Year == year && we.Exercise.Category.Name == "Cardio")
            .ToListAsync();
}
