using CP2496H07Group1.Models;
using CP2496H07Group1.Configs.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace CP2496H07Group1.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    public class TransactionsController : Controller
    {
        private readonly AppDataContext _context;

        public TransactionsController(AppDataContext appDataContext)
        {
            _context = appDataContext;
        }

        public async Task<IActionResult> Index(string keyword, string transactionType, string timeRange, int page = 1)
        {
            int pageSize = 10; // Số giao dịch mỗi trang
            var query = _context.Transactions
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .Include(t => t.DiscountCode)
                .AsQueryable();

            // Tìm kiếm theo từ khóa (Mô tả hoặc ID)
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(t =>
                    (t.Description != null && t.Description.Contains(keyword)) ||
                    t.FromAccountId.ToString().Contains(keyword) ||
                    (t.ToAccountId != null && t.ToAccountId.ToString().Contains(keyword)) ||
                    (t.DiscountCodeId != null && t.DiscountCodeId.ToString().Contains(keyword)));
            }

            // Lọc theo Loại Giao Dịch
            if (!string.IsNullOrEmpty(transactionType))
            {
                query = query.Where(t => t.TransactionType == transactionType);
            }

            // Lọc theo khoảng thời gian
            if (!string.IsNullOrEmpty(timeRange))
            {
                DateTime startDate = DateTime.Now;
                switch (timeRange.ToLower())
                {
                    case "1day":
                        startDate = DateTime.Now.AddDays(-1);
                        break;
                    case "3days":
                        startDate = DateTime.Now.AddDays(-3);
                        break;
                    case "7days":
                        startDate = DateTime.Now.AddDays(-7);
                        break;
                    case "1week":
                        startDate = DateTime.Now.AddDays(-7);
                        break;
                    case "2weeks":
                        startDate = DateTime.Now.AddDays(-14);
                        break;
                    case "1month":
                        startDate = DateTime.Now.AddMonths(-1);
                        break;
                    case "3months":
                        startDate = DateTime.Now.AddMonths(-3);
                        break;
                    case "6months":
                        startDate = DateTime.Now.AddMonths(-6);
                        break;
                    case "1year":
                        startDate = DateTime.Now.AddYears(-1);
                        break;
                }
                query = query.Where(t => t.TransactionDate >= startDate);
            }

            // Lấy danh sách Loại Giao Dịch duy nhất cho dropdown
            var transactionTypes = await _context.Transactions
                .Select(t => t.TransactionType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
            ViewBag.TransactionTypes = transactionTypes.Select(t => new SelectListItem
            {
                Value = t,
                Text = t
            });

            // Phân trang
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages)); // Đảm bảo số trang hợp lệ

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate) // Sắp xếp mới nhất trước
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Truyền dữ liệu sang view
            ViewBag.Keyword = keyword;
            ViewBag.TransactionType = transactionType;
            ViewBag.TimeRange = timeRange;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            // Chỉ định rõ đường dẫn View
            return View("~/Areas/Admin/Views/HistoryTransaction/Index.cshtml", transactions);
        }
    }
}