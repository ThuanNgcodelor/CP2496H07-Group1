using CP2496H07Group1.Models;
using X.PagedList;

namespace CP2496H07Group1.Services.Package;

public interface IPackageService
{
    IPagedList<InsurancePackage> GetAllInsurancePackages(int page, int pageSize, string? keyword = null);
    Task<InsurancePackage?> FindInsurancePackageById(long id);
    Task<InsurancePackage?> AddInsurancePackage(InsurancePackage model);
    Task<InsurancePackage?> UpdateInsurancePackage(InsurancePackage model,long id);
    Task DeleteInsurancePackage(long id);
    
    Task<InsurancePackage?> PaymentByCard(long id,long userId);
}