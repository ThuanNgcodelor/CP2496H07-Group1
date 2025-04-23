using System.Security.Claims;
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Services.Account;
using CP2496H07Group1.Services.Package;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CP2496H07Group1.Controllers;

public class InsuranceController : Controller
{
    private readonly IPackageService _packageService;
    private readonly IAccountService _accountService;
    private readonly AppDataContext _context;
    

    public InsuranceController(IPackageService packageService, IAccountService accountService, AppDataContext context)
    {
        _packageService = packageService;
        _accountService = accountService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> UserInsurance()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (value == null)
            return RedirectToAction("Login", "Auth");

        var userId = long.Parse(value);
        var userInsurances = await _packageService.GetUserInsurances(userId); 
        return View(userInsurances);
    }

    [HttpGet]
    public async Task<IActionResult> UserSec()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (value == null)
        {
            return RedirectToAction("Login", "Auth");
        }
        var userId = long.Parse(value);
        var userInsurances = await _packageService.GetUserSec(userId);
        return View(userInsurances);
    }


    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var insuranceId = await _packageService.FindInsurancePackageById(id);
        return View(insuranceId);
    }

    public Task<IActionResult> Index(int? page, string? keyword)
    {
        int pageSize = 5;
        int pageNumber = page ?? 1;
        var product = _packageService.GetAllInsurancePackages(pageNumber, pageSize, keyword);
        ViewBag.Keyword = keyword;
        return Task.FromResult<IActionResult>(View(product));
    }


    [HttpGet]
    public async Task<IActionResult> BuyNow(long id)
    {
        var insurance = await _packageService.FindInsurancePackageById(id);
        if (insurance == null)
        {
            return NotFound();
        }

        return Json(insurance);
    }

    [HttpPost]
    public async Task<IActionResult> PaymentCard(long id)
    {
        try
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (value == null)
                return RedirectToAction("Login", "Auth");

            var userId = long.Parse(value);

            var result = await _packageService.PaymentByCard(id, userId);

            if (result != null)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Payment service failed." });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Json(new { success = false, message = "Internal server error." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> PaymentByCard(long id)
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (value == null)
            return RedirectToAction("Login", "Auth");

        var userId = long.Parse(value);
        var account = await _accountService.GetAccounts(userId);

        ViewBag.InsuranceId = id;

        return View(account);
    }
    
    
    [HttpPost]
    public async Task<IActionResult> PaymentSec(long id)
    {
        try
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (value == null)
                return RedirectToAction("Login", "Auth");

            var userId = long.Parse(value);

            var result = await _packageService.PaymentByCard(id, userId);

            if (result != null)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Payment service failed." });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Json(new { success = false, message = "Internal server error." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> PaymentBySec(long id)
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (value == null)
            return RedirectToAction("Login", "Auth");

        var userId = long.Parse(value);
        var account = await _accountService.GetAccounts(userId);

        ViewBag.InsuranceId = id;

        return View(account);
    }

    // [HttpPost]
    // public async Task<IActionResult> PaymentByCardInsurancePackage(long insuranceId, long userId, long accountId,
    //     int pin)
    // {
    //     try
    //     {
    //         var userInsurance = await _packageService.GetUserInsurance(userId);
    //         var existingPackageType = userInsurance?.Package.Type;
    //         var validType = new[] { "Health", "Travel", "Life", "Property" };
    //
    //         if (validType.Contains(existingPackageType))
    //         {
    //             return Json(new { success = true, message = "You already have this insurance package." });
    //         }
    //         
    //         await _packageService.PaymentByCardInsurance(insuranceId, userId, accountId, pin);
    //         return Json(new { success = true, message = "Payment service successful." });
    //     }
    //     catch (Exception ex)
    //     {
    //         return Json(new { success = false, message = "Pin service failed." });
    //     }
    // }
    //
    //
    // [HttpPost]
    // public async Task<IActionResult> PaymentBySecInsurancePackage(long insuranceId, long userId, long accountId,
    //     int pin)
    // {
    //     try
    //     {
    //         var userInsurance = await _packageService.GetUserInsurance(userId);
    //         var existingPackageType = userInsurance?.Package.Type;
    //         var validType = new[] { "Health", "Travel", "Life", "Property" };
    //
    //         if (validType.Contains(existingPackageType))
    //         {
    //             return Json(new { success = true, message = "You already have this insurance package." });
    //         }
    //         
    //         await _packageService.PaymentBySecInsurance(insuranceId, userId, accountId, pin);
    //         return Json(new { success = true, message = "Your sec is pending approval.." });
    //     }
    //     catch (Exception ex)
    //     {
    //         return Json(new { success = false, message = "Pin service failed." });
    //     }
    // }
    //
    //
    //
    
    private async Task<IActionResult> HandleInsurancePayment(long insuranceId, long userId, long accountId, int pin, string paymentType)
    {
        try
        {
            var insurance = await _context.InsurancePackages.FirstOrDefaultAsync(p => p.Id == insuranceId);
            if (insurance == null)
            {
                return Json(new { success = false, message = "Invalid insurance package." });
            }

            var userInsurance = await _context.UserInsurances
                .Include(p => p.Package)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Package.Type == insurance.Type && p.Status == "Active");

            if (userInsurance != null)
            {
                return Json(new { success = false, message = $"You already have an active {insurance.Type} insurance package." });
            }

            var validTypes = new[] { "Health", "Travel", "Life", "Property" };
            if (!validTypes.Contains(insurance.Type))
            {
                return Json(new { success = false, message = "Invalid insurance type." });
            }

            // Process payment
            await _packageService.PaymentInsurance(insuranceId, userId, accountId, pin, paymentType);
            string message = paymentType == "Card" ? "Payment successful." : "Your payment is pending approval.";
            return Json(new { success = true, message });
        }
        catch (Exception ex)
        {
            string message;
            switch (ex.Message)
            {
                case "Insufficient funds in the account.":
                    message = "Not enough money in the account.";
                    break;
                case "Wrong PIN":
                    message = "Incorrect PIN code.";
                    break;
                case "Card blocked":
                    message = "Your card is blocked.";
                    break;
                case "Account not found":
                    message = "Account does not exist.";
                    break;
                case "No active credit card linked to the account.":
                    message = "No active credit card found.";
                    break;
                case "Unsupported payment type.":
                    message = "Selected payment type is not supported.";
                    break;
                case "Invalid payment data":
                    message = "Invalid payment details provided.";
                    break;
                default:
                    message = "An error occurred during payment processing.";
                    break;
            }

            return Json(new { success = false, message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> PaymentByCardInsurancePackage(long insuranceId, long userId, long accountId, int pin)
    {
        return await HandleInsurancePayment(insuranceId, userId, accountId, pin, "Card");
    }

    [HttpPost]
    public async Task<IActionResult> PaymentBySecInsurancePackage(long insuranceId, long userId, long accountId, int pin)
    {
        return await HandleInsurancePayment(insuranceId, userId, accountId, pin, "Sec");
    }

    [HttpPost("Insurance/UserSec/UpdateSec/{id}")]
    public async Task<IActionResult> UpdateSec(long id)
    {
        try
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (value == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = long.Parse(value);
            await _packageService.UpdateSec(userId, id);
            return Json(new { success = true, message = "Cancel successfully." });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Payment service failed." });
        }
    }

    
}