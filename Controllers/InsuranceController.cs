using CP2496H07Group1.Services.Package;
using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Controllers;

public class InsuranceController : Controller
{
    private readonly IPackageService _packageService;

    public InsuranceController(IPackageService packageService)
    {
        _packageService = packageService;
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
    
    
}