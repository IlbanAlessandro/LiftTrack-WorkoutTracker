namespace WorkoutTracker.Models.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsDefault { get; set; }
    public int ExerciseCount { get; set; }
}
public class CreateCategoryRequest { public string Name { get; set; } = ""; }
public class ExerciseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int WorkoutCount { get; set; }
}
public class CreateExerciseRequest { public string Name { get; set; } = ""; public int CategoryId { get; set; } }
public class WorkoutDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
    public List<WorkoutExerciseDto> Exercises { get; set; } = new();
}
public class WorkoutExerciseDto
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public int Sets { get; set; }
    public int? Reps { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? Duration { get; set; }
    public string? DurationUnit { get; set; }
    public decimal? DistanceKm { get; set; }
}
public class CreateWorkoutRequest { public DateTime Date { get; set; } public List<int> CategoryIds { get; set; } = new(); }
public class UpdateWorkoutRequest { public List<int> CategoryIds { get; set; } = new(); }
public class AddSetRequest
{
    public int ExerciseId { get; set; }
    public int? Reps { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? Duration { get; set; }
    public string? DurationUnit { get; set; }
    public decimal? DistanceKm { get; set; }
}
public class UpdateSetRequest
{
    public int? Reps { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? Duration { get; set; }
    public string? DurationUnit { get; set; }
    public decimal? DistanceKm { get; set; }
}
public class DashboardDto
{
    public int Year { get; set; }
    public int TotalWorkouts { get; set; }
    public List<WorkoutDateDto> WorkoutDates { get; set; } = new();
    public List<CategorySetsDto> SetsByCategory { get; set; } = new();
}
public class WorkoutDateDto { public DateTime Date { get; set; } public int WorkoutId { get; set; } }
public class CategorySetsDto { public string CategoryName { get; set; } = ""; public int TotalSets { get; set; } }
public class PrDataDto
{
    public List<PrPointDto> Points { get; set; } = new();
    public PrRecordDto? CurrentRecord { get; set; }
    public PrRecordDto? BestRecord { get; set; }
}
public class PrPointDto { public DateTime Date { get; set; } public double Value { get; set; } }
public class PrRecordDto { public DateTime Date { get; set; } public double Value { get; set; } public string Display { get; set; } = ""; }
public class FrequencyDto
{
    public int TotalWorkouts { get; set; }
    public double AvgPerMonth { get; set; }
    public List<int> MonthlyWorkouts { get; set; } = new(new int[12]);
}
public class CardioDto
{
    public decimal TotalDistanceKm { get; set; }
    public decimal TotalDurationMinutes { get; set; }
    public List<decimal> MonthlyDistance { get; set; } = new(new decimal[12]);
    public List<decimal> MonthlyDuration { get; set; } = new(new decimal[12]);
}
