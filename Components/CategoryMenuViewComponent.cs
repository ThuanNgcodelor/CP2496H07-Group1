using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CP2496H07Group1.Models;
using CP2496H07Group1.Configs.Database;

namespace CP2496H07Group1.Components;

public class CategoryMenuViewComponent : ViewComponent
{
    private readonly AppDataContext _context;

    public CategoryMenuViewComponent(AppDataContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await _context.Categories
            .Where(c => c.IsConfirm)
            .ToListAsync();
        return View(categories);
    }
}