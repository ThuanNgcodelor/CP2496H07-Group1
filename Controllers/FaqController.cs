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

        // Hiển thị danh sách FAQ
        public IActionResult Index()
        {
            var faqs = _context.Fqas
                .Where(f => f.IsConfirm) // Chỉ lấy FAQ đã được xác nhận
                .OrderBy(f => f.Id)
                .ToList();

            return View(faqs);
        }
    }
}