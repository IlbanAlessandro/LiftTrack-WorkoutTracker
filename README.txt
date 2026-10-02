================================================================================
  WORKOUT TRACKER — ASP.NET Core 8 MVC + SQL Server LocalDB
================================================================================

QUICK START — Visual Studio 2022 / 2026
────────────────────────────────────────
1. Unzip and open WorkoutTracker.sln
2. Press F5 (or Ctrl+F5)
3. The database is created AUTOMATICALLY on first run — no setup needed.

QUICK START — Command Line
────────────────────────────
1. cd WorkoutTracker
2. dotnet restore
3. dotnet run
4. Open http://localhost:5000

DATABASE
────────────────────────────
The database file (WorkoutTrackerDb.mdf) is created automatically in the
project folder (next to WorkoutTracker.csproj) on first run.

No migrations, no Update-Database, no shared LocalDB instance to manage.
Just run the app and it handles everything.

TO RESET / FRESH START
────────────────────────────
Run this in PowerShell (app must be stopped first):

  sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE IF EXISTS WorkoutTrackerApp"

Then restart the app — it recreates and reseeds everything automatically.

DATABASE LOCATION
────────────────────────────
Database name: WorkoutTrackerApp (managed by your LocalDB instance).
No .mdf file to manage — LocalDB handles it in its own storage.

ARCHITECTURE
────────────────────────────
  Controllers -> Services -> Repositories -> DbContext
  Repository Pattern + Service Layer + DI (SOLID principles)
  All data via REST API (JSON) to JS frontend — no page reloads

================================================================================
