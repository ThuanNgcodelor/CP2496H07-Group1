using CP2496H07Group1.Models;

namespace CP2496H07Group1.Services.Account;

public interface IAccountService 
{
    Task<Models.Account?> GetAccount(long userId);
    Task<string> SendMailCreateAccount(long userId,string accountType,int pin);
    Task<Models.Account?> VerifyOtpAndCommitCreateAccount(long userId, string otp);
}