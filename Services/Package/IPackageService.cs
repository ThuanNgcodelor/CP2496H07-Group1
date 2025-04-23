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
    Task<InsurancePackage?> PaymentInsurance(long id, long userId, long accountId, int pin,string paymentType);
    Task<List<UserInsurance>> GetUserInsurances(long userId);
    Task<List<UserInsurance>> GetUserSec(long userId);
    Task<UserInsurance?> GetUserInsurance(long userId);

    Task<IPagedList<UserInsurance>>GetAllSec(long userId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    
    Task<UserInsurance?> GetUserInsuranceById(long id);
    Task<UserInsurance?> UpdateUserInsurance(UserInsurance model,long id);
    Task<UserInsurance?> UpdateSec(long userId, long id);
    Task DeleteUserInsurance(long id);
    Task<UserInsurance?> GetInsuranceById(long insuranceId);

    // Task<InsurancePackage?> PaymentByCardInsurance(long id, long userId, long accountId, int pin);
    // Task<InsurancePackage?> PaymentBySecInsurance(long id, long userId, long accountId, int pin);
}