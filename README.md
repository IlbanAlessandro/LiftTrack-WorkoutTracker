# 🏋️‍♂️ Lift Track – Digital Workout & Fitness Tracker

**Lift Track** is a modern, web-based fitness tracker designed to replace chaotic paper logs and standard note-taking apps. Built with a **3-Tier Architecture** in **ASP.NET Core 8.0 MVC**, it functions as a fluid **Single Page Application (SPA)** with a dark-mode interface optimized for gym use.

The system is **dual-optimized**: it seamlessly tracks both **strength training** (weight & reps) and **cardio/endurance** sessions (duration & distance) within a unified, flexible database structure.

---

## ✨ Key Features

* **📊 Interactive Dashboard & Activity Calendar:** Automatically generates a monthly calendar highlighting active workout days, total annual volume, and muscle group effort distribution.
* **⚡ SPA Experience (Zero Page Reloads):** Powered by Vanilla JavaScript and asynchronous AJAX calls to a local REST API for instant UI updates.
* **🧠 Smart Set Merging Logic:** Optimizes database storage by detecting identical consecutive sets (same exercise, weight, and reps) and incrementing a `Sets` counter rather than creating duplicate rows.
* **🏆 Automated Personal Records (PR) & Analytics:** Background calculations instantly detect and flag new historical max weights (for strength) or distance/duration milestones (for cardio), updating progress charts in real time.
* **🔒 Historical Data Integrity ("Today-Only" Rule):** Past workouts automatically open in **Read-Only** mode, while active logging is restricted to the current date at both UI and API Service levels.
* **🗂️ Pre-configured Nomenclature:** Ships with 9 default categories and 45 exercises protected against accidental deletion (`IsDefault`), alongside full CRUD support for custom user exercises.

---

## 🛠️ Tech Stack & Architecture

### **Frontend (Presentation Layer - Tier 1)**
* **Razor Views, HTML5, CSS3** (Custom Dark Mode theme)
* **Bootstrap 5** (Responsive mobile-first layout & modals)
* **Vanilla JavaScript & AJAX** (Asynchronous REST API communication & regex input validation)

### **Backend (Business Logic Layer - Tier 2)**
* **C# / ASP.NET Core MVC (.NET 8.0)**
* **Service Layer Pattern:** Encapsulates all business rules (`WorkoutService`, `ProgressService`, `DashboardService`, `ExerciseService`, `CategoryService`), keeping controllers thin.
* **Dependency Injection (DI):** Decoupled architecture registered via interfaces for high testability.

### **Data Access & Database (Tier 3)**
* **SQL Server LocalDB & Entity Framework Core 8.0** (Code-First approach)
* **Repository Pattern:** Abstracts all EF Core queries behind interfaces.
* **Database Rules:** Configured with `Cascade Delete` (for workout sessions), `DeleteBehavior.Restrict` (preventing deletion of exercises/categories used in historical logs), and `Nullable` metrics for strength/cardio flexibility.

---

## 🚀 Getting Started (Plug & Play)

The application uses EF Core's `EnsureCreated()` method and automatic data seeding—**no manual SQL scripts or migrations are required**.

1. **Clone the repository:**
   ```bash
   git clone [https://github.com/IlbanAlessandro/LiftTrack-WorkoutTracker.git](https://github.com/IlbanAlessandro/LiftTrack-WorkoutTracker.git)
