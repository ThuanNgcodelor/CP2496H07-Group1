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
            
            var latestNews = _context.News.Where(n => n.IsConfirm).OrderByDescending(n => n.CreatedAt).Take(6).ToList();

            ViewBag.LatestNews = latestNews;

            return View(sliders);
        }
    }
}