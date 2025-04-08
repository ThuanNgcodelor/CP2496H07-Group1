using System.Security.Claims;
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Jwt;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Models;
using CP2496H07Group1.Services.Account;
using CP2496H07Group1.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CP2496H07Group1.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly JwtHandler _jwtHandler;
    private readonly RedisService _redisService;
    private readonly AppDataContext _context;
    private readonly IAccountService _accountService;

    public AuthController(IAuthService authService, JwtHandler jwtHandler, RedisService redisService,
        AppDataContext appDataContext, IAccountService accountService)
    {
        _authService = authService;
        _jwtHandler = jwtHandler;
        _redisService = redisService;
        _context = appDataContext;
        _accountService = accountService;
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

    public IActionResult Register()
    {
        return View();
    }

    public IActionResult Forgot()
    {
        return View();
    }

    [HttpGet]
    public IActionResult EnterOtpCreateAccount()
    {
        TempData["Message"] = "OTP code has been sent to your email";
        return View();
    }

    [HttpGet]
    public IActionResult RecentTransactions()
    {
        return View();
    }


    [HttpGet]
    public IActionResult EnterOtp()
    {
        TempData["Message"] = "OTP code has been sent to your email";
        return View();
    }

    [HttpGet]
    public IActionResult NewPassword()
    {
        return View();
    }

    [HttpGet]
    public IActionResult CreateAccount()
    {
        var hashedId = User.Claims.FirstOrDefault(c => c.Type == "hashed_id")?.Value;
        if (string.IsNullOrEmpty(hashedId))
        {
            return RedirectToAction("Login", "Auth");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount(string accountType, int pin, int confirmPin)
    {
        var pinStr = pin.ToString();
        var confirmPinStr = confirmPin.ToString();

        if (pinStr.Length != 6 || confirmPinStr.Length != 6 || pin != confirmPin)
        {
            ModelState.AddModelError("", "PIN must be 6 digits and match.");
            return View();
        }

        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (value != null)
        {
            var userId = long.Parse(value);
            await _accountService.SendMailCreateAccount(userId, accountType, pin);
            TempData["UserId"] = userId.ToString();
        }

        return RedirectToAction("EnterOtpCreateAccount");
    }


    [HttpPost]
    public async Task<IActionResult> EnterOtpCreateAccount(long userId, string otp)
    {
        if (string.IsNullOrWhiteSpace(otp) || otp.Length != 6 || !otp.All(char.IsDigit))
        {
            ModelState.AddModelError("", "OTP must be 6 digits.");
            return View();
        }

        var success = await _accountService.VerifyOtpAndCommitCreateAccount(userId, otp);
        if (success == null)
        {
            ModelState.AddModelError("", "OTP incorrect or expired.");
            return View();
        }
        

        TempData["Success"] = "Create Card Success!";
        return RedirectToAction("Users");
    }


    [HttpGet]
    public IActionResult Account()
    {
        var hashedId = User.Claims.FirstOrDefault(c => c.Type == "hashed_id")?.Value;
        if (string.IsNullOrEmpty(hashedId))
        {
            return RedirectToAction("Login", "Auth");
        }

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
            phoneNumber ??= TempData["PhoneNumber"]?.ToString();
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return RedirectToAction("Forgot");
            }

            TempData["PhoneNumber"] = phoneNumber;
            TempData.Keep("PhoneNumber");

            await _authService.ValidateOtp(phoneNumber, otp);

            return RedirectToAction("NewPassword");
        }
        catch (Exception)
        {
            // Gắn lỗi vào trường OTP để hiển thị ngay dưới input
            ModelState.AddModelError("Otp", "OTP code is incorrect. Please check and try again.");
            return View();
        }
    }



    [HttpPost]
    public async Task<IActionResult> NewPassword(string? phoneNumber, string newPassword, string confirmPassword)
    {
        phoneNumber ??= TempData["PhoneNumber"]?.ToString();
        ViewBag.PhoneNumber = phoneNumber;
        if (string.IsNullOrEmpty(phoneNumber))
        {
            return RedirectToAction("Forgot");
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.PhoneNumber = phoneNumber;
            ModelState.AddModelError("confirmPassword", "Passwords do not match.");
            return View();
        }
        try
        {
            await _authService.ResetPassword(phoneNumber, newPassword, confirmPassword);
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
                _ => throw new ArgumentOutOfRangeException()
            };
            return Json(new { success = false, message });
        }
    }

    //1: Get Id trong redis
    //2: Check Id xem no co phai hay ko
    //3: Neu co thi generate AccessToken
    //4: Neu ko thi ke me
    [HttpPost]
    public async Task<IActionResult> RefreshToken()
    {
        if (!Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
        {
            return Unauthorized("Refresh token not found");
        }

        // Giải mã RefreshToken để lấy UserId
        var principal = _jwtHandler.ValidateToken(refreshToken);
        if (principal == null)
        {
            return Unauthorized("Invalid token");
        }

        var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var cache = _redisService.GetDatabase();
        var storedToken = await cache.StringGetAsync($"refreshToken:{userId}");

        if (storedToken.IsNullOrEmpty || storedToken != refreshToken)
        {
            return Unauthorized("Invalid or expired refresh token");
        }

        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

        if (user == null)
        {
            return Unauthorized("User not found");
        }

        var newAccessToken = await _jwtHandler.GenerateToken(user);
        Response.Cookies.Append("AccessToken", newAccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(30)
        });

        Console.WriteLine($"AccessToken: {newAccessToken}");
        return Ok(new { token = newAccessToken });
    }

    [HttpGet]
    public async Task<IActionResult> IsLoggedIn()
    {
        // Lấy refresh token từ cookie
        if (!Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
        {
            return Unauthorized("Refresh token not found");
        }

        // Validate refresh token để lấy thông tin user
        var principal = _jwtHandler.ValidateToken(refreshToken);
        if (principal == null)
        {
            return Unauthorized("Invalid refresh token");
        }

        var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        // Kiểm tra token có còn hợp lệ trong Redis không
        var cache = _redisService.GetDatabase();
        var storedToken = await cache.StringGetAsync($"refreshToken:{userId}");

        if (storedToken.IsNullOrEmpty || storedToken != refreshToken)
        {
            return Unauthorized("Token expired or mismatch");
        }

        // Token hợp lệ
        return Ok(new { loggedIn = true });
    }
}