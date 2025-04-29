using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using CP2496H07Group1.Models;
using CP2496H07Group1.Configs.Database;

namespace CP2496H07Group1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    public class ManageLoanOptionsController : Controller
    {
        private readonly AppDataContext _context;

        public ManageLoanOptionsController(AppDataContext context)
        {
            _context = context;
        }

        // GET: Admin/ManageLoanOptions
        // GET: Admin/ManageLoanOptions
        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            int pageSize = 5; // 5 dòng mỗi trang

            var query = _context.LoanOptions.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(lo =>
                    lo.LoanDate.ToString().Contains(searchString) ||
                    lo.InterestRate.ToString().Contains(searchString));
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var loanOptions = await query
                .OrderByDescending(lo => lo.Id) // bạn có thể đổi thành LoanDate nếu thích
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewData["CurrentFilter"] = searchString;

            return View(loanOptions);
        }


        // GET: Admin/ManageLoanOptions/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var loanOption = await _context.LoanOptions.FirstOrDefaultAsync(m => m.Id == id);
            if (loanOption == null) return NotFound();

            return View(loanOption);
        }

        // GET: Admin/ManageLoanOptions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/ManageLoanOptions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LoanDate,InterestRate")] LoanOption loanOption)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loanOption);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(loanOption);
        }

        // GET: Admin/ManageLoanOptions/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var loanOption = await _context.LoanOptions.FindAsync(id);
            if (loanOption == null) return NotFound();

            return View(loanOption);
        }

        // POST: Admin/ManageLoanOptions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,LoanDate,InterestRate")] LoanOption loanOption)
        {
            if (id != loanOption.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loanOption);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoanOptionExists(loanOption.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(loanOption);
        }

        // GET: Admin/ManageLoanOptions/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            var loanOption = await _context.LoanOptions.FirstOrDefaultAsync(m => m.Id == id);
            if (loanOption == null) return NotFound();

            return View(loanOption);
        }

        // POST: Admin/ManageLoanOptions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var loanOption = await _context.LoanOptions.FindAsync(id);
            if (loanOption != null)
            {
                _context.LoanOptions.Remove(loanOption);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool LoanOptionExists(long id)
        {
            return _context.LoanOptions.Any(e => e.Id == id);
        }
    }
}