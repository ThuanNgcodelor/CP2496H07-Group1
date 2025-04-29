using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CP2496H07Group1.Models;
using CP2496H07Group1.Configs.Database;
using Microsoft.AspNetCore.Authorization;

namespace CP2496H07Group1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    public class AccountDiscountsController : Controller
    {
        private readonly AppDataContext _context;

        public AccountDiscountsController(AppDataContext context)
        {
            _context = context;
        }

        // GET: Admin/AccountDiscounts
        public async Task<IActionResult> Index(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.AccountDiscounts
                .Include(ad => ad.Account)
                    .ThenInclude(a => a.User)
                .Include(ad => ad.DiscountCode)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(ad =>
                    ad.DiscountCode.DiscountCodes.Contains(searchString) ||
                    ad.Account.AccountNumber.Contains(searchString) ||
                    (ad.Account.User.FirstName + " " + ad.Account.User.LastName).Contains(searchString));
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var pagedResults = await query
                 .OrderByDescending(ad => ad.Id)
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize)
                 .ToListAsync();


            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Keyword = searchString;

            return View(pagedResults);
        }

        // GET: Admin/AccountDiscounts/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var accountDiscount = await _context.AccountDiscounts
                .Include(ad => ad.Account)
                    .ThenInclude(a => a.User)
                .Include(ad => ad.DiscountCode)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (accountDiscount == null) return NotFound();

            return View(accountDiscount);
        }
    }
}