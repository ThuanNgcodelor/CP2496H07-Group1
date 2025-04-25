using System.Diagnostics;
using System.Security.Claims;
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Jwt;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Dtos;
using CP2496H07Group1.Models;
using CP2496H07Group1.Services.Account;
using CP2496H07Group1.Services.Auth;
using Microsoft.AspNetCore.Authorization;
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
    private readonly ITransactionService _transactionService;

    public AuthController(IAuthService authService, JwtHandler jwtHandler, RedisService redisService,
        AppDataContext appDataContext, IAccountService accountService, ITransactionService transactionService)
    {
        _authService = authService;
        _jwtHandler = jwtHandler;
        _redisService = redisService;
        _context = appDataContext;
        _accountService = accountService;
        _transactionService = transactionService;
    }


    [HttpPost]
    public async Task<IActionResult> Pay(string type, decimal? amount)
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(value))
        {
            return Json(new { success = false, message = "User not authenticated!" });
        }

        long userId = long.Parse(value);
        var account = _context.Accounts
            .Include(a => a.CreditCard)
            .FirstOrDefault(a => a.UserId == userId && a.AccountType == "Credit Card");

        if (account == null || account.CreditCard == null || !account.CreditCard.IsActive)
            return Json(new { success = false, message = "No active credit card found!" });

        Console.WriteLine();
        var now = DateTime.Now.Date;
        var statementDate = account.CreditCard.StatementDate.Date;
        var dueDate = account.CreditCard.DueDate.Date;

        if (now < statementDate || now > dueDate)
        {
            return Json(new
            {
                success = false,
                message = $"Payment allowed only from {statementDate:yyyy-MM-dd} to {dueDate:yyyy-MM-dd}."
            });
        }


        bool success = false;
        if (type == "Full")
        {
            success = await _accountService.PayFullDebtAsync(userId);
        }
        else if (type == "Partial" && amount.HasValue)
        {
            success = await _accountService.PayPartialDebtAsync(userId, amount.Value);
        }

        if (success)
        {
            return Json(new { success = true, message = "Payment successful!" });
        }

        return Json(new { success = false, message = "Payment failed! Please check balance or debt." });
    }


    [HttpGet]
    public IActionResult Users()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "Admin")
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

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

        var totalAmount = _context.Accounts
            .Where(a => a != null && a.UserId == user.Id)
            .Sum(a => a!.Balance);
        var monthlyExpenses = _context.Users
            .Where(u => u.Id == user.Id)
            .SelectMany(u => u.Accounts)
            .SelectMany(a => a.TransactionsFrom)
            .Sum(t => t.Amount);
        ViewBag.MonthlyExpenses = monthlyExpenses;
        ViewBag.TotalAmount = totalAmount;

        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeMail([FromBody] ChangeEmailRequest request)
    {
        try
        {
            string result = await _authService.SendOtpChangeEmail(request.OldEmail, request.NewEmail);
            return Json(new { success = true, message = result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmOtpChangeEmail([FromBody] ConfirmOtpRequest request)
    {
        try
        {
            var user = await _authService.ConfirmOtpChangeEmail(request.OldEmail, request.NewEmail, request.InputOtp);
            return Json(new { success = true, message = "Email changed successfully!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
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
    public IActionResult EnterOtpCreateCard()
    {
        TempData["Message"] = "OTP code has been sent to your email";
        return View();
    }

    [HttpGet]
    public IActionResult RecentTransactions(DateTime? fromDate, DateTime? toDate, string types, int page = 1)
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(value))
        {
            return RedirectToAction("Login", "Auth");
        }

        var transactionTypes = _context.Transactions
            .Select(t => t.TransactionType)
            .Distinct()
            .ToList();

        ViewBag.TransactionTypes = transactionTypes;

        long userId = long.Parse(value);
        List<string> typesList = string.IsNullOrEmpty(types) ? new List<string>() : new List<string> { types };
        var transaction = _transactionService.GetTransactions(userId, fromDate, toDate, typesList, page, 5);

        return View(transaction);
    }


    [HttpGet]
    public IActionResult EnterOtp()
    {
        TempData.Keep("PhoneNumber");

        string? phone = TempData["PhoneNumber"] as string;
        string? message = TempData["Message"] as string;

        ViewBag.PhoneNumber = phone;
        ViewBag.Message = message ?? "OTP code has been sent to your email";

        return View();
    }

    [HttpGet]
    public IActionResult NewPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CardDetails(long accountId, string pin)
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (value == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        long userId = long.Parse(value);

        var account = await _accountService.GetAccountDetails(userId, accountId, pin);
        if (account == null)
        {
            ModelState.AddModelError("", "Invalid PIN.");
            return RedirectToAction("Card", "Auth");
        }

        // Lưu dấu xác thực 1 lần
        TempData["AccessGranted"] = "true";
        TempData.Keep("AccessGranted");

        return View(account);
    }


    [HttpGet]
    public async Task<IActionResult> Card()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (value == null)
            return RedirectToAction("Login", "Auth");

        var userId = long.Parse(value);
        var account = await _accountService.GetAccounts(userId);

        return View(account);
    }


    [HttpGet]
    public IActionResult CreateCard()
    {
        var hashedId = User.Claims.FirstOrDefault(c => c.Type == "hashed_id")?.Value;
        if (string.IsNullOrEmpty(hashedId))
        {
            return RedirectToAction("Login", "Auth");
        }

        return View();
    }


    [HttpPost]
    public async Task<IActionResult> CreateCard(string accountType, int pin, int confirmPin)
    {
        var pinStr = pin.ToString();
        var confirmPinStr = confirmPin.ToString();

        if (pinStr.Length != 6 || confirmPinStr.Length != 6 || pin != confirmPin)
        {
            ModelState.AddModelError("", "PIN must be 6 digits and match.");
            return View();
        }

        // Lấy userId từ claim
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var userId = long.Parse(userIdClaim);

        if (accountType == "Credit Card")
        {
            bool alreadyHas = await _context.CreditCards
                .AnyAsync(cc => cc.Account.UserId == userId);
            if (alreadyHas)
            {
                ModelState.AddModelError("", "You have a Credit Card");
                return View();
            }
        }
        
        await _accountService.SendMailCreateAccount(userId, accountType, pin);
        TempData["UserId"] = userId.ToString();

        return RedirectToAction("EnterOtpCreateCard");
    }


    [HttpPost]
    [Route("Auth/ResendOtpCreateCard")]
    public async Task<IActionResult> ResendOtpCreateCard()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (value == null)
        {
            return Json(new { success = false, message = "User not found." });
        }

        var userId = long.Parse(value);

        await _accountService.SendMailResendCreateOtp(userId);
        return Json(new { success = true, message = "OTP has been resent to your email." });
    }


    public async Task<IActionResult> EnterOtpCreateCard(long userId, string otp)
    {
        var account = await _accountService.VerifyOtpAndCommitCreateAccount(userId, otp);

        if (account == null)
        {
            ModelState.AddModelError("Otp", "Invalid OTP. Please try again.");
            ViewData["UserId"] = userId;
            return View();
        }

        TempData["Message"] = "Account successfully created!";
        return RedirectToAction("Card");
    }

    [HttpPost]
    public async Task<IActionResult> Forgot(string phoneNumber)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            string result = await _authService.ForgotPassword(phoneNumber);
            TempData["PhoneNumber"] = phoneNumber;
            TempData.Keep("PhoneNumber");
            TempData["Message"] = result;
            return RedirectToAction("EnterOtp");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("PhoneNumber", ex.Message);
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
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
    {
        if (string.IsNullOrEmpty(model.PhoneNumber) ||
            string.IsNullOrEmpty(model.OldPassword) ||
            string.IsNullOrEmpty(model.NewPassword) ||
            string.IsNullOrEmpty(model.ConfirmPassword))
        {
            return Json(new { success = false, message = "All fields are required." });
        }

        if (model.NewPassword != model.ConfirmPassword)
        {
            return Json(new { success = false, message = "Passwords do not match!" });
        }

        try
        {
            await _authService.ChangePassword(model.PhoneNumber, model.OldPassword, model.NewPassword,
                model.ConfirmPassword);
            return Json(new { success = true, message = "Password changed successfully!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
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
            if (ex.Message.Contains("phone") && ex.Message.Contains("email"))
            {
                ModelState.AddModelError("PhoneNumber", ex.Message);
                ModelState.AddModelError("Email", ex.Message);
            }
            else if (ex.Message.Contains("phone"))
            {
                ModelState.AddModelError("PhoneNumber", ex.Message);
            }
            else if (ex.Message.Contains("email"))
            {
                ModelState.AddModelError("Email", ex.Message);
            }

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
    [Route("Auth/ResendOtp")]
    public async Task<IActionResult> ResendOtp(string email)
    {
        await _authService.ResendOtp(email);
        return Json(new { success = true, message = "OTP code has been sent to your email" });
    }

    [HttpPost]
    [Route("Auth/RefreshSendOtpForgotPassword")]
    public async Task<IActionResult> RefreshSendOtpForgotPassword(string phoneNumber)
    {
        await _authService.ForgotPassword(phoneNumber);
        return Json(new { success = true, message = "OTP code has been sent to your email" });
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

        TempData["Message"] = "Confirm Otp success!";
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
            if (roles.Contains("Client"))
            {
                redirectUrl = Url.Action("Users", "Auth");
            }

            return Json(new { success = true, redirectUrl });
        }
        catch (ArgumentException ex)
        {
            string message = ex.Message switch
            {
                { } msg when msg.StartsWith("Password is invalid") =>
                    "Invalid password. 3 incorrect attempts will lock your account. Number of errors: " +
                    msg.Replace("Password is invalid", "").Trim(),
                "Phone number is invalid" => "Phone number is invalid. Please try again.",
                "User has been locked" => "User has been locked out for entering the wrong password more than 3 times",
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

        var principal = _jwtHandler.ValidateToken(refreshToken);
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
        // Console.WriteLine($"AccessToken: {newAccessToken}");
        return Ok(new { token = newAccessToken });
    }

    public async Task<IActionResult> Logout()
    {
        try
        {
            if (!Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
            {
                return BadRequest("No refresh token found");
            }

            var principal = _jwtHandler.ValidateToken(refreshToken);
            var userId = principal?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found");
            }

            var cache = _redisService.GetDatabase();
            await cache.KeyDeleteAsync($"refreshToken:{userId}");
            Response.Cookies.Delete("RefreshToken");
            Response.Cookies.Delete("AccessToken");
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return Unauthorized();
    }


    [HttpGet]
    public async Task<IActionResult> IsLoggedIn()
    {
        if (!Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
        {
            return Unauthorized("Refresh token not found");
        }

        var principal = _jwtHandler.ValidateToken(refreshToken);
        var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var cache = _redisService.GetDatabase();
        var storedToken = await cache.StringGetAsync($"refreshToken:{userId}");
        if (storedToken.IsNullOrEmpty || storedToken != refreshToken)
        {
            return Unauthorized("Token expired or mismatch");
        }

        return Ok(new { loggedIn = true });
    }
}