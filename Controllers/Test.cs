using CP2496H07Group1.Services.Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace CP2496H07Group1.Controllers;

public class Test :Controller
{
    private readonly IHangFile _hangFile;

    public Test(IHangFile hangFile)
    {
        _hangFile = hangFile;
    }

    public async Task<IActionResult> TestAutoPayment()
    {
        await _hangFile.AutoPayCreditCardDebts();
        return Content("AutoPayCreditCardDebts executed successfully!");
    }
}