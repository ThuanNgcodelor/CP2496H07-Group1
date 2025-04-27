using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CP2496H07Group1.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Dtos;
namespace CP2496H07Group1.Controllers;

public class VipController : Controller
{
    private readonly AppDataContext _context;

    public VipController(AppDataContext appDataContext)
    {
        _context = appDataContext;
    }

    // Hiển thị danh sách VIP với phân trang và tìm kiếm
    public async Task<IActionResult> Index(int page = 1, string keyword = "")
    {
        int pageSize = 6;
        var query = _context.Vips.AsQueryable();

        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(v => v.TypeVip.ToString().Contains(keyword));
        }

        var totalItems = await query.CountAsync();
        var vips = await query
            .OrderBy(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new VipViewModel
        {
            Vips = vips,
            PageNumber = page,
            PageCount = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
      

        ViewBag.Keyword = keyword;
        return View(model);
    }

    // Lấy thông tin VIP và danh sách tài khoản để hiển thị trong modal
    [HttpGet]
    public async Task<IActionResult> BuyNow(long id)
    {
        Console.WriteLine($"BuyNow called with id: {id}");
        if (id <= 0)
        {
            Console.WriteLine("Invalid vipId: id must be greater than 0");
            return Json(new { success = false, message = "Vip package ID is invalid." });
        }

        var vip = await _context.Vips.FindAsync(id);
        if (vip == null)
        {
            Console.WriteLine($"VIP not found for id: {id}");
            return Json(new { success = false, message = "VIP package does not exist." });
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr))
        {
            Console.WriteLine("UserId is null or empty.");
            return Json(new { success = false, message = "Unknown user." });
        }

        if (!long.TryParse(userIdStr, out long userId))
        {
            Console.WriteLine($"Invalid userId format: {userIdStr}");
            return Json(new { success = false, message = "User information is invalid." });
        }

        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => new
            {
                id = a.Id, // Lowercase property names
                accountNumber = a.AccountNumber,
                balance = a.Balance.ToString("N2") // Numeric format (e.g., "1000.00")
            })
            .ToListAsync();
        Console.WriteLine($"UserId: {userId}");
  

        // Retrieve the discount codes associated with the user's accounts
        var discountCodes = await (
            from ac in _context.Accounts
            join ad in _context.AccountDiscounts on ac.Id equals ad.AccountId
            join dc in _context.DiscountCodes on ad.DiscountId equals dc.Id
            where ac.UserId == userId
            select new DiscountcodeViewModel
            {
                Id = dc.Id,
                DiscountCodes = dc.DiscountCodes,
                Points = dc.Points,
                Percent = dc.Percent,
                LongDate = dc.LongDate
            }).Distinct().ToListAsync();

        Console.WriteLine($"Found {discountCodes.Count} discount codes.");

        Console.WriteLine($"Found {accounts.Count} accounts for userId: {userId}");
        if (accounts.Count == 0)
        {
            Console.WriteLine($"No accounts found for userId: {userId}");
            return Json(new { success = false, message = "No account found for this user." });
        }

        return Json(new
        {
            success = true,
            id = vip.Id,
            typeVip = vip.TypeVip,
            price = vip.Price,
            moneyBack = vip.MoneyBack,
            noPick = vip.NoPick,
            accounts = accounts,
            discountCodes = discountCodes // Pass the discount codes here
        });
    }

    // Xử lý mua VIP
    [HttpPost]
    public async Task<IActionResult> PurchaseVip([FromBody] PurchaseVipRequest request)
    {
        try
        {
            Console.WriteLine($"PurchaseVip called with vipId: {request.VipId}, accountId: {request.AccountId}, pin: {request.Pin}");
            if (request.VipId <= 0 || request.AccountId <= 0)
            {
                Console.WriteLine("Invalid vipId or accountId");
                return Json(new { success = false, message = "Vip package ID or invalid account." });
            }
    
            if (!User.Identity.IsAuthenticated)
            {
                Console.WriteLine("User is not authenticated.");
                return Json(new { success = false, message = "Users have not logged in." });
            }
    
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                Console.WriteLine("Invalid userId.");
                return Json(new { success = false, message = "Can't get user information." });
            }
    
            var account = await _context.Accounts
                .Include(a => a.Vip)
                .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId);
            if (account == null)
            {
                Console.WriteLine($"Account not found for accountId: {request.AccountId}, userId: {userId}");
                return Json(new { success = false, message = "The account does not exist or does not belong to you." });
            }
    
            var vip = await _context.Vips.FindAsync(request.VipId);
            if (vip == null)
            {
                Console.WriteLine($"VIP not found for vipId: {request.VipId}");
                return Json(new { success = false, message = "VIP package does not exist." });
            }
    
            if (account.Balance < vip.Price)
            {
                Console.WriteLine($"Insufficient balance for accountId: {account.Id}, balance: {account.Balance}, price: {vip.Price}");
                return Json(new { success = false, message = "The balance is not enough to buy VIP package." });
            }
    
            if (account.Pin != request.Pin)
            {
                Console.WriteLine($"Incorrect PIN for accountId: {account.Id}");
                return Json(new { success = false, message = "The PIN is not correct." });
            }
    
            // Kiểm tra thứ tự nâng cấp VIP
            if (account.Vip == null)
            {
                if (vip.TypeVip != 1)
                {
                    Console.WriteLine($"Users do not have VIP, can only buy VIP package level 1.");
                    return Json(new { success = false, message = "You need to buy a first -class VIP package first." });
                }
            }
            else
            {
                if (account.Vip.TypeVip + 1 != vip.TypeVip)
                {
                    Console.WriteLine($"Can't buy a level VIP package {vip.TypeVip}Because you are at level {account.Vip.TypeVip}.");
                    return Json(new { success = false, message = "Please upgrade VIP in order." });
                }
            }
            decimal discountAmount = 0;
            decimal finalPrice = vip.Price;
    
            if (request.DiscountId != null && request.DiscountId > 0)
            {
                var discount = await _context.DiscountCodes
                    .Include(d => d.AccountDiscounts)
                    .FirstOrDefaultAsync(dc => dc.Id == request.DiscountId);
    
                if (discount == null || !discount.AccountDiscounts.Any(ad => ad.AccountId == request.AccountId))
                {
                    return Json(new { success = false, message = "Coupon code is invalid or does not belong to this account." });
                }
    
                // Áp dụng giảm giá
                discountAmount = (vip.Price * discount.Percent) / 100;
                finalPrice -= discountAmount;
    
                var accountDiscount = await _context.AccountDiscounts
                    .FirstOrDefaultAsync(ad => ad.AccountId == request.AccountId && ad.DiscountId == request.DiscountId);
    
                if (accountDiscount != null)
                {
                    _context.AccountDiscounts.Remove(accountDiscount);
                    await _context.SaveChangesAsync();
                }
            }
    
            // ❌ Đừng trừ vip.Price – dùng finalPrice thay vì vip.Price
            account.Balance -= finalPrice;
            account.VipId = vip.Id;
    
            var transaction = new Transaction
            {
                FromAccountId = account.Id,
                ToAccountId = null,
                Amount = finalPrice,
                TransactionType = "PURCHASE_VIP",
                Description = $"Buy package VIP {vip.TypeVip}" + (discountAmount > 0 ? $" (discounted {discountAmount})" : ""),
                TransactionDate = DateTime.Now,
                FromAccount = account,
                ToAccount = null,
                VipId = vip.Id,
                DiscountCodeId = request.DiscountId
            };
    
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
    
            Console.WriteLine($"Purchase successful for vipId: {request.VipId}, accountId: {account.Id}");
            return Json(new { success = true, message = "Buy Vip package successfully!" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in PurchaseVip: {ex.Message}\nStackTrace: {ex.StackTrace}");
            return Json(new { success = false, message = "Error occurred during the process of buying a VIP package." });
        }
    }
}