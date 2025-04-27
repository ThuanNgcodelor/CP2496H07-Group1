using CP2496H07Group1.Models;

namespace CP2496H07Group1.Services.Account;

public interface IAccountService
{
    public Task<List<Models.Account>> GetAccounts(long userId);
    public Task<Models.Account?> GetAccountDetails(long userId,long accountId,string pin);
    public Task<Models.Account?> GetAccountDetails(long userId,long accountId);

    Task<string> SendMailCreateAccount(long userId,string accountType,int pin);
    Task<string> ChangePin(long userId,long accountId);
    public Task<Models.Account?> ConfirmChangePin(long userId,long accountId,string pin,string otp);
    Task<Models.Account?> VerifyOtpAndCommitCreateAccount(long userId, string otp);
    Task<string> SendMailResendCreateOtp(long userId);
    Task<bool> PayFullDebtAsync(long userId);
    Task<bool> PayPartialDebtAsync(long userId, decimal amount);
}