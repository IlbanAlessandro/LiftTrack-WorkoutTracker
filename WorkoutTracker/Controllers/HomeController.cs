using Microsoft.AspNetCore.Mvc;
namespace WorkoutTracker.Controllers;
public class HomeController : Controller
{
    public IActionResult Index() => View();
}
