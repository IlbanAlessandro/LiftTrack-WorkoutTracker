using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Models;

namespace WorkoutTracker.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<WorkoutCategory> WorkoutCategories => Set<WorkoutCategory>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(50).IsRequired();
            e.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Exercise>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Category)
             .WithMany(c => c.Exercises)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Workout>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Date).IsRequired();
        });

        modelBuilder.Entity<WorkoutCategory>(e =>
        {
            e.HasKey(wc => wc.Id);
            e.HasOne(wc => wc.Workout)
             .WithMany(w => w.WorkoutCategories)
             .HasForeignKey(wc => wc.WorkoutId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(wc => wc.Category)
             .WithMany(c => c.WorkoutCategories)
             .HasForeignKey(wc => wc.CategoryId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutExercise>(e =>
        {
            e.HasKey(we => we.Id);
            e.Property(we => we.Sets).HasDefaultValue(1);
            e.Property(we => we.WeightKg).HasColumnType("decimal(10,2)");
            e.Property(we => we.Duration).HasColumnType("decimal(10,2)");
            e.Property(we => we.DistanceKm).HasColumnType("decimal(10,2)");
            e.Property(we => we.DurationUnit).HasMaxLength(10);
            e.HasOne(we => we.Workout)
             .WithMany(w => w.WorkoutExercises)
             .HasForeignKey(we => we.WorkoutId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(we => we.Exercise)
             .WithMany(x => x.WorkoutExercises)
             .HasForeignKey(we => we.ExerciseId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
