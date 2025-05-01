using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Security.Claims;

namespace CP2496H07Group1.Controllers
{
    public class SavingsController : Controller
    {
        private readonly AppDataContext _context;

        public SavingsController(AppDataContext appDataContext)
        {
            _context = appDataContext;
        }

        // GET: /Savings/Index
        public async Task<IActionResult> Index(string keyword)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var query = _context.SavingCategories.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(c => c.TypeTk.Contains(keyword));
            }

            var savingCategories = await query.ToListAsync();
            return View(savingCategories);
        }

        // GET: /Savings/ViewSavings
        public async Task<IActionResult> ViewSavings()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            long user = long.Parse(userId);
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == user);
            if (account == null)
            {
                return View(new List<Savings>());
            }

            var savings = await _context.Savings
                .Where(s => s.AccountId == account.Id)
                .Include(s => s.SavingCategory)
                .ToListAsync();

            return View(savings);
        }

        // GET: /Savings/BuyNow?id={id}
        [HttpGet]
        public async Task<IActionResult> BuyNow(long id)
        {
            var category = await _context.SavingCategories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Savings category does not exist." });
            }

            return Json(new
            {
                success = true,
                id = category.Id,
                typeTk = category.TypeTk,
                money = category.Money,
                month = category.Month,
                description = category.Description
            });
        }

        // POST: /Savings/PurchaseSaving
        [HttpPost]
        public async Task<IActionResult> PurchaseSaving([FromBody] PurchaseSavingRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login to make transactions." });
            }

            long user = long.Parse(userId);
            var account = await _context.Accounts
                .Include(a => a.Vip)
                .FirstOrDefaultAsync(a => a.UserId == user && a.Pin == request.Pin);

            if (account == null)
            {
                return Json(new { success = false, message = "The PIN is invalid." });
            }

            var category = await _context.SavingCategories.FindAsync(request.SavingCategoryId);
            if (category == null)
            {
                return Json(new { success = false, message = "Savings category does not exist." });
            }

            if (account.Balance < category.Money)
            {
                return Json(new { success = false, message = "The balance is not enough to make transactions." });
            }

            // Áp dụng VIP (nếu có)
            decimal moneyBack = 0;
            bool isNoPick = false;
            if (1!= 1)
            {
                if (account.Vip.NoPick)
                {
                    isNoPick = true;
                }
                if (account.Vip.MoneyBack.HasValue)
                {
                    moneyBack = category.Money * (account.Vip.MoneyBack.Value / 100m);
                }
            }

            // Trừ tiền từ tài khoản
            account.Balance -= category.Money;
            if (!isNoPick)
            {
                //account.Balance -= 0.1m; // Phí giao dịch
            }

            // Cộng tiền hoàn nếu có
            //account.Balance += moneyBack;

            // Cộng điểm (1% số tiền tiết kiệm)
            decimal pointEarned = category.Money * 0.01m;
            account.Point = (account.Point ?? 0) + (long)pointEarned;

            // Tạo bản ghi Savings
            var savings = new Savings
            {
                AccountId = account.Id,
                DateStart = DateTime.Now,
                DateEnd = DateTime.Now.AddMonths(category.Month),
                TypeTk = category.TypeTk,
                Status = "Unedened",
                Pay = "Unedened",
                SavingCategoryId = category.Id,
                SavingCategory = category,
                Account = account
            };
            _context.Savings.Add(savings);

            // Tạo giao dịch
            var transaction = new Transaction
            {
                FromAccountId = account.Id,
                ToAccountId = null,
                Amount = category.Money,
                TransactionType = "Saving",
                TransactionDate = DateTime.Now,
                Description = $"Buy savings {category.TypeTk}",
                FromAccount = account,
                VipId = account.VipId
            };
            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Buy successful savings!", savingsId = savings.Id });
        }

        // GET: /Savings/WithdrawNow?id={id}
        [HttpGet]
        public async Task<IActionResult> WithdrawNow(long id)
        {
            var savings = await _context.Savings
                .Include(s => s.SavingCategory)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (savings == null)
            {
                return Json(new { success = false, message = "Savings do not exist." });
            }

            if (savings.DateEnd > DateTime.Now || savings.Pay != "Unedened")
            {
                return Json(new { success = false, message = "Savings are not due or withdrew." });
            }

            var withdrawalAmount = savings.SavingCategory.Money + (savings.SavingCategory.Month * savings.SavingCategory.Money * 0.03m);

            return Json(new
            {
                success = true,
                id = savings.Id,
                typeTk = savings.TypeTk,
                dateEnd = savings.DateEnd.ToString("dd/MM/yyyy"),
                withdrawalAmount = withdrawalAmount
            });
        }

        // POST: /Savings/WithdrawSaving
        [HttpPost]
        public async Task<IActionResult> WithdrawSaving([FromBody] WithdrawSavingRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Please login to make transactions." });
                }

                if (!long.TryParse(userId, out long user))
                {
                    return Json(new { success = false, message = "User ID is invalid." });
                }

                Console.WriteLine($"UserId: {user}, SavingsId: {request.SavingsId}, Pin: {request.Pin}");

                var account = await _context.Accounts
                    .Include(a => a.Vip)
                    .FirstOrDefaultAsync(a => a.UserId == user && a.Pin == request.Pin);

                if (account == null)
                {
                    return Json(new { success = false, message = "The PIN is invalid or the account does not exist." });
                }

                Console.WriteLine($"Account found: {account.Id}");

                var savings = await _context.Savings
                    .Include(s => s.SavingCategory)
                    .FirstOrDefaultAsync(s => s.Id == request.SavingsId);

                if (savings == null)
                {
                    return Json(new { success = false, message = "Savings do not exist." });
                }

                if (savings.SavingCategory == null)
                {
                    return Json(new { success = false, message = "Savings categories are not linked." });
                }

                Console.WriteLine($"Savings found: {savings.Id}, Category: {savings.SavingCategory.Id}");

                if (savings.DateEnd > DateTime.Now)
                {
                    return Json(new { success = false, message = "Savings are not due. " });
                }

                if (savings.Pay != "Unedened")
                {
                    return Json(new { success = false, message = "Savings have been withdrawn." });
                }

                var category = savings.SavingCategory;
                var withdrawalAmount = category.Money + (category.Month * category.Money * 0.03m);

                Console.WriteLine($"WithdrawalAmount: {withdrawalAmount}");

                // Áp dụng VIP (nếu có)
                decimal moneyBack = 0;
                bool isNoPick = false;
                if (account.Vip != null)
                {
                    if (account.Vip.NoPick)
                    {
                        isNoPick = true;
                    }
                    if (account.Vip.MoneyBack.HasValue)
                    {
                        moneyBack = withdrawalAmount * (account.Vip.MoneyBack.Value / 100m);
                    }
                }

                // Cộng tiền vào tài khoản
                account.Balance += withdrawalAmount;
                account.Balance += moneyBack;

                // Cộng điểm
                decimal pointEarned = withdrawalAmount * 0.01m;
                account.Point = (account.Point ?? 0) + (long)pointEarned;

                // Cập nhật trạng thái
                savings.Pay = "Withdrawn";
                savings.Status = "Completed";

                // Tạo giao dịch
                var transaction = new Transaction
                {
                    FromAccountId = account.Id,
                    ToAccountId = account.Id,
                    Amount = withdrawalAmount,
                    TransactionType = "WithdrawSaving",
                    TransactionDate = DateTime.Now,
                    Description = $"Savings withdrawal {savings.TypeTk} (principal + interest)",
                    FromAccount = null,
                    ToAccount = account,
                    VipId = account.VipId
                };
                _context.Transactions.Add(transaction);

                Console.WriteLine("Saving changes...");
                await _context.SaveChangesAsync();
                Console.WriteLine("Changes saved successfully");

                return Json(new { success = true, message = "Successful withdrawal!", transactionId = transaction.Id });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "No inner exception";
                Console.WriteLine($"Error in WithdrawSaving: {ex.Message}\nInner: {innerMessage}\n{ex.StackTrace}");
                return Json(new { success = false, message = $"Lỗi server: {ex.Message}. Inner: {innerMessage}" });
            }
        }

        public class PurchaseSavingRequest
        {
            public long SavingCategoryId { get; set; }
            public int Pin { get; set; }
        }

        public class WithdrawSavingRequest
        {
            public long SavingsId { get; set; }
            public int Pin { get; set; }
        }
    }
}