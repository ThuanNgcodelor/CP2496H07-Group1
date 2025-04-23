using CP2496H07Group1.Services.Main;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize("Admin")]
public class UserController : Controller
{
    private readonly IMainService _mainService;

    public UserController(IMainService mainService)
    {
        _mainService = mainService;
    }
    
    public Task<IActionResult> Index(int? page, string? keyword)
    {
        int pageSize = 5;
        int pageNumber = page ?? 1;
        var accounts =  _mainService.GetAllUsers(pageNumber, pageSize, keyword);
        ViewBag.Keyword = keyword;
        return Task.FromResult<IActionResult>(View(accounts));
    }
    
    [HttpGet]
    public async Task<IActionResult> Update(long id)
    {
        var user = await _mainService.FindUserById(id);
        return View(user);
    }
    
    [HttpPost]
    public async Task<IActionResult> Update(Models.User model)
    {
        await _mainService.UpdateUser(model, model.Id);
        return RedirectToAction("Index");
    }
    
    public async Task<IActionResult> Delete(long id)
    {
        await _mainService.DeleteUser(id);
        return RedirectToAction("Index");
    }
    
    [HttpGet]
    public IActionResult Add()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> Add(Models.User model)
    {
        if (ModelState.IsValid)
        {
            await _mainService.AddUser(model);
            return RedirectToAction("Index");
        }
        return View(model);
    }
}