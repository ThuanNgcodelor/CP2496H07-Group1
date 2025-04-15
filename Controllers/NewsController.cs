using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CP2496H07Group1.Controllers;

public class NewsController : Controller
{
    private readonly AppDataContext _context;

    public NewsController(AppDataContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(long? categoryId, int pageNumber = 1, int pageSize = 6)
    {
        var newsQuery = _context.News
            .Include(n => n.Category)
            .Where(n => n.IsConfirm) // Chỉ lấy tin tức có IsConfirm = true
            .OrderByDescending(n => n.CreatedAt);

        if (categoryId.HasValue)
        {
            newsQuery = (IOrderedQueryable<News>)newsQuery.Where(n => n.CategoryId == categoryId.Value);
        }

        var totalItems = await newsQuery.CountAsync();
        var newsItems = await newsQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.CurrentPage = pageNumber;
        ViewBag.CategoryId = categoryId;

        if (newsItems == null || !newsItems.Any())
        {
            return View(new List<News>());
        }

        return View(newsItems);
    }

    [HttpGet]
    public async Task<IActionResult> NewsDetail(long id)
    {
        var news = await _context.News
            .Include(n => n.Comments)
                .ThenInclude(c => c.User)
            .Include(n => n.Comments)
                .ThenInclude(c => c.Replies)
                    .ThenInclude(r => r.User)
            .Include(n => n.Category)
            .FirstOrDefaultAsync(n => n.Id == id);

        return View(news);
    }

}