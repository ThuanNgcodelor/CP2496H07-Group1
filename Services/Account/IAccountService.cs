using CP2496H07Group1.Models;

namespace CP2496H07Group1.Services.Account;

public interface IAccountService 
{
    Task<Models.Account> CreateAccount(long userId,string accountType,int pin);
}