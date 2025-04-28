using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        
        public IActionResult Leader()
        {
            return View();
        }

        public IActionResult Member1()
        {
            return View(); 
        }

        public IActionResult Member2()
        {
            return View();
        }

        public IActionResult Member3()
        {
            return View();
        }
    }
}
