using CP2496H07Group1.Dtos;
using CP2496H07Group1.Models;
using CP2496H07Group1.Services.Main;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize("Admin")]
public class CardController : Controller
{
    private readonly IMainService _mainService;

    public CardController(IMainService mainService)
    {
        _mainService = mainService;
    }
    
    public Task<IActionResult> Index(int? page, string? keyword)
    {
        int pageSize = 5;
        int pageNumber = page ?? 1;
        var accounts =  _mainService.GetAllAccounts(pageNumber, pageSize, keyword);
        ViewBag.Keyword = keyword;
        return Task.FromResult<IActionResult>(View(accounts));
    }
    
    [HttpGet]
    public async Task<IActionResult> Update(long id)
    {
        var account = await _mainService.FindAccountById(id);
        return View(account);
    }


    [HttpPost]
    public async Task<IActionResult> Update(Models.Account model)
    {
        await _mainService.UpdateAccount(model, model.Id);
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Delete(long id)
    {
        await _mainService.DeleteAccount(id);
        return RedirectToAction("Index");
    }

    [HttpGet]
    [Route("Admin/Card/ViewCreditCard/{id}")]
    public async Task<IActionResult> ViewCreditCard(long id)
    {
        var creditCard = await _mainService.FindCreditCardById(id);
        return Json(creditCard);
    }
    
    [HttpPost]
    [Route("Admin/Card/LockCreditCard/{id}")]
    public async Task<IActionResult> LockCreditCard(long id)
    {
        var creditCard = await _mainService.LockCreditCardById(id);
        if (creditCard == null)
        {
            return NotFound();
        }
        return Json(creditCard);
    }
    
    [HttpPost]
    [Route("Admin/Card/UnlockCreditCard/{id}")]
    public async Task<IActionResult> UnlockCreditCard(long id)
    {
        var creditCard = await _mainService.UnlockCreditCardById(id);
        if (creditCard == null)
        {
            return NotFound();
        }
        return Json(creditCard);
    }
}