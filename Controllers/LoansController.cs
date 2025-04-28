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
using CP2496H07Group1.Configs.Email;
using CP2496H07Group1.Configs.Sms;
using Twilio.TwiML.Voice;

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
                .Include(l => l.PaymentSchedules)
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
               .Select(lo => new LoanOptionViewModel
               {
                   LoanOptionId = lo.Id,
                   DisplayText = $"{lo.LoanDate} months at {lo.InterestRate * 100}%",
                   LoanDate = lo.LoanDate,
                   InterestRate = (decimal)lo.InterestRate
               })
               .ToList();
            var accounts = (from a in _context.Accounts
                            where a.UserId == userId
                            join v in _context.Vips on a.VipId equals v.Id into accountVips
                            from av in accountVips.DefaultIfEmpty()
                            select new AccountViewModel
                            {
                                AccountId = a.Id,
                                AccountNumber = a.AccountNumber,
                                Balance = a.Balance,
                                DisplayText = $"Card: {a.AccountNumber} | Balance: ${a.Balance.ToString("F2")}",
                                TypeVip = av != null ? (int?)av.TypeVip : null
                            })
                            .ToList();


            var viewModel = new CreateLoanViewModel
            {
                LoanOptions = loanOptions,
                Accounts = accounts
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


            // tìm user theo iD
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["LoanError"] = $"Người dùng không tồn tại (debug: userId={userId})";
                return RedirectToAction("Index");
            }

            // ktra pass
            if (!VerifyPassword(pass, user.PasswordHash))
            {
                TempData["LoanError"] = "Password is incorrect.";
                return RedirectToAction("Index");
            }


            // lấy option
            var loanOption = await _context.LoanOptions.FindAsync(LoanOptionId);
            if (loanOption == null)
            {
                TempData["LoanError"] = "Invalid loan package.";
                return RedirectToAction("Index");
            }

            // Tạo khoản vay
            var interestRateDecimal = (decimal)loanOption.InterestRate;      
            var totalOweMoney = AmountBorrowed + (AmountBorrowed * interestRateDecimal);
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
                OweMoney = (int)totalOweMoney,
                User = user,
                LoanOption = loanOption,    
                Status = "Unpaid"
            };


            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();
           


            for (int i = 1; i <= loanOption.LoanDate; i++)
            {
                var dueDate = loan.StartDate.AddMonths(i);
                _context.LoanPaymentSchedules.Add(new LoanPaymentSchedule
                {
                    LoanId = loan.Id,
                    PaymentDueDate = dueDate
                });
            }


            await _context.SaveChangesAsync();
            await _authService.SendLoanConfirmationEmail( user, loan);



            if (!ModelState.IsValid)
            {
                TempData["LoanError"] = "Invalid information. Please check again.";
                return RedirectToAction("Index");
            }
            
            try
            {
          
                TempData["LoanSuccess"] = "Loan created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["LoanError"] = "An error occurred while processing the loan.";
                return RedirectToAction("Index");
            }
        }
        [HttpGet]
        public async Task<IActionResult> Pay(long id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
            {
                TempData["LoanError"] = "Invalid user.";
                return RedirectToAction("Index");
            }

            var loan = await _context.Loans
                .Include(l => l.PaymentSchedules)
                .Include(l => l.Account)
                .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

            if (loan == null)
            {
                TempData["LoanError"] = "Loan not found.";
                return RedirectToAction("Index");
            }

            var firstUnpaid = loan.PaymentSchedules
                .Where(ps => !ps.Paymentstatus)
                .OrderBy(ps => ps.PaymentDueDate)
                .FirstOrDefault();

            if (firstUnpaid == null)
            {
                TempData["LoanError"] = "All payments are already completed.";
                return RedirectToAction("Index");
            }

            var account = loan.Account;

            if (account == null)
            {
                TempData["LoanError"] = "Associated account not found.";
                return RedirectToAction("Index");
            }

            if (account.Balance < loan.MonthlyPayment)
            {
                TempData["LoanError"] = "Insufficient balance.";
                return RedirectToAction("Index");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Trừ tiền tài khoản
                account.Balance -= loan.MonthlyPayment;

                // Cập nhật khoản vay
                loan.OweMoney -= loan.MonthlyPayment;
                loan.OweMoney = Math.Max(0, loan.OweMoney);

                // Cập nhật lịch thanh toán
                firstUnpaid.Paymentstatus = true;
                firstUnpaid.IsReminderSent = true;

                // Tạo giao dịch mới
                var paymentTransaction = new Transaction
                {
                    FromAccountId = account.Id,
                    ToAccountId = null, // vì trả nợ cho hệ thống
                    Amount = loan.MonthlyPayment,
                    TransactionDate = DateTime.Now,
                    TransactionType = "LoanPayment",
                    Description = $"Loan payment for due date {firstUnpaid.PaymentDueDate:MM/dd/yyyy}",
                    FromAccount = account
                };

                _context.Transactions.Add(paymentTransaction);

                // Nếu đã trả hết nợ, xóa các lịch thanh toán và khoản vay
                if (loan.OweMoney == 0)
                {
                    _context.LoanPaymentSchedules.RemoveRange(loan.PaymentSchedules);
                    _context.Loans.Remove(loan);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["LoanSuccess"] = loan.OweMoney == 0
                    ? "You have fully paid off your loan. Loan closed!"
                    : "Monthly payment successful!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["LoanError"] = "An error occurred while processing the payment.";
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
