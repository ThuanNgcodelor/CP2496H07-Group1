using CP2496H07Group1.Models;

namespace CP2496H07Group1.Services.Account;

public interface IAccountService
{
    public Task<List<Models.Account>> GetAccounts(long userId);
    public Task<Models.Account?> GetAccountDetails(long userId,long accountId);
    Task<string> SendMailCreateAccount(long userId,string accountType,int pin);
    Task<Models.Account?> VerifyOtpAndCommitCreateAccount(long userId, string otp);
}