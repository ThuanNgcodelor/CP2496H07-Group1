using System.Security.Claims;
using CP2496H07Group1.Configs.Jwt;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Dtos;
using CP2496H07Group1.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Areas.Admin.Controllers;

[Area("Admin")]
public class Teckbank_c9f1d0ea64a04a69a5bda3b12f5e92cf : Controller
{
    private readonly IAuthService _authService;
    private readonly JwtHandler _jwtHandler;
    private readonly RedisService _redisService;

    public Teckbank_c9f1d0ea64a04a69a5bda3b12f5e92cf(IAuthService authService, JwtHandler jwtHandler,
        RedisService redisService)
    {
        _authService = authService;
        _jwtHandler = jwtHandler;
        _redisService = redisService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            return RedirectToAction("Users", "Auth");
        }
        return View();
    }

    public IActionResult Logout()
    {
        Response.Cookies.Delete("AccessToken");
        return RedirectToAction("Login" , "Teckbank_c9f1d0ea64a04a69a5bda3b12f5e92cf" , new { area = "Admin" });
    }


    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        try
        {
            var admin = await _authService.LoginAdmin(model);
            if (admin == null)
            {
                ModelState.AddModelError("", "Email or password is incorrect. Please try again.");
                return View(model); 
            }

            var accessToken = await _jwtHandler.GenerateTokenAdmin(admin);

            Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(30)
            });
  
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Password and Email error");
            return View(model);
        }
    }
}