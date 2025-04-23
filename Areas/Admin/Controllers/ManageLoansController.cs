using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CP2496H07Group1.Models;
using CP2496H07Group1.Configs.Database;
using Microsoft.AspNetCore.Authorization;

namespace CP2496H07Group1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    public class LoansController : Controller
    {
        private readonly AppDataContext _context;

        public LoansController(AppDataContext context)
        {
            _context = context;
        }

        // GET: Admin/Loans
        public async Task<IActionResult> Index(string searchString)
        {
            var loans = _context.Loans
                .Include(l => l.User)
                .Include(l => l.Account)
                .Include(l => l.LoanOption)
                .Include(l => l.Vip)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                loans = loans.Where(l =>
                    (l.User.FirstName + " " + l.User.LastName).Contains(searchString) ||
                    (l.Account != null && l.Account.AccountNumber.Contains(searchString)) ||
                    l.Status.Contains(searchString) ||
                    l.AmountBorrowed.ToString().Contains(searchString) ||
                    l.LoanOption.InterestRate.ToString().Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await loans.ToListAsync());
        }

        // GET: Admin/Loans/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var loan = await _context.Loans
                .Include(l => l.User)
                .Include(l => l.Account)
                .Include(l => l.LoanOption)
                .Include(l => l.Vip)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null) return NotFound();

            return View(loan);
        }
    }
}
