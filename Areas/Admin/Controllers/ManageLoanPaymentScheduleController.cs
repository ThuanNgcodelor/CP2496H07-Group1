using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CP2496H07Group1.Models;
using CP2496H07Group1.Configs.Database;
using Microsoft.AspNetCore.Authorization;

namespace CP2496H07Group1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    public class LoanPaymentSchedulesController : Controller
    {
        private readonly AppDataContext _context;

        public LoanPaymentSchedulesController(AppDataContext context)
        {
            _context = context;
        }

        // GET: Admin/LoanPaymentSchedules
        public async Task<IActionResult> Index(string searchString)
        {
            var schedules = _context.LoanPaymentSchedules
                .Include(s => s.Loan)
                    .ThenInclude(l => l.User)
                .Include(s => s.Loan)
                    .ThenInclude(l => l.Account)
                .Include(s => s.Loan)
                    .ThenInclude(l => l.LoanOption)
                .Include(s => s.Loan)
                    .ThenInclude(l => l.Vip)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                schedules = schedules.Where(s =>
                    (s.Loan.User.FirstName + " " + s.Loan.User.LastName).Contains(searchString) ||
                    (s.Loan.Account != null && s.Loan.Account.AccountNumber.Contains(searchString)) ||
                    s.PaymentDueDate.ToString().Contains(searchString) ||
                    s.Paymentstatus.ToString().Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await schedules.ToListAsync());
        }

        // GET: Admin/LoanPaymentSchedules/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var schedule = await _context.LoanPaymentSchedules
                .Include(s => s.Loan)
                    .ThenInclude(l => l.User)
                .Include(s => s.Loan)
                    .ThenInclude(l => l.Account)
                .Include(s => s.Loan)
                    .ThenInclude(l => l.LoanOption)
                .Include(s => s.Loan)
                    .ThenInclude(l => l.Vip)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null) return NotFound();

            return View(schedule);
        }
    }
}
