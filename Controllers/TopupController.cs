
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Jwt;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Dtos;
using CP2496H07Group1.Models;
using CP2496H07Group1.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Security.Claims;

namespace CP2496H07Group1.Controllers
{
    public class TopupController : Controller
    {
        private readonly IAuthService _authService;
        private readonly JwtHandler _jwtHandler;
        private readonly RedisService _redisService;
        private readonly AppDataContext _context;

        public TopupController(IAuthService authService, JwtHandler jwtHandler, RedisService redisService, AppDataContext appDataContext)
        {
            _authService = authService;
            _jwtHandler = jwtHandler;
            _redisService = redisService;
            _context = appDataContext;
        }
        public async Task<IActionResult> HistoryTopup()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(value) || !long.TryParse(value, out long userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["LoanError"] = $"Người dùng không tồn tại (debug: userId={userId})";
                return RedirectToAction("Login", "Auth");
            }

            var topups = await _context.Topups
                .Where(t => t.AccountId == user.Id)
                .Include(t => t.Account)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(topups);
        }



        public IActionResult Index()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                return RedirectToAction("Login", "Auth");
            }
            var accounts = _context.Accounts
             .Where(a => a.UserId == userId)
             .Select(a => new AccountViewModel
             {
                 AccountId = a.Id,
                 AccountNumber = a.AccountNumber,
                 Balance = a.Balance,
                 DisplayText = $"Card: {a.AccountNumber} | Balance: ${a.Balance.ToString("F2")}"
             })
             .ToList();

            var viewModel = new TopupInputModel
            {
                Accounts = accounts
            };



            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(TopupInputModel model, IFormFile imageUpload)
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(value) || !long.TryParse(value, out var userId))
            {
                TempData["LoanError"] = "Không xác định được người dùng.";
                return RedirectToAction("Index");
            }

            string imagePath = "";

            if (imageUpload != null && imageUpload.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/topups");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageUpload.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    imageUpload.CopyTo(fileStream);
                }

                imagePath = "/images/topups/" + uniqueFileName; // relative path to be stored
            }
                
            var topup = new Topup
            {
                AccountId = model.AccountId,
                AmountTopup = model.AmountTopup,
                Description = model.Description,
                CreatedAt = DateTime.Now,
                Image = imagePath,
                Status = false
            };


            _context.Topups.Add(topup);
            _context.SaveChanges();
            TempData["TopupSuccess"] = "The Topup request has been submitted and is being processed....";

            return RedirectToAction("Index");
            
        }
    }                                                                                                                                                                                                                           
}
