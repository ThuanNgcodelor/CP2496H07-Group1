using CP2496H07Group1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList;
using X.PagedList.Mvc.Core;
using CP2496H07Group1.Configs.Database;
using Microsoft.AspNetCore.Authorization;

namespace CP2496H07Group1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    public class ManageFaqController : Controller
    {
        private readonly AppDataContext _context;

        public ManageFaqController(AppDataContext context)
        {
            _context = context;
        }

        // GET: Admin/ManageFaq
        public async Task<IActionResult> Index(string searchString, int? page)
        {
            var faqs = from f in _context.Fqas
                       where f.IsConfirm == true
                       select f;

            if (!string.IsNullOrEmpty(searchString))
            {
                faqs = faqs.Where(f => f.Question.Contains(searchString));
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            var pagedFaqs = await faqs
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.TotalItems = await faqs.CountAsync();
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            return View(pagedFaqs);
        }

        // GET: Admin/ManageFaq/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/ManageFaq/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Question,Answer,IsConfirm")] Faq faq)
        {
            if (ModelState.IsValid)
            {
                _context.Add(faq);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(faq);
        }

        // GET: Admin/ManageFaq/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var faq = await _context.Fqas.FindAsync(id);
            if (faq == null)
            {
                return NotFound();
            }
            return View(faq);
        }

        // POST: Admin/ManageFaq/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,Question,Answer,IsConfirm")] Faq faq)
        {
            if (id != faq.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(faq);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FaqExists(faq.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(faq);
        }

        // GET: Admin/ManageFaq/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var faq = await _context.Fqas
                .FirstOrDefaultAsync(m => m.Id == id);
            if (faq == null)
            {
                return NotFound();
            }

            return View(faq);
        }

        // POST: Admin/ManageFaq/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var faq = await _context.Fqas.FindAsync(id);
            if (faq != null)
            {
                faq.IsConfirm = false;
                _context.Fqas.Update(faq);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FaqExists(long id)
        {
            return _context.Fqas.Any(e => e.Id == id);
        }
    }
}