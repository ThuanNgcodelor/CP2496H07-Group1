﻿using CP2496H07Group1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CP2496H07Group1.Configs.Database;
using Microsoft.AspNetCore.Authorization;

namespace CP2496H07Group1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    public class ManageCategoryNewsController : Controller
    {
        private readonly AppDataContext _context;

        public ManageCategoryNewsController(AppDataContext context)
        {
            _context = context;
        }

        // GET: Admin/ManageCategoryNews
        public async Task<IActionResult> Index(string searchString, int? page)
        {
            var categories = from c in _context.Categories
                             where c.IsConfirm == true
                             select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                categories = categories.Where(c => c.Name.Contains(searchString));
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            var pagedCategories = await categories
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.TotalItems = await categories.CountAsync();
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            return View(pagedCategories);
        }

        // GET: Admin/ManageCategoryNews/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/ManageCategoryNews/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,IsConfirm")] Category category)
        {
            if (ModelState.IsValid)
            {
               
                bool isNameDuplicate = await _context.Categories
                    .AnyAsync(c => c.Name == category.Name);
                if (isNameDuplicate)
                {
                    ModelState.AddModelError("Name", "Category name already exists in the system.");
                }

              
                bool isDescriptionDuplicate = await _context.Categories
                    .AnyAsync(c => c.Description == category.Description);
                if (isDescriptionDuplicate)
                {
                    ModelState.AddModelError("Description", "Description already exists in the system.");
                }

                if (!ModelState.IsValid)
                {
                    return View(category);
                }

                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }


        // GET: Admin/ManageCategoryNews/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Admin/ManageCategoryNews/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,Name,Description,IsConfirm")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra trùng Name (trừ chính nó)
                bool isNameDuplicate = await _context.Categories
                    .AnyAsync(c => c.Id != category.Id && c.Name == category.Name);
                if (isNameDuplicate)
                {
                    ModelState.AddModelError("Name", "Category name already exists in the system.");
                }

                // Kiểm tra trùng Description (trừ chính nó)
                bool isDescriptionDuplicate = await _context.Categories
                    .AnyAsync(c => c.Id != category.Id && c.Description == category.Description);
                if (isDescriptionDuplicate)
                {
                    ModelState.AddModelError("Description", "Description already exists in the system.");
                }

                if (!ModelState.IsValid)
                {
                    return View(category);
                }

                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // GET: Admin/ManageCategoryNews/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Admin/ManageCategoryNews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            // Kiểm tra có tin nào thuộc category này không
            bool hasNews = await _context.News.AnyAsync(n => n.CategoryId == id);
            if (hasNews)
            {
                ViewBag.ErrorMessage = "Cannot delete category because it contains news.";
                return View("Delete", category); // Trả lại View Delete với thông báo
            }
            _context.Categories.Remove(category); // Xóa luôn khỏi DB
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(long id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}