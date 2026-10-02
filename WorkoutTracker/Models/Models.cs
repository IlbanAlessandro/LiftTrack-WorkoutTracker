namespace WorkoutTracker.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
    public ICollection<WorkoutCategory> WorkoutCategories { get; set; } = new List<WorkoutCategory>();
}

public class Exercise
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
}

public class Workout
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public ICollection<WorkoutCategory> WorkoutCategories { get; set; } = new List<WorkoutCategory>();
    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
}

public class WorkoutCategory
{
    public int Id { get; set; }
    public int WorkoutId { get; set; }
    public int CategoryId { get; set; }
    public Workout Workout { get; set; } = null!;
    public Category Category { get; set; } = null!;
}

/// <summary>
/// One row = one "set group". Sets column = how many identical sets.
/// Increment/decrement logic: if an identical row exists (same non-null values),
/// increment Sets; otherwise create new row with Sets=1.
/// </summary>
public class WorkoutExercise
{
    public int Id { get; set; }
    public int WorkoutId { get; set; }
    public int ExerciseId { get; set; }
    public int Sets { get; set; } = 1;
    public int? Reps { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? Duration { get; set; }
    public string? DurationUnit { get; set; }  // seconds | minutes | hours
    public decimal? DistanceKm { get; set; }
    public Workout Workout { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
}
