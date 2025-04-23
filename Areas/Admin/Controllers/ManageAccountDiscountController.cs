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

        // GET: AccountDiscounts
        public async Task<IActionResult> Index(string searchString)
        {
            var accountDiscounts = _context.AccountDiscounts
                 .Include(ad => ad.Account)
                     .ThenInclude(a => a.User)
                 .Include(ad => ad.DiscountCode)
                 .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                accountDiscounts = accountDiscounts.Where(ad =>
                    ad.DiscountCode.DiscountCodes.Contains(searchString) ||
                    ad.Account.AccountNumber.Contains(searchString) ||
                    (ad.Account.User.FirstName + " " + ad.Account.User.LastName).Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await accountDiscounts.ToListAsync());
        }

        // GET: AccountDiscounts/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var accountDiscount = await _context.AccountDiscounts
                .Include(ad => ad.Account)
                    .ThenInclude(a => a.User)
                .Include(ad => ad.DiscountCode)
                .FirstOrDefaultAsync(m => m.Id == id);

            return View(accountDiscount);
        }
    }
}
