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
        // GET: Admin/LoanPaymentSchedules
        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            int pageSize = 5; // 5 dòng mỗi trang

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

            int totalItems = await schedules.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var pagedSchedules = await schedules
                .OrderByDescending(s => s.PaymentDueDate) // bạn có thể thay đổi thứ tự nếu cần
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewData["CurrentFilter"] = searchString;

            return View(pagedSchedules);
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