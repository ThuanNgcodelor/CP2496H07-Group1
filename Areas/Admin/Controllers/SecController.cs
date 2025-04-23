using System.Security.Claims;
using CP2496H07Group1.Models;
using CP2496H07Group1.Services.Main;
using CP2496H07Group1.Services.Package;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace CP2496H07Group1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize("Admin")]
public class SecController : Controller
{
    private readonly IPackageService _packageService;
    private readonly IMainService _mainService;

    public SecController(IPackageService packageService,IMainService mainService)
    {
        _packageService = packageService;
        _mainService = mainService;
    }
    
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? keyword = null, string? status = null)
    {
        var userInsurances = _mainService.GetAllUserSec(page, pageSize, keyword, status);
        ViewBag.Keyword = keyword;
        ViewBag.Status = status; 
        return View(userInsurances);
    }
    
    [HttpGet]
    public async Task<IActionResult> Update(long id)
    {
        var insurance = await _packageService.GetUserInsuranceById(id);
        if (insurance == null) return NotFound();

        return View(insurance);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(long id, string status)
    {
        try
        {
            await _mainService.UpdateStatusSec(id, status);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    
    [HttpPost]
    public async Task<IActionResult> Update(UserInsurance model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = await _packageService.UpdateUserInsurance(model, model.Id);
        if (updated == null)
        {
            TempData["Error"] = "Update failed!";
            return View(model);
        }

        TempData["Success"] = "Update successfully!";
        return RedirectToAction("Index");
    }
    
    [HttpGet]
    public async Task<IActionResult> Delete(long id)
    {
        await _packageService.DeleteUserInsurance(id);
        return RedirectToAction("Index");
    }

}