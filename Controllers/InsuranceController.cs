using System.Security.Claims;
using CP2496H07Group1.Models;
using CP2496H07Group1.Services.Account;
using CP2496H07Group1.Services.Package;
using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Controllers;

public class InsuranceController : Controller
{
    private readonly IPackageService _packageService;
    private readonly IAccountService _accountService;

    public InsuranceController(IPackageService packageService,IAccountService accountService)
    {
        _packageService = packageService;
        _accountService = accountService;
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
    public async Task<IActionResult> PaymentCard(long insuranceId)
    {
        try
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (value == null)
                return RedirectToAction("Login", "Auth");
    
            var userId = long.Parse(value);
            var payment = await _packageService.PaymentByCard(insuranceId, userId);
            return Json(payment);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
     
    }

    [HttpGet]
    public async Task<IActionResult> PaymentCard()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (value == null)
            return RedirectToAction("Login", "Auth");

        var userId = long.Parse(value);
        var account = await _accountService.GetAccounts(userId);

        return View(account);
    }

    
}