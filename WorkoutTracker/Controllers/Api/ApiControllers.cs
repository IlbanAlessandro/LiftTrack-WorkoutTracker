using Microsoft.AspNetCore.Mvc;
using WorkoutTracker.Models.DTOs;
using WorkoutTracker.Services.Interfaces;

namespace WorkoutTracker.Controllers.Api;

[ApiController] [Route("api/dashboard")]
public class DashboardApiController : ControllerBase
{
    private readonly IDashboardService _svc;
    public DashboardApiController(IDashboardService svc) => _svc = svc;
    [HttpGet] public async Task<IActionResult> Get([FromQuery] int? year) =>
        Ok(await _svc.GetDashboardAsync(year ?? DateTime.Today.Year));
}

[ApiController] [Route("api/categories")]
public class CategoriesApiController : ControllerBase
{
    private readonly ICategoryService _svc;
    public CategoriesApiController(ICategoryService svc) => _svc = svc;
    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _svc.GetAllAsync());
    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "Name is required." });
        return Ok(await _svc.CreateAsync(req));
    }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id)
    {
        var (ok, err) = await _svc.DeleteAsync(id);
        return ok ? Ok(new { success = true }) : BadRequest(new { error = err });
    }
}

[ApiController] [Route("api/exercises")]
public class ExercisesApiController : ControllerBase
{
    private readonly IExerciseService _svc;
    public ExercisesApiController(IExerciseService svc) => _svc = svc;
    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] int? categoryId) => Ok(await _svc.GetAllAsync(categoryId));
    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateExerciseRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "Name is required." });
        return Ok(await _svc.CreateAsync(req));
    }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id)
    {
        var (ok, err, count) = await _svc.DeleteAsync(id);
        return ok ? Ok(new { success = true }) : BadRequest(new { error = err, workoutCount = count });
    }
}

[ApiController] [Route("api/workouts")]
public class WorkoutsApiController : ControllerBase
{
    private readonly IWorkoutService _svc;
    public WorkoutsApiController(IWorkoutService svc) => _svc = svc;
    [HttpGet("available-years")] public async Task<IActionResult> GetAvailableYears() => Ok(await _svc.GetAvailableYearsAsync());
    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] int? year, [FromQuery] int? month) =>
        Ok(await _svc.GetByYearMonthAsync(year ?? DateTime.Today.Year, month));
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id)
    {
        var dto = await _svc.GetByIdAsync(id);
        return dto == null ? NotFound() : Ok(dto);
    }
    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateWorkoutRequest req)
    {
        var (ok, err, data) = await _svc.CreateOrUpdateTodayAsync(req);
        return ok ? Ok(data) : BadRequest(new { error = err });
    }
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkoutRequest req)
    {
        var (ok, err) = await _svc.UpdateCategoriesAsync(id, req);
        return ok ? Ok(new { success = true }) : BadRequest(new { error = err });
    }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id)
    {
        var (ok, err) = await _svc.DeleteAsync(id);
        return ok ? Ok(new { success = true }) : BadRequest(new { error = err });
    }
    [HttpPost("{id}/sets")] public async Task<IActionResult> AddSet(int id, [FromBody] AddSetRequest req)
    {
        var (ok, err, data) = await _svc.AddSetAsync(id, req);
        return ok ? Ok(data) : BadRequest(new { error = err });
    }
    [HttpPut("{workoutId}/sets/{weId}")] public async Task<IActionResult> UpdateSet(int workoutId, int weId, [FromBody] UpdateSetRequest req)
    {
        var (ok, err, data) = await _svc.UpdateSetAsync(workoutId, weId, req);
        return ok ? Ok(data) : BadRequest(new { error = err });
    }
    [HttpDelete("{workoutId}/sets/{weId}")] public async Task<IActionResult> RemoveSet(int workoutId, int weId)
    {
        var (ok, err) = await _svc.RemoveSetAsync(workoutId, weId);
        return ok ? Ok(new { success = true }) : BadRequest(new { error = err });
    }
}

[ApiController] [Route("api/progress")]
public class ProgressApiController : ControllerBase
{
    private readonly IProgressService _svc;
    public ProgressApiController(IProgressService svc) => _svc = svc;
    [HttpGet("pr")] public async Task<IActionResult> GetPr([FromQuery] int exerciseId, [FromQuery] int? year) =>
        Ok(await _svc.GetPrDataAsync(exerciseId, year ?? DateTime.Today.Year));
    [HttpGet("frequency")] public async Task<IActionResult> GetFrequency([FromQuery] int? year) =>
        Ok(await _svc.GetFrequencyAsync(year ?? DateTime.Today.Year));
    [HttpGet("cardio")] public async Task<IActionResult> GetCardio([FromQuery] int? year) =>
        Ok(await _svc.GetCardioAsync(year ?? DateTime.Today.Year));
}
