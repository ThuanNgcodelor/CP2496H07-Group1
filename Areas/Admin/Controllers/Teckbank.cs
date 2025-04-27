using System.Security.Claims;
using CP2496H07Group1.Configs.Jwt;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Dtos;
using CP2496H07Group1.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/{year:int}/{month:int}/{day:int}/Teckbank")]
public class TeckbankController : Controller
{
    private readonly IAuthService _authService;
    private readonly JwtHandler _jwtHandler;
    private readonly RedisService _redisService;

    public TeckbankController(IAuthService authService, JwtHandler jwtHandler,
        RedisService redisService)
    {
        _authService = authService;
        _jwtHandler = jwtHandler;
        _redisService = redisService;
    }

    [HttpGet("Login")]
    public IActionResult Login(int year, int month, int day)
    {
        var today = DateTime.UtcNow.Date;

        if (year != today.Year || month != today.Month || day != today.Day)
        {
            return Forbid();
        }

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
        return RedirectToAction("Login", "Teckbank", new
        {
            area = "Admin",
            year = DateTime.UtcNow.Year,
            month = DateTime.UtcNow.Month,
            day = DateTime.UtcNow.Day
        });

    }


    [HttpPost("Login")]
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
                Expires = DateTime.UtcNow.AddHours(4)
            });

            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
        catch
        {
            ModelState.AddModelError("", "Password and Email error");
            return RedirectToAction("Login"); 
        }
    }
}