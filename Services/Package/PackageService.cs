using System.Text.Json;
using System.Text.Json.Serialization;
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using X.PagedList;
using X.PagedList.Extensions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CP2496H07Group1.Services.Package;

public class PackageService : IPackageService
{
    private readonly AppDataContext _context;
    private readonly RedisService _redis;
    private static readonly Random _random = new Random();
    private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public PackageService(AppDataContext context, RedisService redis)
    {
        _context = context;
        _redis = redis;
    }

    public IPagedList<InsurancePackage> GetAllInsurancePackages(int page, int pageSize, string? keyword = null)
    {
        try
        {
            string cacheKey = $"insurance:Page:{page}:size:{pageSize}:keyword:{keyword ?? ""}";
            var cacheResult = _redis.Get(cacheKey);

            if (!string.IsNullOrEmpty(cacheResult))
            {
                var insurancePackages = JsonSerializer.Deserialize<List<InsurancePackage>>(cacheResult,
                    new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.Preserve
                    });
                if (insurancePackages != null)
                    return new StaticPagedList<InsurancePackage>(
                        insurancePackages.Skip((page - 1) * pageSize).Take(pageSize),
                        page,
                        pageSize,
                        insurancePackages.Count);
            }

            var query = _context.InsurancePackages.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Name.Contains(keyword) || p.Type.Contains(keyword));
            }

            var allInsurancePackages = query.OrderByDescending(i => i.Id).ToList();
            var result = new StaticPagedList<InsurancePackage>(
                allInsurancePackages.Skip((page - 1) * pageSize).Take(pageSize),
                page,
                pageSize,
                allInsurancePackages.Count);

            string json = JsonSerializer.Serialize(allInsurancePackages, new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve
            });
            _redis.Set(cacheKey, json, TimeSpan.FromDays(30));
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
    
    public async Task<UserInsurance?> GetInsuranceById(long insuranceId)
    {
        return await _context.UserInsurances
            .Include(ui => ui.Package)
            .FirstOrDefaultAsync(ui => ui.Id == insuranceId);
    }

    public async Task<InsurancePackage?> FindInsurancePackageById(long id)
    {
        try
        {
            var cacheKey = $"InsurancePackages_{id}";
            var cacheInsurance = _redis.Get(cacheKey);

            if (!string.IsNullOrEmpty(cacheInsurance))
            {
                return JsonConvert.DeserializeObject<InsurancePackage>(cacheInsurance);
            }

            var insurance = await _context.InsurancePackages
                .FirstOrDefaultAsync(i => i.Id == id);

            if (insurance != null)
            {
                var insuranceJson = JsonConvert.SerializeObject(insurance);
                _redis.Set(cacheKey, insuranceJson, TimeSpan.FromDays(30));
            }

            return insurance;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task<InsurancePackage?> AddInsurancePackage(InsurancePackage model)
    {
        try
        {
            await _context.InsurancePackages.AddAsync(model);
            await _context.SaveChangesAsync();

            await _redis.RemoveByPatternAsync("insurance:Page:*");
            var cacheKey = $"InsurancePackages_{model.Id}";
            var insuranceJson = JsonConvert.SerializeObject(model);
            _redis.Set(cacheKey, insuranceJson, TimeSpan.FromDays(30));

            return model;
        }
        catch (Exception ex)
        {
            throw new Exception("Error adding insurance package: " + ex.Message);
        }
    }

    public async Task<InsurancePackage?> UpdateInsurancePackage(InsurancePackage model, long id)
    {
        try
        {
            var existingInsurancePackage = await _context.InsurancePackages.FindAsync(id);
            if (existingInsurancePackage == null)
            {
                throw new Exception("Insurance package not found");
            }

            existingInsurancePackage.Name = model.Name;
            existingInsurancePackage.Type = model.Type;
            existingInsurancePackage.DurationDays = model.DurationDays;
            existingInsurancePackage.Description = model.Description;
            existingInsurancePackage.Price = model.Price;
            await _context.SaveChangesAsync();

            var cacheKey = $"InsurancePackages_{id}";
            _redis.Remove(cacheKey);
            await _redis.RemoveByPatternAsync("insurance:Page:*");
            var insuranceJson = JsonConvert.SerializeObject(existingInsurancePackage);
            _redis.Set(cacheKey, insuranceJson, TimeSpan.FromDays(30));

            return existingInsurancePackage;
        }
        catch (Exception ex)
        {
            throw new Exception("Error updating insurance package: " + ex.Message);
        }
    }
    public async Task DeleteInsurancePackage(long id)
    {
        try
        {
            var insurancePackage = await _context.InsurancePackages.FindAsync(id);
            if (insurancePackage != null)
            {
                _context.InsurancePackages.Remove(insurancePackage);
                await _context.SaveChangesAsync();

                var cacheKey = $"InsurancePackages_{id}";
                _redis.Remove(cacheKey);
                await _redis.RemoveByPatternAsync("insurance:Page:*");
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error deleting insurance package: " + ex.Message);
        }
    }
    public async Task<InsurancePackage?> PaymentByCard(long id, long userId)
    {
        try
        {
            var insurance = await _context.InsurancePackages.FindAsync(id);
            var key = $"PaymentCardInsurance:UserId:{userId}:InsuranceId:{insurance?.Id.ToString()}";
            var insuranceJson = JsonConvert.SerializeObject(insurance);

            _redis.Set(key, insuranceJson, TimeSpan.FromMinutes(10));
            return insurance;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error during payment by card: {ex.Message}");
        }
    }

    public async Task<InsurancePackage?> PaymentInsurance(long id, long userId, long accountId, int pin,
        string paymentType)
    {
        await using var transactionLibrary = await _context.Database.BeginTransactionAsync();
        try
        {
            string redisKey = $"PaymentCardInsurance:UserId:{userId}:InsuranceId:{id}";
            var cacheValue = _redis.Get(redisKey);
            if (cacheValue == null)
            {
                throw new Exception($"Error during payment by card: {cacheValue}");
            }

            var data = JsonConvert.DeserializeObject<InsurancePackage>(cacheValue.ToString());
            if (data == null)
                throw new Exception("Invalid insurance package data.");

            var account = await _context.Accounts
                .Include(c => c.CreditCard)
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null)
                throw new Exception("Account not found");

            if (pin != account.Pin)
                throw new Exception("Wrong PIN");

            
            if (data == null || data.Price <= 0 || data.DurationDays <= 0)
                throw new Exception("Invalid payment data");

            decimal price = data.Price;
            int durationDays = data.DurationDays;

            if (paymentType == "Card")
            {
                if (account.AccountType == "Credit Card")
                {
                    var creditCard = account.CreditCard;
                    if (creditCard == null || !creditCard.IsActive)
                        throw new Exception("No active credit card linked to the account.");

                    var now = DateTime.Now;

                    if (creditCard.CurrentDebt + creditCard.NewDebt + price > creditCard.CreditLimit)
                        throw new Exception("Insufficient credit limit.");

                    if (now > creditCard.StatementDate)
                    {
                        creditCard.NewDebt += price;
                    }
                    else
                    {
                        creditCard.CurrentDebt += price;
                    }
                }
                else
                {
                    if (account.Balance < price)
                        throw new Exception("Insufficient funds in the account.");

                    account.Balance -= price;
                }
            }
            else if (paymentType == "Sec")
            {
                if (account.AccountType == "Credit Card")
                {
                    var creditCard = account.CreditCard;
                    if (creditCard == null || !creditCard.IsActive)
                        throw new Exception("No active credit card linked to the account.");

                    if (creditCard.CurrentDebt + price > creditCard.CreditLimit)
                        throw new Exception("Insufficient funds in the account.");
                }
                else
                {
                    if (account.Balance < price)
                        throw new Exception("Insufficient funds in the account.");
                }
            }
            else
            {
                throw new Exception("Unsupported payment type.");
            }

            var transaction = new Transaction
            {
                FromAccountId = accountId,
                ToAccount = null,
                Amount = price,
                TransactionType = "BuyInsurance",
                Description = $"Buy Insurance #{id} by {paymentType} for user {userId}",
                TransactionDate = DateTime.Now,
                FromAccount = account,
                ToAccountId = null,
                Status = paymentType == "Sec" ? "Sec" : "Active"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            var userInsurance = new Models.UserInsurance
            {
                UserId = userId,
                PackageId = id,
                TransactionId = transaction.Id,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(durationDays),
                Status = paymentType == "Sec" ? "Pending" : "Active",
                InsuranceNumber = GenerateInsuranceNumber(),
                User = null,
                Package = null,
                Transaction = transaction,
            };

            await _context.UserInsurances.AddAsync(userInsurance);
            await _context.SaveChangesAsync();
            await transactionLibrary.CommitAsync();

            return data;
        }
        catch (Exception e)
        {
            await transactionLibrary.RollbackAsync();
            Console.WriteLine(e);
            throw;
        }
    }
    
    public static string GenerateInsuranceNumber()
    {
        char[] insuranceNumber = new char[15];
        for (int i = 0; i < 15; i++)
        {
            insuranceNumber[i] = Characters[_random.Next(Characters.Length)];
        }

        return $"{new string(insuranceNumber, 0, 4)}-" +
               $"{new string(insuranceNumber, 4, 4)}-" +
               $"{new string(insuranceNumber, 8, 4)}-" +
               $"{new string(insuranceNumber, 12, 3)}";
    }

    public async Task<List<UserInsurance>> GetUserInsurances(long userId)
    {
        return await _context.UserInsurances
            .Include(p => p.Package)
            .Include(u => u.User)
            .Where(p => p.UserId == userId && p.Status == "Active")
            .ToListAsync();
    }

    public async Task<List<UserInsurance>> GetUserSec(long userId)
    {
        return await _context.UserInsurances
            .Include(p => p.Package)
            .Include(u => u.User)
            .Include(t => t.Transaction)
            .Where(u => u.UserId == userId && u.Transaction.Status == "Sec" && u.Status == "Pending" ||
                        u.Status == "Cancel")
            .OrderByDescending(u => u.Id)
            .ToListAsync();
    }

    public async Task<UserInsurance?> GetUserInsurance(long userId)
    {
        var userInsurance = await _context.UserInsurances
            .Include(p => p.Package)
            .Include(u => u.User)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Status == "Active");
        if (userInsurance != null)
        {
            return userInsurance;
        }

        return null;
    }

    public async Task<UserInsurance?> UpdateSec(long userId, long id)
    {
        var userInsurance = await _context.UserInsurances
            .FirstOrDefaultAsync(u => u.Id == id && u.UserId == userId && u.Status == "Pending");

        if (userInsurance == null)
        {
            return null;
        }

        userInsurance.Status = "Cancel";
        await _context.SaveChangesAsync();
        return userInsurance;
    }


    public Task<IPagedList<UserInsurance>> GetAllSec(long userId, DateTime? fromDate, DateTime? toDate, int page,
        int pageSize)
    {
        var query = _context.UserInsurances
            .Include(p => p.Package)
            .Include(u => u.User)
            .Include(t => t.Transaction)
            .Where(u => u.UserId == userId && u.Transaction.Status == "Sec");

        if (fromDate.HasValue)
        {
            query = query.Where(u => u.Transaction.TransactionDate.Date >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(u => u.Transaction.TransactionDate.Date <= toDate.Value.Date);
        }

        return Task.FromResult(query.OrderByDescending(u => u.Transaction.TransactionDate)
            .ToPagedList(page, pageSize));
    }

    public async Task<UserInsurance?> GetUserInsuranceById(long id)
    {
        var insurance = await _context.UserInsurances
            .Include(p => p.Package)
            .Include(u => u.User)
            .Include(t => t.Transaction)
            .FirstOrDefaultAsync(i => i.Id == id);
        return insurance;
    }

    public async Task<UserInsurance?> UpdateUserInsurance(UserInsurance model, long id)
    {
        var userInsurance = await GetUserInsuranceById(id);
        if (userInsurance == null)
        {
            throw new Exception("User insurance not found");
        }

        userInsurance.StartDate = model.StartDate;
        userInsurance.EndDate = model.EndDate;
        userInsurance.Status = model.Status;
        await _context.SaveChangesAsync();
        return userInsurance;
    }

    public Task DeleteUserInsurance(long id)
    {
        var userInsurance = _context.UserInsurances.Find(id);
        if (userInsurance != null) _context.UserInsurances.Remove(userInsurance);
        _redis.Clear();
        return _context.SaveChangesAsync();
    }
}

 