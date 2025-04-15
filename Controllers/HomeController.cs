using CP2496H07Group1.Configs.Database;
using Microsoft.AspNetCore.Mvc;


namespace CP2496H07Group1.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDataContext _context;

        public HomeController(AppDataContext appDataContext)
        {
            _context = appDataContext;
        }

        public ActionResult Index()
        {
            // Lấy danh sách các slider có Status = true
            var sliders = _context.Sliders.Where(s => s.Status == true).ToList();
            return View(sliders);
        }
    }
}