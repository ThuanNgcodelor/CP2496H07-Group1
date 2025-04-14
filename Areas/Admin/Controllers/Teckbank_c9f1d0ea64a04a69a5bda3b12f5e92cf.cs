using CP2496H07Group1.Configs.Jwt;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Areas.Admin.Controllers;

[Area("Admin")]
public class Teckbank_c9f1d0ea64a04a69a5bda3b12f5e92cf : Controller
{
    private readonly IAuthService _authService;
    private readonly JwtHandler _jwtHandler;
    private readonly RedisService _redisService;

    public Teckbank_c9f1d0ea64a04a69a5bda3b12f5e92cf(IAuthService authService, JwtHandler jwtHandler, RedisService redisService)
    {
        _authService = authService;
        _jwtHandler = jwtHandler;
        _redisService = redisService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }
    
    
    [HttpPost]
    public async Task<IActionResult> Login(Models.Admin model)
    {
        try
        {
            var admin = await _authService.LoginAdmin(model);
            if (admin == null)
            {
                return Json(new { success = false, message = "Login Failed" });
            }

            var accessToken = await _jwtHandler.GenerateTokenAdmin(admin);
            var refreshToken = await _jwtHandler.GenerateRefreshTokenAdmin(admin);

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

            var cache = _redisService.GetDatabase();
            await cache.StringSetAsync($"refreshToken:{admin.Id}", refreshToken, TimeSpan.FromDays(7));

            var roles = admin.Roles.Select(r => r.RoleName).ToList();
            if (roles.Contains("Admin"))
            {
                Url.Action("Index", "Dashboard", new { area = "Admin" });
            }
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        
    }
}