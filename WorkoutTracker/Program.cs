using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Data;
using WorkoutTracker.Models;
using WorkoutTracker.Repositories.Interfaces;
using WorkoutTracker.Repositories.Implementations;
using WorkoutTracker.Services.Interfaces;
using WorkoutTracker.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository Pattern — Scoped per request (Dependency Inversion Principle)
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
builder.Services.AddScoped<IWorkoutRepository,  WorkoutRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IProgressRepository, ProgressRepository>();

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IExerciseService, ExerciseService>();
builder.Services.AddScoped<IWorkoutService,  WorkoutService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IProgressService, ProgressService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

// ── Database initialisation ───────────────────────────────────────────────────
// EnsureCreated creates WorkoutTrackerDb.mdf in the project folder on first run.
// RunSeedIfNeeded handles both fresh installs and upgrades where categories
// exist but exercises were not yet seeded.
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.EnsureCreated();
        RunSeedIfNeeded(db, logger);
    }
    catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 208)
    {
        // DB file existed but had no tables — drop and recreate cleanly
        logger.LogWarning("Tables missing — dropping and recreating database...");
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        RunSeedIfNeeded(db, logger);
    }
}

app.Run();

// ── Seed helper ───────────────────────────────────────────────────────────────
static void RunSeedIfNeeded(ApplicationDbContext db, ILogger<Program> logger)
{
    bool noCats = !db.Categories.Any();
    bool noExes = !db.Exercises.Any();
    if (!noCats && !noExes) return;

    Category Get(string name)
    {
        var existing = db.Categories.FirstOrDefault(c => c.Name == name);
        if (existing != null) return existing;
        var created = new Category { Name = name, IsDefault = true };
        db.Categories.Add(created);
        return created;
    }

    if (noCats) logger.LogInformation("Seeding categories...");
    var chest       = Get("Chest");
    var shoulders   = Get("Shoulders");
    var back        = Get("Back");
    var arms        = Get("Arms");
    var core        = Get("Core");
    var legs        = Get("Legs");
    var fullBody    = Get("Full Body");
    var cardio      = Get("Cardio");
    var flexibility = Get("Flexibility");
    if (noCats) db.SaveChanges();

    if (noExes)
    {
        logger.LogInformation("Seeding starter exercises...");
        db.Exercises.AddRange(
            new Exercise { Name = "Bench Press",         Category = chest },
            new Exercise { Name = "Incline Bench Press", Category = chest },
            new Exercise { Name = "Push-ups",            Category = chest },
            new Exercise { Name = "Chest Fly",           Category = chest },
            new Exercise { Name = "Dips",                Category = chest },
            new Exercise { Name = "Overhead Press",      Category = shoulders },
            new Exercise { Name = "Lateral Raise",       Category = shoulders },
            new Exercise { Name = "Front Raise",         Category = shoulders },
            new Exercise { Name = "Arnold Press",        Category = shoulders },
            new Exercise { Name = "Face Pull",           Category = shoulders },
            new Exercise { Name = "Pull-ups",            Category = back },
            new Exercise { Name = "Deadlift",            Category = back },
            new Exercise { Name = "Bent Over Row",       Category = back },
            new Exercise { Name = "Lat Pulldown",        Category = back },
            new Exercise { Name = "Seated Cable Row",    Category = back },
            new Exercise { Name = "Bicep Curl",          Category = arms },
            new Exercise { Name = "Tricep Pushdown",     Category = arms },
            new Exercise { Name = "Hammer Curl",         Category = arms },
            new Exercise { Name = "Skull Crusher",       Category = arms },
            new Exercise { Name = "Chin-ups",            Category = arms },
            new Exercise { Name = "Plank",               Category = core },
            new Exercise { Name = "Crunches",            Category = core },
            new Exercise { Name = "Russian Twist",       Category = core },
            new Exercise { Name = "Leg Raise",           Category = core },
            new Exercise { Name = "Ab Wheel Rollout",    Category = core },
            new Exercise { Name = "Squat",               Category = legs },
            new Exercise { Name = "Leg Press",           Category = legs },
            new Exercise { Name = "Lunges",              Category = legs },
            new Exercise { Name = "Romanian Deadlift",   Category = legs },
            new Exercise { Name = "Calf Raise",          Category = legs },
            new Exercise { Name = "Burpees",             Category = fullBody },
            new Exercise { Name = "Clean and Press",     Category = fullBody },
            new Exercise { Name = "Kettlebell Swing",    Category = fullBody },
            new Exercise { Name = "Thruster",            Category = fullBody },
            new Exercise { Name = "Turkish Get-up",      Category = fullBody },
            new Exercise { Name = "Running",             Category = cardio },
            new Exercise { Name = "Cycling",             Category = cardio },
            new Exercise { Name = "Rowing Machine",      Category = cardio },
            new Exercise { Name = "Jump Rope",           Category = cardio },
            new Exercise { Name = "Stair Climber",       Category = cardio },
            new Exercise { Name = "Yoga Flow",           Category = flexibility },
            new Exercise { Name = "Hip Flexor Stretch",  Category = flexibility },
            new Exercise { Name = "Hamstring Stretch",   Category = flexibility },
            new Exercise { Name = "Foam Rolling",        Category = flexibility },
            new Exercise { Name = "Shoulder Stretch",    Category = flexibility }
        );
        db.SaveChanges();
        logger.LogInformation("Seed complete — 9 categories, 45 exercises.");
    }
}
