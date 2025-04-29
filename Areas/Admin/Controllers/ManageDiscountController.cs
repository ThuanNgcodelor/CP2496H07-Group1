using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CP2496H07Group1.Models;
using CP2496H07Group1.Configs.Database;
using Microsoft.AspNetCore.Authorization;

namespace CP2496H07Group1.Areas.Admin.Controllers
{

    [Area("Admin")]
    [Authorize("Admin")]
    public class DiscountCodeController : Controller
    {
        private readonly AppDataContext _context;
        private const int PageSize = 5;
        public DiscountCodeController(AppDataContext context)
        {
            _context = context;

        }

        // GET: DiscountCode
        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            var discounts = _context.DiscountCodes.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                discounts = discounts.Where(d =>
                    d.DiscountCodes.Contains(searchString) ||
                    d.Points.ToString().Contains(searchString) ||
                    d.Percent.ToString().Contains(searchString) ||
                    d.LongDate.ToString().Contains(searchString)
                );
            }

            discounts = discounts.OrderByDescending(d => d.Id); // Mặc định mới nhất lên trước

            int totalItems = await discounts.CountAsync();
            var pagedDiscounts = await discounts
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling(totalItems / (double)PageSize);

            return View(pagedDiscounts);
        }



        // GET: DiscountCode/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var discountCode = await _context.DiscountCodes.FirstOrDefaultAsync(m => m.Id == id);
            if (discountCode == null) return NotFound();

            return View(discountCode);
        }

        // GET: DiscountCode/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DiscountCode/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DiscountCodes,Points,Percent,LongDate")] DiscountCode discountCode)
        {
            if (ModelState.IsValid)
            {
                _context.Add(discountCode);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(discountCode);
        }

        // GET: DiscountCode/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var discountCode = await _context.DiscountCodes.FindAsync(id);
            if (discountCode == null) return NotFound();

            return View(discountCode);
        }

        // POST: DiscountCode/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,DiscountCodes,Points,Percent,LongDate")] DiscountCode discountCode)
        {
            if (id != discountCode.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(discountCode);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DiscountCodeExists(discountCode.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(discountCode);
        }

        // GET: DiscountCode/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            var discountCode = await _context.DiscountCodes.FirstOrDefaultAsync(m => m.Id == id);
            if (discountCode == null) return NotFound();

            return View(discountCode);
        }

        // POST: DiscountCode/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var discountCode = await _context.DiscountCodes.FindAsync(id);
            if (discountCode != null)
            {
                _context.DiscountCodes.Remove(discountCode);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DiscountCodeExists(long id)
        {
            return _context.DiscountCodes.Any(e => e.Id == id);
        }
    }
}