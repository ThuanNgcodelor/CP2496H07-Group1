using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CP2496H07Group1.Models;
using CP2496H07Group1.Configs.Database;
using Microsoft.AspNetCore.Authorization;
using CP2496H07Group1.Services.Auth;
using CP2496H07Group1.Configs.Jwt;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Configs.Email;
using CP2496H07Group1.Configs.Sms;
using Twilio.TwiML.Voice;
using Microsoft.Extensions.Logging;
using System.Security.Claims;


namespace CP2496H07Group1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    public class TopupsController : Controller
    {
        private readonly IAuthService _authService;
        private readonly JwtHandler _jwtHandler;
        private readonly RedisService _redisService;
        private readonly AppDataContext _context;
        private readonly ILogger<AuthService> _logger;
        public TopupsController(IAuthService authService, JwtHandler jwtHandler, RedisService redisService, ILogger<AuthService> logger,
            AppDataContext appDataContext)
        {
            _authService = authService;
            _jwtHandler = jwtHandler;
            _redisService = redisService;
            _context = appDataContext;
            _logger = logger;

        }

        // GET: Admin/Topups
        public async Task<IActionResult> Index(string searchString, int page = 1, int pageSize = 5  )
        {
            var query = _context.Topups
                .Include(t => t.Account)
                .AsQueryable(); // Bắt đầu bằng IQueryable

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(t =>
                    (t.Account != null && t.Account.AccountNumber.Contains(searchString)) ||
                    t.AmountTopup.ToString().Contains(searchString) ||
                    t.CreatedAt.ToString("yyyy-MM-dd HH:mm").Contains(searchString)
                );
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var topups = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Keyword = searchString;

            return View(topups);
        }




        // GET: Admin/Topups/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var topup = await _context.Topups
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (topup == null) return NotFound();

            return View(topup);
        }
        [HttpPost]
        public async Task<IActionResult> ConfirmTopup(int id)
        {
            var topup = await _context.Topups
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (topup == null)
                return NotFound();

            if (topup.Status)
                return Conflict("Top-up already confirmed.");

            if (topup.Account == null)
                return BadRequest("Top-up has no associated account.");

            topup.Status = true;
            topup.Account.Balance += topup.AmountTopup;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDescription(long id, [FromBody] string newDescription)
        {
            // Include Account để tránh null khi truy cập Account.UserId
            var topup = await _context.Topups
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (topup == null)
            {
                _logger.LogError("Top-up not found with id {Id}", id);
                return NotFound();
            }

            topup.Description = newDescription;
            await _context.SaveChangesAsync();

            if (topup.Account == null)
            {
                _logger.LogWarning("Account not found for Top-up with id {Id}", id);
                return NotFound("Không tìm thấy tài khoản.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == topup.Account.UserId);

            if (user == null)
            {
                _logger.LogWarning("User not found for Topup with id {Id}", id);
                return NotFound("Không tìm thấy người dùng.");
            }

            try
            {
                _logger.LogInformation("Attempting to send email for Top-up id {TopupId} to {Email}", id, user.Email);
                await _authService.SendTopupConfirmationEmail(user, topup);
                _logger.LogInformation("Email sent successfully to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email for Top-up id {TopupId} to {Email}", id, user.Email);
                return StatusCode(500, "Lỗi máy chủ nội bộ khi gửi email.");
            }

            return Ok();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var topup = await _context.Topups.FindAsync(id);
                if (topup == null)
                {
                    _logger.LogWarning("Topup with ID not found: {Id}", id);
                    return NotFound();
                }

                _context.Topups.Remove(topup);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted Topup with ID: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when deleting Topup with ID: {Id}", id);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi xóa. Vui lòng thử lại.");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
    