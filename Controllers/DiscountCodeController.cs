using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Jwt;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Dtos;
using CP2496H07Group1.Models;
using CP2496H07Group1.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CP2496H07Group1.Controllers
{
    public class DiscountCodeController : Controller
    {
        private readonly IAuthService _authService;
        private readonly JwtHandler _jwtHandler;
        private readonly RedisService _redisService;
        private readonly AppDataContext _context;

        public DiscountCodeController(
            IAuthService authService,
            JwtHandler jwtHandler,
            RedisService redisService,
            AppDataContext appDataContext)
        {
            _authService = authService;
            _jwtHandler = jwtHandler;
            _redisService = redisService;
            _context = appDataContext;
        }

        // GET: Hiển thị danh sách mã giảm giá
        public IActionResult Index(int page = 1)
        {
            int pageSize = 3; // 3 mã giảm giá mỗi trang
            var discountsQuery = _context.DiscountCodes.AsQueryable();

            var totalItems = discountsQuery.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var discounts = discountsQuery
                .OrderByDescending(d => d.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var accounts = _context.Accounts
                .Where(a => a.UserId == userId)
                .Select(a => new AccountViewModel
                {
                    AccountId = a.Id,
                    AccountNumber = a.AccountNumber,
                    Point = a.Point,
                    DisplayText = $"Account {a.AccountNumber} - Points: {a.Point}"
                })
                .ToList();

            ViewBag.Accounts = accounts;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(discounts);
        }



        // POST: Đổi mã giảm giá
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Redeem(long id, long accountId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                TempData["DiscountError"] = "User not identified.";
                return RedirectToAction("Index");
            }

            var account = _context.Accounts
                .Include(a => a.AccountDiscounts)
                .FirstOrDefault(a => a.Id == accountId && a.UserId == userId);

            if (account == null)
            {
                TempData["DiscountError"] = "Account not found.";
                return RedirectToAction("Index");
            }

            var discount = _context.DiscountCodes.FirstOrDefault(d => d.Id == id);
            if (discount == null)
            {
                TempData["DiscountError"] = "Coupon code does not exist.";
                return RedirectToAction("Index");
            }

            if (account.Point < discount.Points)
            {
                TempData["DiscountError"] = "Not enough points to redeem this coupon.";
                return RedirectToAction("Index");
            }

            account.Point -= discount.Points;
            var endDate = DateTime.Now.AddMonths((int)discount.LongDate);

            _context.AccountDiscounts.Add(new AccountDiscounts
            {
                AccountId = account.Id,
                DiscountId = discount.Id,
                Status = 1,
                STopDate = endDate,
                SDateTime = DateTime.Now,
                DiscountCode = discount,
                Account = account
            });

            _context.SaveChanges();

            TempData["DiscountSuccess"] = "Discount code redeemed successfully!";
            return RedirectToAction("Index");
        }

    }
}