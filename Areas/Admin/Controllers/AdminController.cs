using CP2496H07Group1.Services.Main;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Areas.Admin.Controllers;

[Area("Admin")]
public class AdminController : Controller
{
    private readonly IMainService _mainService;

    public AdminController(IMainService mainService)
    {
        _mainService = mainService;
    }

    public Task<IActionResult> Index(int? page, string? keyword)
    {
        int pageSize = 5;
        int pageNumber = page ?? 1;
        var admins = _mainService.GetAllAdmins(pageNumber, pageSize, keyword);
        ViewBag.Keyword = keyword;
        return Task.FromResult<IActionResult>(View(admins));
    }

    [HttpGet]
    public async Task<IActionResult> Update(long id)
    {
        var admin = await _mainService.FindAdminById(id);
        return View(admin);
    }

    [HttpPost]
    public async Task<IActionResult> Update(Models.Admin model)
    {
        var newPassword = Request.Form["NewPassword"];

        await _mainService.UpdateAdmin(model, model.Id, newPassword);
        return RedirectToAction("Index");
    }


    [HttpGet]
    public IActionResult Add()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Add(Models.Admin model)
    {
        if (ModelState.IsValid)
        {
            await _mainService.AddAdmin(model);
            return RedirectToAction("Index");
        }

        return View(model);
    }


    public async Task<IActionResult> Delete(long id)
    {
        await _mainService.DeleteAdmin(id);
        return RedirectToAction("Index");
    }
}