using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize("Admin")]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}