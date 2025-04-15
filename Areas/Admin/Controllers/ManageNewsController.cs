using CP2496H07Group1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using CP2496H07Group1.Configs.Database;
using Microsoft.AspNetCore.Authorization;

namespace CP2496H07Group1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    public class ManageNewsController : Controller
    {
        private readonly AppDataContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private const int PageSize = 10;

        public ManageNewsController(AppDataContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: Admin/ManageNews
        public async Task<IActionResult> Index(string searchString, int? page)
        {
            var news = from n in _context.News
                      .Include(n => n.Category)
                       where n.IsConfirm == true
                       select n;

            if (!string.IsNullOrEmpty(searchString))
            {
                news = news.Where(n => n.Title.Contains(searchString));
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            var pagedNews = await news.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            ViewBag.TotalItems = await news.CountAsync();
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            return View(pagedNews);
        }

        // GET: Admin/ManageNews/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var news = await _context.News
                .Include(n => n.Category)
                .Include(c => c.Comments)
                .FirstOrDefaultAsync(m => m.Id == id);

            return news == null ? NotFound() : View(news);
        }

        // GET: Admin/ManageNews/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(News news, IFormFile imageFile)
        {
            try
            {
                // File upload handling
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploads = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                    var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(uploads, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    news.ImageUrl = "/uploads/" + fileName;
                }

                // Set default values
                news.CreatedAt = DateTime.Now;
                news.IsConfirm = true;

                // Save to database
                _context.News.Add(news);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log error and return view
                ModelState.AddModelError("", $"Lỗi khi tạo tin: {ex.Message}");
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
                return View(news);
            }
        }
        // GET: Admin/ManageNews/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", news.CategoryId);
            return View(news);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, News news, IFormFile imageFile)
        {
            if (id != news.Id) return NotFound();

            try
            {
                // Handle image upload if new file is provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploads = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(news.ImageUrl))
                    {
                        var oldPath = Path.Combine(_hostingEnvironment.WebRootPath, news.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    // Save new image
                    var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(uploads, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    news.ImageUrl = "/uploads/" + fileName;
                }

                // Update news
                _context.Update(news);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi khi cập nhật: {ex.Message}");
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", news.CategoryId);
                return View(news);
            }
        }
        // GET: Admin/ManageNews/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            var news = await _context.News
                .Include(n => n.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            return news == null ? NotFound() : View(news);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var news = await _context.News.FindAsync(id);
            if (news != null)
            {
                // Soft delete by setting IsConfirm to false
                news.IsConfirm = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Details/{id}/Comments")]
        public async Task<IActionResult> NewsComments(long id)
        {
            var news = await _context.News
                .Include(n => n.Comments)
                    .ThenInclude(c => c.User)
                .Include(n => n.Comments)
                    .ThenInclude(c => c.Admin)
                .Include(n => n.Comments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .Include(n => n.Comments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.Admin)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (news == null) return NotFound();

            return View(news);
        }

        private bool NewsExists(long id)
        {
            return _context.News.Any(e => e.Id == id);
        }
    }
}