using CP2496H07Group1.Configs.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CP2496H07Group1.Controllers
{
    public class AccountDiscountsController : Controller
    {
        private readonly AppDataContext _context;


        public AccountDiscountsController(AppDataContext context)
        {
            _context = context;

        }

        public async Task<IActionResult> Index()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(value) || !long.TryParse(value, out long userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["DiscountError"] = $"User not found (debug: userId={userId})";
                return RedirectToAction("Login", "Auth");
            }

            var discounts = await _context.AccountDiscounts
                .Where(ad => ad.Account.UserId == userId)
                .Include(ad => ad.Account)
                .Include(ad => ad.DiscountCode)
                .OrderByDescending(ad => ad.SDateTime)
                .ToListAsync();
            // Xoá trước khi trả view
            var expired = await _context.AccountDiscounts
                .Where(ad => ad.Account.UserId == userId && ad.STopDate <= DateTime.Now)
                .ToListAsync();

            if (expired.Any())
            {
                _context.AccountDiscounts.RemoveRange(expired);
                await _context.SaveChangesAsync();
            }

            return View(discounts);
        }
    }
}