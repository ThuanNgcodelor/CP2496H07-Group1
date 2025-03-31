using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}