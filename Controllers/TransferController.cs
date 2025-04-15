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
            // Lấy ID người dùng đang đăng nhập
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Kiểm tra tài khoản người gửi dựa trên AccountType từ form
            var sender = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == model.AccountType);
            if (sender == null)
            {
                return Json(new { success = false, message = "Tài khoản người chuyển không tồn tại." });
            }

            // Kiểm tra tài khoản thuộc về người dùng đăng nhập
            if (sender.UserId != userId)
            {
                return Json(new { success = false, message = "Bạn không có quyền sử dụng tài khoản này." });
            }

            // Kiểm tra mã PIN
            if (sender.Pin != model.Pin)
            {
                return Json(new { success = false, message = "Mã PIN không đúng." });
            }

            // Tìm tài khoản người nhận
            var receiver = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == model.AccountNumber);
            if (receiver == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản người nhận." });
            }

            // Kiểm tra số dư
            if (sender.Balance < model.Monney)
            {
                return Json(new { success = false, message = "Số dư không đủ." });
            }

            // Cập nhật số dư
            sender.Balance -= model.Monney;
            receiver.Balance += model.Monney;

            // Cộng điểm cho người gửi (1% số tiền chuyển)
            decimal pointEarned = model.Monney * 0.01m;
            sender.Point = (sender.Point ?? 0) + (long)pointEarned;

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

            return Json(new { success = true, message = "Chuyển khoản thành công!", transactionId = transaction.Id });
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
                TempData["Error"] = "Không tìm thấy thông tin giao dịch.";
                return RedirectToAction("Index", "Home");
            }

            return View(transaction);
        }
    }
}