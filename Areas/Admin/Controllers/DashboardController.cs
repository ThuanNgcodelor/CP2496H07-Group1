using System.Security.Claims;
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CP2496H07Group1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize("Admin")]
public class DashboardController : Controller
{
    private readonly AppDataContext _context;

    public DashboardController(AppDataContext appDataContext)
    {
        _context = appDataContext;
    }

    public async Task<IActionResult> Index()
    {
        // Total money in all accounts (sum of Balance)
        var totalMoney = await _context.Accounts.SumAsync(a => a.Balance);

        // Total number of transactions
        var totalTransactions = await _context.Transactions.CountAsync();

        // Total money of transactions in the last 24 hours
        var todayStart = DateTime.Now.AddDays(-1);
        var todayTransactions = await _context.Transactions
            .Where(t => t.TransactionDate >= todayStart)
            .SumAsync(t => t.Amount);

        // Total number of users
        var totalUsers = await _context.Users.CountAsync();

        // Fetch top 5 transactions, ordered by TransactionDate (most recent first)
        var recentTransactions = await _context.Transactions
            .Include(t => t.FromAccount) // Include related data if needed
            .Include(t => t.ToAccount)
            .OrderByDescending(t => t.TransactionDate)
            .Take(5)
            .ToListAsync();

        // Create view model
        var dashboardViewModel = new DashboardViewModel
        {
            TotalMoney = totalMoney,
            TotalTransactions = totalTransactions,
            TodayTransactions = todayTransactions,
            TotalUsers = totalUsers,
            RecentTransactions = recentTransactions
        };
        
        var recentComments = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.News)
            .Include(c => c.Replies)
            .OrderByDescending(c => c.CreatedAt)
            .Take(4)
            .ToListAsync();

        ViewBag.RecentComments = recentComments;

        return View(dashboardViewModel);
    }
    
    
    public async Task<IActionResult> AllComments()
    {
        var allComments = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.News)
            .Include(c => c.Admin)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(allComments);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(long parentId, string replyContent)
    {
        if (string.IsNullOrWhiteSpace(replyContent) || replyContent.Length > 500)
        {
            TempData["Error"] = "Reply must be between 1500 characters.";
            return RedirectToAction("AllComments", "Dashboard");
        }
        
        var parentComment = await _context.Comments.FindAsync(parentId);
        if (parentComment == null) return NotFound();

        var adminId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); ;

        var reply = new Comment
        {
            Content = replyContent,
            IsAdminReply = true,
            ParentId = parentId,
            AdminId = adminId,
            NewsId = parentComment.NewsId,
            CreatedAt = DateTime.Now
        };

        _context.Comments.Add(reply);
        await _context.SaveChangesAsync();

        return RedirectToAction("AllComments");
    }
}

// ViewModel to pass data to the view
public class DashboardViewModel
{
    public decimal TotalMoney { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TodayTransactions { get; set; }
    public int TotalUsers { get; set; }
    public List<Transaction> RecentTransactions { get; set; } = new List<Transaction>(); 
}