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

    public AuthController(IAuthService authService, JwtHandler jwtHandler, RedisService redisService,
        AppDataContext appDataContext)
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

    public IActionResult Forgot()
    {
        return View();
    }
    
    [HttpGet]
    public IActionResult EnterOtp()
    {
        return View();
    }
    [HttpGet]
    public IActionResult NewPassword()
    {
        return View();
    }
    

    [HttpPost]
    public async Task<IActionResult> Forgot(string phoneNumber)
    {
        try
        {
            string result = await _authService.ForgotPassword(phoneNumber);
            TempData["PhoneNumber"] = phoneNumber;  
            TempData.Keep("PhoneNumber"); 
            TempData["Message"] = result;
            return RedirectToAction("EnterOtp");  
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View();
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> EnterOtp(string? phoneNumber, string otp)
    {
        try
        {
            // Nếu phoneNumber null, lấy từ TempData
            phoneNumber ??= TempData["PhoneNumber"]?.ToString();
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return RedirectToAction("Forgot");
            }

            TempData["PhoneNumber"] = phoneNumber; // Lưu lại phoneNumber để dùng tiếp
            TempData.Keep("PhoneNumber");

            await _authService.ValidateOtp(phoneNumber, otp);
            return RedirectToAction("NewPassword");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View();
        }
    }



    [HttpPost]
    public async Task<IActionResult> NewPassword(string? phoneNumber,string newPassword, string confirmPassword)
    {
        phoneNumber ??= TempData["PhoneNumber"]?.ToString();
        if (string.IsNullOrEmpty(phoneNumber))
        {
            return RedirectToAction("Forgot");
        }

        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Passwords do not match.");
            return View();
        }

        try
        {
            await _authService.ResetPassword(phoneNumber, newPassword,confirmPassword);
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View();
        }
    }


    [HttpPost]
    public async Task<IActionResult> Register(User model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _authService.RegisterOpt(model);
            
            TempData["Email"] = user.Email;
            TempData["Message"] = "OTP code has been sent to your email";
            
            return RedirectToAction("VerifyOtp", "Auth");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("PhoneNumber", ex.Message);
            return View(model);
        }
    }
    
    
    [HttpGet]
    public IActionResult VerifyOtp()
    {
        var email = TempData["Email"]?.ToString();
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Register", "Auth");
        }
        TempData.Keep("Email");
        return View(new VerifyOtpModel { Email = email });
    }

    [HttpPost]
    public async Task<IActionResult> VerifyOtp(VerifyOtpModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Email"] = model.Email;
            return View(model);
        }

        var user = await _authService.ConfirmEmail(model.Email, model.Otp);
        if (user == null)
        {
            ModelState.AddModelError("Otp", "OTP code is incorrect. Please check and try again.");
            TempData["Email"] = model.Email;
            return View(model);
        }

        TempData["Message"] = "Xác nhận OTP thành công!";
        return RedirectToAction("Login", "Auth");
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
        try
        {
            var user = await _authService.Login(model);
            if (user == null) 
            {
                return Json(new { success = false, message = "An unknown error occurred" });
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

            var cache = _redisService.GetDatabase();
            await cache.StringSetAsync($"refreshToken:{user.Id}", refreshToken, TimeSpan.FromDays(7));

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
        catch (ArgumentException ex)
        {
            string message = ex.Message switch
            {
                "Phone number is invalid" => "Phone number is invalid",
                "User has been locked" => "User has been locked",
                "Password is invalid" => "Password is invalid",
                _ => "An unknown error occurred"
            };
            return Json(new { success = false, message });
        }
    }
}