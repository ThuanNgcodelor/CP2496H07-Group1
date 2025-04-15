using Microsoft.AspNetCore.Mvc;
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CP2496H07Group1.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VipController : Controller
    {
        private readonly AppDataContext _context;
        private readonly ILogger<VipController> _logger;

        public VipController(AppDataContext appDataContext, ILogger<VipController> logger)
        {
            _context = appDataContext;
            _logger = logger;
        }

        // GET: Admin/Vip
        public async Task<IActionResult> Index(string keyword = "", int page = 1)
        {
            int pageSize = 5;
            var vips = _context.Vips.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                vips = vips.Where(v => v.TypeVip.ToString().Contains(keyword));
            }

            var totalItems = await vips.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            vips = vips.Skip((page - 1) * pageSize).Take(pageSize);

            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(await vips.ToListAsync());
        }

        // GET: Admin/Vip/Add
        public IActionResult Add()
        {
            return View();
        }

        // POST: Admin/Vip/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Vip vip)
        {
            if (ModelState.IsValid)
            {
                _context.Vips.Add(vip);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Thêm VIP thành công: TypeVip {TypeVip}", vip.TypeVip);
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("ModelState Error: {Error}", error.ErrorMessage);
            }

            return View(vip);
        }

        // GET: Admin/Vip/Update/5
        public async Task<IActionResult> Update(long id)
        {
            var vip = await _context.Vips.FindAsync(id);
            if (vip == null)
            {
                _logger.LogWarning("Không tìm thấy VIP với ID: {Id}", id);
                return NotFound();
            }
            return View(vip);
        }

        // POST: Admin/Vip/Update/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(long id, Vip vip)
        {
            if (id != vip.Id)
            {
                _logger.LogWarning("ID không khớp: {Id} != {VipId}", id, vip.Id);
                return BadRequest();
            }

            var existingVip = await _context.Vips.FindAsync(id);
            if (existingVip == null)
            {
                _logger.LogWarning("Không tìm thấy VIP với ID: {Id}", id);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingVip.TypeVip = vip.TypeVip;
                    existingVip.Price = vip.Price;
                    existingVip.NoPick = vip.NoPick;
                    existingVip.MoneyBack = vip.MoneyBack;

                    _context.Update(existingVip);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cập nhật VIP thành công: TypeVip {TypeVip}", vip.TypeVip);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi cập nhật VIP: TypeVip {TypeVip}", vip.TypeVip);
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật. Vui lòng thử lại.");
                }
            }

            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("ModelState Error: {Error}", error.ErrorMessage);
            }

            return View(vip);
        }

        // POST: Admin/Vip/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var vip = await _context.Vips.FindAsync(id);
                if (vip == null)
                {
                    _logger.LogWarning("Không tìm thấy VIP với ID: {Id}", id);
                    return NotFound();
                }

                _context.Vips.Remove(vip);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã xóa VIP với ID: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa VIP với ID: {Id}", id);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi xóa. Vui lòng thử lại.");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}