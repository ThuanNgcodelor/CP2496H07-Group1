using CP2496H07Group1.Configs.Database;
using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Controllers
{
    public class FaqController : Controller
    {
        private readonly AppDataContext _context;

        public FaqController(AppDataContext context)
        {
            _context = context;
        }

        public IActionResult Index(int page = 1)
        {
            int pageSize = 5;
            var totalFaqs = _context.Fqas.Where(f => f.IsConfirm).Count();
            var totalPages = (int)Math.Ceiling((double)totalFaqs / pageSize);

            var faqs = _context.Fqas
                .Where(f => f.IsConfirm)
                .OrderBy(f => f.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(faqs);
        }

    }
}