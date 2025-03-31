using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Jwt;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Models;
using CP2496H07Group1.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly JwtHandler _jwtHandler;
    private readonly RedisService _redisService;
    private readonly AppDataContext _context;

    public AuthController(IAuthService authService, JwtHandler jwtHandler, RedisService redisService, AppDataContext appDataContext)
    {
        _authService = authService;
        _jwtHandler = jwtHandler;
        _redisService = redisService;
        _context = appDataContext;
    }

    public IActionResult Login()
    {
        return View();
    }
    
    public IActionResult Register()
    {
        return View();
    }
    
    [HttpGet]
    public IActionResult Users()
    {
        var hashedId = User.Claims.FirstOrDefault(c => c.Type == "hashed_id")?.Value;
        if (string.IsNullOrEmpty(hashedId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var users = _context.Users.ToList();
        var user = users.FirstOrDefault(u => _jwtHandler.HashId(u.Id) == hashedId);

        if (user == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        return View(user);
    }
    [HttpPost]
    public async Task<IActionResult> Login(User model)
    {
        var user = await _authService.Login(model);
        if (user == null)
        {
            return Json(new { success = false, message = "Invalid username or password" });
        }
        

        var accessToken = await _jwtHandler.GenerateToken(user);
        var refreshToken = await _jwtHandler.GenerateRefreshToken(user);
        
        Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(30)
        });

        Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(30)
        });
        
        // Lưu RefreshToken vào Redis với key "refreshToken:{UserId}"
        var cache = _redisService.GetDatabase();
        await cache.StringSetAsync($"refreshToken:{user.Id}", refreshToken, TimeSpan.FromDays(7));

        // Kiểm tra danh sách Roles của user
        var roles = user.Roles.Select(r => r.RoleName).ToList();

        string? redirectUrl = null;
        if (roles.Contains("Admin"))
        {
            redirectUrl = Url.Action("Index", "Dashboard", new { area = "Admin" });
        }
        else if (roles.Contains("Client"))
        {
            redirectUrl = Url.Action("Users", "Auth");
        }

        return Json(new { success = true, redirectUrl });
    }
}