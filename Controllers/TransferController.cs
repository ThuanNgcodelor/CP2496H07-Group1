using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CP2496H07Group1.Controllers
{
    public class TransferController : Controller
    {
        private readonly AppDataContext _context;

        public TransferController(AppDataContext appDataContext)
        {
            _context = appDataContext;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            long user = long.Parse(userId);

            var accounts = await _context.Accounts
                .Where(a => a.UserId == user)
                .Select(a => new
                {
                    a.Id,
                    a.AccountNumber,
                    a.Balance
                })
                .ToListAsync();

            return View(accounts);
        }

        [HttpPost]
        public async Task<IActionResult> Transfer([FromBody] TransferViewModel model)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var sender = await _context.Accounts
                .Include(a => a.Vip) // Include Vip để truy xuất các thuộc tính vip
                .FirstOrDefaultAsync(a => a.Id == model.AccountType);

            if (sender == null)
                return Json(new { success = false, message = "The sender account does not exist.." });

            if (sender.UserId != userId)
                return Json(new { success = false, message = "You do not have permission to use this account." });

            if (sender.Pin != model.Pin)
                return Json(new { success = false, message = "Incorrect PIN." });

            var receiver = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == model.AccountNumber);
            if (receiver == null)
                return Json(new { success = false, message = "Recipient account not found." });

            if (sender.AccountNumber == receiver.AccountNumber)
                return Json(new { success = false, message = "Do not transfer money to yourself." });

            // Kiểm tra số dư
            if (sender.Balance < model.Monney)
                return Json(new { success = false, message = "Insufficient balance." });

            if (model.Monney < 0 )
                return Json(new { success = false, message = "No negative money transfer." });

            // ---------------------------
            // Áp dụng VIP
            decimal moneyBack = 0;
            bool isNopick = false;

            if (sender.Vip != null)
            {
                // Kiểm tra nếu Vip.Name là "Nopick" thì không trừ 0.1
                if (sender.Vip.NoPick)
                {
                    isNopick = true;
                }


                // Nếu có moneyback → tính hoàn tiền
                if (sender.Vip.MoneyBack.HasValue)
                {
                    moneyBack = model.Monney * (sender.Vip.MoneyBack.Value / 100m);
                }
            }

            // Trừ tiền (có trừ phí 0.1 nếu không phải Nopick)
            sender.Balance -= model.Monney;
            if (!isNopick)
            {
                sender.Balance -= 0.1m;
            }

            // Cộng tiền cho người nhận
            receiver.Balance += model.Monney;

            // Cộng điểm 1% tổng tiền chuyển
            decimal pointEarned = model.Monney * 0.01m;
            sender.Point = (sender.Point ?? 0) + (long)pointEarned;

            // Cộng lại tiền hoàn nếu có
            sender.Balance += moneyBack;

            // Tạo giao dịch
            var transaction = new Transaction
            {
                FromAccountId = sender.Id,
                ToAccountId = receiver.Id,
                Amount = model.Monney,
                TransactionType = "TRANSFER",
                Description = model.TransferContent,
                FromAccount = sender,
                ToAccount = receiver
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Transfer successful!", transactionId = transaction.Id });
        }


        [HttpGet]
        public async Task<IActionResult> CheckAccountNumber(string accountNumber)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
            if (account == null)
            {
                return Json(new { exists = false });
            }
            return Json(new { exists = true });
        }

        public async Task<IActionResult> Success(long id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                TempData["Error"] = "No transaction information found.";
                return RedirectToAction("Index", "Home");
            }

            return View(transaction);
        }
    }
}