using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Jwt;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Models;
using CP2496H07Group1.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using CP2496H07Group1.Dtos;

namespace CP2496H07Group1.Controllers
{
    public class LoansController : Controller

    {
        private readonly IAuthService _authService;
        private readonly JwtHandler _jwtHandler;
        private readonly RedisService _redisService;
        private readonly AppDataContext _context;

        public long LoanName { get; private set; }

        public LoansController(IAuthService authService, JwtHandler jwtHandler, RedisService redisService,
            AppDataContext appDataContext)
        {
            _authService = authService;
            _jwtHandler = jwtHandler;
            _redisService = redisService;
            _context = appDataContext;
        }

        public async Task<IActionResult> Index()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(value) || !long.TryParse(value, out long userId))
            {
                return RedirectToAction("Login", "Auth");
            }


            // Tìm user theo ID gốc
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["LoanError"] = $"Người dùng không tồn tại (debug: userId={userId})";
                return RedirectToAction("Login", "Auth");
            }

            // Lấy danh sách loan của user đó, include các bảng liên quan
            var loans = await _context.Loans
                .Where(l => l.UserId == user.Id)
                .Include(l => l.User)
                .Include(l => l.LoanOption)
                .Include(l => l.Vip)
                .ToListAsync();

            return View(loans); // truyền vào view
        }


        [HttpGet]
        public IActionResult Create()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(value) || !long.TryParse(value, out long userId))
            {
                // Redirect if the user ID is invalid or missing
                return RedirectToAction("Login", "Auth");
            }

            // Check if the user already has an existing loan
            var existingLoan = _context.Loans.FirstOrDefault(l => l.UserId == userId);
            if (existingLoan != null)
            {
                TempData["LoanError"] = "You already have an existing loan.";
                return RedirectToAction("Index", "Auth");
            }

            // Retrieve loan options
            var loanOptions = _context.LoanOptions
                .Select(lo => new LoanOption.LoanOptionViewModel
                {
                    LoanOptionId = lo.Id,
                    DisplayText = $"{lo.LoanDate} months at {lo.InterestRate * 100}%",
                    LoanDate = lo.LoanDate,
                    InterestRate = (decimal)lo.InterestRate
                })
                .ToList();

            ViewBag.LoanOptions = loanOptions;


            var viewModel = new CreateLoanViewModel
            {
                Accounts = (from a in _context.Accounts
                    where a.UserId == userId
                    join v in _context.Vips on a.VipId equals v.Id into accountVips
                    from av in accountVips.DefaultIfEmpty()
                    select new AccountViewModel
                    {
                        AccountId = a.Id,
                        AccountNumber = a.AccountNumber,
                        Balance = a.Balance,
                        DisplayText = $" Card: {a.AccountNumber} | Balance: ${a.Balance:N0}",
                        TypeVip = av != null ? (int?)av.TypeVip : null
                    }).ToList()
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmLoan(decimal AmountBorrowed, long LoanOptionId, string pass,
            int MonthlyPayment, DateTime StartDate, DateTime EndDate, long LoanName, long AccountId, long VipId)
        {
            // Lấy hashedId từ JWT
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(value) || !long.TryParse(value, out var userId))
            {
                TempData["LoanError"] = "Không xác định được người dùng.";
                return RedirectToAction("Index");
            }


            // Tìm user theo ID gốc
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["LoanError"] = $"Người dùng không tồn tại (debug: userId={userId})";
                return RedirectToAction("Index");
            }

            // Kiểm tra mật khẩu
            if (!VerifyPassword(pass, user.PasswordHash))
            {
                TempData["LoanError"] = "Password is incorrect.";
                return RedirectToAction("Index");
            }


            // Lấy gói vay
            var loanOption = await _context.LoanOptions.FindAsync(LoanOptionId);
            if (loanOption == null)
            {
                TempData["LoanError"] = "Invalid loan package.";
                return RedirectToAction("Index");
            }

            // Tạo khoản vay
            var loan = new Loans
            {
                UserId = user.Id,
                LoanOptionId = LoanOptionId,
                LoanName = LoanName,
                AccountId = AccountId,
                VipId = VipId,
                AmountBorrowed = AmountBorrowed,
                MonthlyPayment = MonthlyPayment,
                StartDate = StartDate,
                EndDate = EndDate,
                OweMoney = (int)AmountBorrowed,
                User = user,
                LoanOption = loanOption
            };

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            if (!ModelState.IsValid)
            {
                TempData["LoanError"] = "Invalid information. Please check again.";
                return RedirectToAction("Index");
            }

            try
            {
                // Giả sử bạn đã lưu khoản vay thành công
                TempData["LoanSuccess"] = "Loan created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["LoanError"] = "An error occurred while processing the loan.";
                return RedirectToAction("Index");
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string inputPassword, string storedHash)
        {
            return HashPassword(inputPassword) == storedHash;
        }
    }
}