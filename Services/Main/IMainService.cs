using CP2496H07Group1.Dtos;
using CP2496H07Group1.Models;
using X.PagedList;

namespace CP2496H07Group1.Services.Main;

public interface IMainService
{
    IPagedList<Admin> GetAllAdmins(int page, int pageSize, string? keyword);
    Task<Admin?> FindAdminById(long id);
    Task<Admin?> AddAdmin(Admin model);
    Task<Admin?> UpdateAdmin(Admin model, long id, string? newPassword);
    Task DeleteAdmin(long id);
    
    IPagedList<User> GetAllUsers(int page, int pageSize, string? keyword);
    Task<User?> FindUserById(long id);
    Task<User?> AddUser(User model);
    Task<User?> UpdateUser(User model,long id);
    Task DeleteUser(long id);
    
    IPagedList<Models.Account> GetAllAccounts(int page, int pageSize, string? keyword);
    Task<Models.Account?> FindAccountById(long id);
    Task<CreditCardDto?> FindCreditCardById(long id);
    Task<CreditCardDto?> LockCreditCardById(long id);
    Task<CreditCardDto?> UnlockCreditCardById(long id);
    Task<Models.Account?> AddAccount(Models.Account model);
    Task<Models.Account?> UpdateAccount(Models.Account model,long id);
    Task DeleteAccount(long id);
    
    IPagedList<UserInsurance> GetAllUserSec(int page, int pageSize, string? keyword, string? status);
    Task<UserInsurance?> UpdateStatusSec(long id,string status);
    
}