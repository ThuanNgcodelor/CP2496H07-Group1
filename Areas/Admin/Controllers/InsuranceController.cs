using CP2496H07Group1.Models;
using CP2496H07Group1.Services.Package;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize("Admin")]
public class InsuranceController : Controller
{
    private readonly IPackageService _packageService;

    public InsuranceController(IPackageService packageService)
    {
        _packageService = packageService;
    }

    [HttpGet]
    public IActionResult Add()
    {
        return View();
    }
    
    [HttpGet]
    public async  Task<IActionResult> Update(long id)
    {
        var package = await _packageService.FindInsurancePackageById(id);
        return View(package);
    }
    
    public Task<IActionResult> Index(int? page, string? keyword)
    {
        int pageSize = 5;
        int pageNumber = page ?? 1;
        var product =  _packageService.GetAllInsurancePackages(pageNumber, pageSize, keyword);
        ViewBag.Keyword = keyword;
        return Task.FromResult<IActionResult>(View(product));
    }
    
    [HttpPost]
    public async Task<IActionResult> Add(InsurancePackage model)
    {
        await _packageService.AddInsurancePackage(model);
        return RedirectToAction("Index", "Insurance");
    }

    [HttpPost]
    public async Task<IActionResult> Update(InsurancePackage model,long id)
    {
        await _packageService.UpdateInsurancePackage(model,id);
        return RedirectToAction("Index", "Insurance");
    }
}