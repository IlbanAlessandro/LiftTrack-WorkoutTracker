using WorkoutTracker.Models;
using WorkoutTracker.Models.DTOs;
using WorkoutTracker.Repositories.Interfaces;
using WorkoutTracker.Services.Interfaces;

namespace WorkoutTracker.Services.Implementations;

// ── CategoryService ────────────────────────────────────────────────────────────
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repo;
    public CategoryService(ICategoryRepository repo) => _repo = repo;

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var cats = await _repo.GetAllAsync();
        var result = new List<CategoryDto>();
        foreach (var c in cats)
            result.Add(new CategoryDto { Id = c.Id, Name = c.Name, IsDefault = c.IsDefault, ExerciseCount = await _repo.GetExerciseCountAsync(c.Id) });
        return result;
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest req)
    {
        var cat = new Category { Name = req.Name.Trim(), IsDefault = false };
        await _repo.AddAsync(cat);
        return new CategoryDto { Id = cat.Id, Name = cat.Name, IsDefault = false, ExerciseCount = 0 };
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(int id)
    {
        var cat = await _repo.GetByIdAsync(id);
        if (cat == null) return (false, "Category not found.");
        if (cat.IsDefault) return (false, "Predefined categories cannot be deleted.");
        if (await _repo.HasExercisesAsync(id)) return (false, "Cannot delete: exercises exist in this category.");
        await _repo.DeleteAsync(cat);
        return (true, null);
    }
}

// ── ExerciseService ────────────────────────────────────────────────────────────
public class ExerciseService : IExerciseService
{
    private readonly IExerciseRepository _repo;
    public ExerciseService(IExerciseRepository repo) => _repo = repo;

    public async Task<List<ExerciseDto>> GetAllAsync(int? categoryId = null)
    {
        var list = await _repo.GetAllAsync(categoryId);
        var result = new List<ExerciseDto>();
        foreach (var e in list)
            result.Add(new ExerciseDto { Id = e.Id, Name = e.Name, CategoryId = e.CategoryId, CategoryName = e.Category.Name, WorkoutCount = await _repo.GetWorkoutUsageCountAsync(e.Id) });
        return result;
    }

    public async Task<ExerciseDto> CreateAsync(CreateExerciseRequest req)
    {
        var ex = new Exercise { Name = req.Name.Trim(), CategoryId = req.CategoryId };
        await _repo.AddAsync(ex);
        var full = await _repo.GetByIdAsync(ex.Id);
        return new ExerciseDto { Id = ex.Id, Name = ex.Name, CategoryId = ex.CategoryId, CategoryName = full?.Category.Name ?? "", WorkoutCount = 0 };
    }

    public async Task<(bool Ok, string? Error, int WorkoutCount)> DeleteAsync(int id)
    {
        var ex = await _repo.GetByIdAsync(id);
        if (ex == null) return (false, "Exercise not found.", 0);
        var count = await _repo.GetWorkoutUsageCountAsync(id);
        if (count > 0) return (false, $"Used in {count} workout(s) — cannot delete.", count);
        await _repo.DeleteAsync(ex);
        return (true, null, 0);
    }
}

// ── WorkoutService ─────────────────────────────────────────────────────────────
public class WorkoutService : IWorkoutService
{
    private readonly IWorkoutRepository _repo;
    public WorkoutService(IWorkoutRepository repo) => _repo = repo;

    public async Task<List<WorkoutDto>> GetByYearMonthAsync(int year, int? month = null)
        => (await _repo.GetByYearMonthAsync(year, month)).Select(Map).ToList();

    public async Task<WorkoutDto?> GetByIdAsync(int id)
    {
        var w = await _repo.GetByIdAsync(id);
        return w == null ? null : Map(w);
    }

    public async Task<(bool Ok, string? Error, WorkoutDto? Data)> CreateOrUpdateTodayAsync(CreateWorkoutRequest req)
    {
        var today = DateTime.Today;
        var existing = await _repo.GetByDateAsync(today);
        if (existing != null)
        {
            await _repo.ReplaceCategoriesAsync(existing.Id, req.CategoryIds);
            var updated = await _repo.GetByIdAsync(existing.Id);
            return (true, null, Map(updated!));
        }
        var workout = new Workout { Date = today };
        await _repo.AddAsync(workout);
        await _repo.ReplaceCategoriesAsync(workout.Id, req.CategoryIds);
        var fresh = await _repo.GetByIdAsync(workout.Id);
        return (true, null, Map(fresh!));
    }

    public async Task<(bool Ok, string? Error)> UpdateCategoriesAsync(int id, UpdateWorkoutRequest req)
    {
        var w = await _repo.GetByIdAsync(id);
        if (w == null) return (false, "Workout not found.");
        if (w.Date.Date != DateTime.Today) return (false, "Only today's workout can be edited.");
        await _repo.ReplaceCategoriesAsync(id, req.CategoryIds);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(int id)
    {
        var w = await _repo.GetByIdAsync(id);
        if (w == null) return (false, "Workout not found.");
        if (w.Date.Date != DateTime.Today) return (false, "Only today's workout can be deleted.");
        await _repo.DeleteAsync(w);
        return (true, null);
    }

    public Task<List<int>> GetAvailableYearsAsync() => _repo.GetAvailableYearsAsync();

    public async Task<(bool Ok, string? Error, WorkoutExerciseDto? Data)> AddSetAsync(int workoutId, AddSetRequest req)
    {
        var workout = await _repo.GetByIdAsync(workoutId);
        if (workout == null) return (false, "Workout not found.", null);
        if (workout.Date.Date != DateTime.Today) return (false, "Can only modify today's workout.", null);

        decimal? weight = req.WeightKg == 0 ? null : req.WeightKg;
        int? reps = req.Reps == 0 ? null : req.Reps;
        decimal? dur = req.Duration == 0 ? null : req.Duration;
        string? unit = dur.HasValue ? (string.IsNullOrWhiteSpace(req.DurationUnit) ? "minutes" : req.DurationUnit) : null;
        decimal? dist = req.DistanceKm == 0 ? null : req.DistanceKm;

        var match = await _repo.FindMatchingSetAsync(workoutId, req.ExerciseId, reps, weight, dur, unit, dist);
        if (match != null)
        {
            match.Sets++;
            await _repo.UpdateSetAsync(match);
            var reloaded = await _repo.GetSetByIdAsync(match.Id);
            return (true, null, MapSet(reloaded!));
        }

        var newSet = new WorkoutExercise { WorkoutId = workoutId, ExerciseId = req.ExerciseId, Sets = 1, Reps = reps, WeightKg = weight, Duration = dur, DurationUnit = unit, DistanceKm = dist };
        await _repo.AddSetAsync(newSet);
        var saved = await _repo.GetSetByIdAsync(newSet.Id);
        return (true, null, MapSet(saved!));
    }

    public async Task<(bool Ok, string? Error, WorkoutExerciseDto? Data)> UpdateSetAsync(int workoutId, int weId, UpdateSetRequest req)
    {
        var we = await _repo.GetSetByIdAsync(weId);
        if (we == null || we.WorkoutId != workoutId) return (false, "Set not found.", null);
        decimal? dur = req.Duration == 0 ? null : req.Duration;
        we.Reps = req.Reps == 0 ? null : req.Reps;
        we.WeightKg = req.WeightKg == 0 ? null : req.WeightKg;
        we.Duration = dur;
        we.DurationUnit = dur.HasValue ? (string.IsNullOrWhiteSpace(req.DurationUnit) ? "minutes" : req.DurationUnit) : null;
        we.DistanceKm = req.DistanceKm == 0 ? null : req.DistanceKm;
        await _repo.UpdateSetAsync(we);
        return (true, null, MapSet(we));
    }

    public async Task<(bool Ok, string? Error)> RemoveSetAsync(int workoutId, int weId)
    {
        var we = await _repo.GetSetByIdAsync(weId);
        if (we == null || we.WorkoutId != workoutId) return (false, "Set not found.");
        if (we.Sets > 1) { we.Sets--; await _repo.UpdateSetAsync(we); }
        else await _repo.DeleteSetAsync(we);
        return (true, null);
    }

    private static WorkoutDto Map(Workout w) => new()
    {
        Id = w.Id, Date = w.Date,
        Categories = w.WorkoutCategories.Select(wc => new CategoryDto { Id = wc.Category.Id, Name = wc.Category.Name, IsDefault = wc.Category.IsDefault }).OrderBy(c => c.Name).ToList(),
        Exercises = w.WorkoutExercises.Select(MapSet).ToList()
    };

    private static WorkoutExerciseDto MapSet(WorkoutExercise we) => new()
    {
        Id = we.Id, ExerciseId = we.ExerciseId,
        ExerciseName = we.Exercise?.Name ?? "",
        CategoryName = we.Exercise?.Category?.Name ?? "",
        Sets = we.Sets, Reps = we.Reps, WeightKg = we.WeightKg,
        Duration = we.Duration, DurationUnit = we.DurationUnit, DistanceKm = we.DistanceKm
    };
}

// ── DashboardService ───────────────────────────────────────────────────────────
public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _repo;
    public DashboardService(IDashboardRepository repo) => _repo = repo;

    public async Task<DashboardDto> GetDashboardAsync(int year) => new()
    {
        Year = year,
        TotalWorkouts = await _repo.GetTotalWorkoutsAsync(year),
        WorkoutDates = await _repo.GetWorkoutDatesAsync(year),
        SetsByCategory = await _repo.GetSetsByCategoryAsync(year)
    };
}

// ── ProgressService ────────────────────────────────────────────────────────────
public class ProgressService : IProgressService
{
    private readonly IProgressRepository _repo;
    public ProgressService(IProgressRepository repo) => _repo = repo;

    public async Task<PrDataDto> GetPrDataAsync(int exerciseId, int year)
    {
        var sets = await _repo.GetExerciseSetsForYearAsync(exerciseId, year);
        if (!sets.Any()) return new PrDataDto();

        // Determine whether this exercise uses weight, so we can pick the right PR metric.
        // Priority: weight > distance > duration > reps only
        bool hasWeight = sets.Any(we => we.WeightKg.HasValue && we.WeightKg > 0);

        // Per-day: pick the single best set using weight-first ordering
        var byDate = sets
            .GroupBy(we => we.Workout.Date.Date)
            .Select(g =>
            {
                // Best set = heaviest weight; on tie, most reps wins
                WorkoutExercise best;
                double chartValue;
                if (hasWeight)
                {
                    best = g.Where(we => we.WeightKg.HasValue && we.WeightKg > 0)
                             .OrderByDescending(we => we.WeightKg)
                             .ThenByDescending(we => we.Reps ?? 0)
                             .FirstOrDefault()
                           ?? g.First();
                    chartValue = (double)(best.WeightKg ?? 0);
                }
                else
                {
                    best = g.OrderByDescending(FallbackValue).First();
                    chartValue = FallbackValue(best);
                }
                return new { Date = g.Key, Best = best, ChartValue = chartValue };
            })
            .OrderBy(x => x.Date)
            .ToList();

        var points  = byDate.Select(x => new PrPointDto { Date = x.Date, Value = x.ChartValue }).ToList();

        // Overall best = highest chart value; tiebreak by reps
        var overall = byDate
            .OrderByDescending(x => x.ChartValue)
            .ThenByDescending(x => x.Best.Reps ?? 0)
            .First();
        var current = byDate.Last();

        return new PrDataDto
        {
            Points = points,
            CurrentRecord = new PrRecordDto { Date = current.Date, Value = current.ChartValue, Display = FmtPr(current.Best, hasWeight) },
            BestRecord    = new PrRecordDto { Date = overall.Date, Value = overall.ChartValue, Display = FmtPr(overall.Best, hasWeight) }
        };
    }

    // Fallback PR value for cardio exercises (no weight)
    private static double FallbackValue(WorkoutExercise we)
    {
        if (we.DistanceKm.HasValue) return (double)we.DistanceKm;
        if (we.Duration.HasValue)
            return we.DurationUnit switch { "seconds" => (double)we.Duration / 60, "hours" => (double)we.Duration * 60, _ => (double)we.Duration };
        if (we.Reps.HasValue) return we.Reps.Value;
        return 0;
    }

    // Display string: "100 kg × 5 reps" for strength, or distance/duration for cardio
    private static string FmtPr(WorkoutExercise we, bool hasWeight)
    {
        if (hasWeight && we.WeightKg.HasValue && we.WeightKg > 0)
        {
            var s = $"{we.WeightKg} kg";
            if (we.Reps.HasValue && we.Reps > 0) s += $" × {we.Reps} reps";
            return s;
        }
        if (we.DistanceKm.HasValue && we.DistanceKm > 0) return $"{we.DistanceKm} km";
        if (we.Duration.HasValue && we.Duration > 0) return $"{we.Duration} {we.DurationUnit ?? "min"}";
        if (we.Reps.HasValue && we.Reps > 0) return $"{we.Reps} reps";
        return "—";
    }

    public async Task<FrequencyDto> GetFrequencyAsync(int year)
    {
        var workouts = await _repo.GetWorkoutsForYearAsync(year);
        var monthly = new int[12];
        foreach (var w in workouts) monthly[w.Date.Month - 1]++;
        return new FrequencyDto { TotalWorkouts = workouts.Count, AvgPerMonth = workouts.Count == 0 ? 0 : Math.Round(workouts.Count / 12.0, 1), MonthlyWorkouts = monthly.ToList() };
    }

    public async Task<CardioDto> GetCardioAsync(int year)
    {
        var sets = await _repo.GetCardioSetsForYearAsync(year);
        var dist = new decimal[12];
        var dur = new decimal[12];
        foreach (var we in sets)
        {
            int m = we.Workout.Date.Month - 1;
            if (we.DistanceKm.HasValue) dist[m] += we.DistanceKm.Value * we.Sets;
            if (we.Duration.HasValue)
            {
                decimal mins = we.DurationUnit switch { "seconds" => we.Duration.Value / 60, "hours" => we.Duration.Value * 60, _ => we.Duration.Value };
                dur[m] += mins * we.Sets;
            }
        }
        return new CardioDto { TotalDistanceKm = dist.Sum(), TotalDurationMinutes = dur.Sum(), MonthlyDistance = dist.ToList(), MonthlyDuration = dur.ToList() };
    }
}
