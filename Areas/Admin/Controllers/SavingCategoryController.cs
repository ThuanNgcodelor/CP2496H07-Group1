using Microsoft.AspNetCore.Mvc;
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace CP2496H07Group1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    public class SavingCategoryController : Controller
    {
        private readonly AppDataContext _context;
        private readonly ILogger<SavingCategoryController> _logger;

        public SavingCategoryController(AppDataContext appDataContext, ILogger<SavingCategoryController> logger)
        {
            _context = appDataContext;
            _logger = logger;
        }

        // GET: Admin/SavingCategory
        public async Task<IActionResult> Index(string keyword = "", int page = 1)
        {
            int pageSize = 5;
            var categories = _context.SavingCategories.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                categories = categories.Where(c => c.TypeTk.Contains(keyword) || c.Description.Contains(keyword));
            }

            var totalItems = await categories.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            categories = categories.Skip((page - 1) * pageSize).Take(pageSize);

            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(await categories.ToListAsync());
        }

        // GET: Admin/SavingCategory/Add
        public IActionResult Add()
        {
            return View();
        }

        // POST: Admin/SavingCategory/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(SavingCategory category)
        {
            if (category.Month <= 0)
            {
                ModelState.AddModelError("Month", "Month must be greater than 0.");
            }
            if (category.Month > 120)
            {
                ModelState.AddModelError("Month", "Month cannot exceed 120.");
            }

            if (ModelState.IsValid)
            {
                category.Date = DateTime.Now; // Gán Date từ ứng dụng
                _context.SavingCategories.Add(category);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Thêm danh mục tiết kiệm thành công: {TypeTk}", category.TypeTk);
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("ModelState Error: {Error}", error.ErrorMessage);
            }

            return View(category);
        }

        // GET: Admin/SavingCategory/Update/5
        public async Task<IActionResult> Update(long id)
        {
            var category = await _context.SavingCategories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Không tìm thấy danh mục tiết kiệm với ID: {Id}", id);
                return NotFound();
            }
            return View(category);
        }

        // POST: Admin/SavingCategory/Update/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(long id, SavingCategory category)
        {
            if (id != category.Id)
            {
                _logger.LogWarning("ID không khớp: {Id} != {CategoryId}", id, category.Id);
                return BadRequest();
            }

            var existingCategory = await _context.SavingCategories.FindAsync(id);
            if (existingCategory == null)
            {
                _logger.LogWarning("Không tìm thấy danh mục tiết kiệm với ID: {Id}", id);
                return NotFound();
            }

            if (category.Month <= 0)
            {
                ModelState.AddModelError("Month", "Month must be greater than 0.");
            }
            if (category.Month > 120)
            {
                ModelState.AddModelError("Month", "Month cannot exceed 120.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingCategory.TypeTk = category.TypeTk;
                    existingCategory.Money = category.Money;
                    existingCategory.Description = category.Description;
                    existingCategory.Month = category.Month;
                    // Date giữ nguyên hoặc cập nhật nếu cần: existingCategory.Date = DateTime.Now;

                    _context.Update(existingCategory);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cập nhật danh mục tiết kiệm thành công: {TypeTk}", category.TypeTk);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi cập nhật danh mục tiết kiệm: {TypeTk}", category.TypeTk);
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật. Vui lòng thử lại.");
                }
            }

            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("ModelState Error: {Error}", error.ErrorMessage);
            }

            return View(category);
        }

        // POST: Admin/SavingCategory/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var category = await _context.SavingCategories.FindAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Không tìm thấy danh mục tiết kiệm với ID: {Id}", id);
                    return NotFound();
                }

                _context.SavingCategories.Remove(category);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã xóa danh mục tiết kiệm với ID: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa danh mục tiết kiệm với ID: {Id}", id);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi xóa. Vui lòng thử lại.");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}