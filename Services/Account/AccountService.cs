using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;

namespace CP2496H07Group1.Services.Account;

public class AccountService : IAccountService
{
    private readonly AppDataContext _context;

    public AccountService(AppDataContext context)
    {
        _context = context;
    }
    
    public async Task<Models.Account> CreateAccount(long userId, string accountType, int pin)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found.");

            var accountNumber = GenerateAccountNumber();
            var account = new Models.Account
            {
                UserId = userId,
                AccountNumber = accountNumber,
                AccountType = accountType,
                Balance = 0,
                Pin = pin,
                CreatedAt = DateTime.Now,
                User = user,
                VipId = null,
                Point = 0,
                TransactionsFrom = new List<Transaction>(),
                TransactionsTo = new List<Transaction>()
            };
            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();


            var transaction = new Transaction
            {
                FromAccountId = account.AccountId,
                ToAccount = null,
                Amount = 0,
                TransactionType = "AccountCreation",
                Description = $"Created account #{accountNumber} for user {userId}",
                TransactionDate = DateTime.Now,
                FromAccount = account,
                ToAccountId = null,
            };
            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            
            return account;
        }catch
        {
            throw new ArgumentException("Invalid or expired OTP.");
        }
    }
    
    private string GenerateAccountNumber()
    {
        var random = new Random();
        return string.Concat(Enumerable.Range(0, 12).Select(_ => random.Next(0, 10).ToString()));
    }
}