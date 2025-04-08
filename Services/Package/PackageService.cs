using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using X.PagedList;
using X.PagedList.Extensions;

namespace CP2496H07Group1.Services.Package;

public class PackageService : IPackageService
{
    private readonly AppDataContext _context;
    private readonly RedisService _redis;

    public PackageService(AppDataContext context, RedisService redis)
    {
        _context = context;
        _redis = redis;
    }

    public IPagedList<InsurancePackage> GetAllInsurancePackages(int page, int pageSize , string? keyword = null)
    {
        try
        {
            var query = _context.InsurancePackages.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Name.Contains(keyword));
            }

            return query.OrderByDescending(p => p.Name)
                .ToPagedList(page, pageSize);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
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
                .FirstOrDefaultAsync(i=>i.Id == id);

            if (insurance != null)
            {
                var insuranceJson = JsonConvert.SerializeObject(insurance);
                _redis.Set(cacheKey, insuranceJson,TimeSpan.FromDays(30));
            }
            return insurance;
        }catch(Exception ex){
            throw new Exception(ex.Message);
        }
    }

    public async Task<InsurancePackage?> AddInsurancePackage(InsurancePackage model)
    {
        try
        {
            await _context.InsurancePackages.AddAsync(model);
            await _context.SaveChangesAsync();
            return model;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task<InsurancePackage?> UpdateInsurancePackage(InsurancePackage model, long id)
    {
        try
        {
            var existingInsurancePackage = await _context.InsurancePackages.FindAsync(id);
            if (existingInsurancePackage == null)
            {
                throw new Exception();
            }

            existingInsurancePackage.Name = model.Name;
            existingInsurancePackage.Type = model.Type;
            existingInsurancePackage.Description = model.Description;
            existingInsurancePackage.Price = model.Price;
            await _context.SaveChangesAsync();
            return existingInsurancePackage;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public Task DeleteInsurancePackage(long id)
    {
        var insurancePackage = _context.InsurancePackages.Find(id);
        if (insurancePackage != null) _context.InsurancePackages.Remove(insurancePackage);
        return _context.SaveChangesAsync();
    }
}