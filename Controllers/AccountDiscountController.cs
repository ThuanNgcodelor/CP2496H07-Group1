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

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 3; // Hiển thị 3 mã giảm giá mỗi trang

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["DiscountError"] = $"User not found (debug: userId={userId})";
                return RedirectToAction("Login", "Auth");
            }

            // Lấy tổng số mã giảm giá của người dùng
            var totalItems = await _context.AccountDiscounts
                .Where(ad => ad.Account.UserId == userId)
                .CountAsync();

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Lấy danh sách mã giảm giá cho trang hiện tại
            var discounts = await _context.AccountDiscounts
                .Where(ad => ad.Account.UserId == userId)
                .Include(ad => ad.Account)
                .Include(ad => ad.DiscountCode)
                .OrderByDescending(ad => ad.SDateTime)
                .Skip((page - 1) * pageSize) // Bỏ qua các phần tử của các trang trước đó
                .Take(pageSize) // Lấy đúng 3 phần tử
                .ToListAsync();

            // Xóa mã giảm giá hết hạn
            var expired = await _context.AccountDiscounts
                .Where(ad => ad.Account.UserId == userId && ad.STopDate <= DateTime.Now)
                .ToListAsync();

            if (expired.Any())
            {
                _context.AccountDiscounts.RemoveRange(expired);
                await _context.SaveChangesAsync();
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(discounts);
        }

    }
}