using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Email;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CP2496H07Group1.Services.Account;

public class AccountService : IAccountService
{
    private readonly AppDataContext _context;
    private readonly IEmailService _emailService;
    private readonly RedisService _redis;


    public AccountService(AppDataContext context, IEmailService emailService, RedisService redis)
    {
        _context = context;
        _emailService = emailService;
        _redis = redis;
    }


    public async Task<List<Models.Account>> GetAccounts(long userId)
    {
        try
        {
            return await _context.Accounts
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }catch
        {
            throw new ArgumentException("Invalid or expired OTP.");
        }
    }

    public Task<Models.Account?> GetAccountDetails(long userId, long accountId)
    {
        try
        {
            return Task.FromResult(_context.Accounts
                .FirstOrDefault(a => a != null && a.UserId == userId && a.Id == accountId));
        }
        catch
        {
            throw new ArgumentException("Invalid or expired OTP.");
        }
    }

    public async Task<string> SendMailCreateAccount(long userId, string accountType, int pin)
    {
        var user = await _context.Users.FindAsync(userId);
        if(user == null)
            throw new ApplicationException($"User with id {userId} not found");

        var otp = new Random().Next(100000,999999).ToString();
        var tempAccount = new
        {
            userId = user.Id,
            AccountType = accountType,
            Pin = pin,
            Email = user.Email
        };

        string redisKey = $"create_account_otp_{userId}";

        var combinedData = new
        {
            otp,
            tempAccount
        };

        _redis.Set(redisKey, JsonConvert.SerializeObject(combinedData), TimeSpan.FromMinutes(10));

        var emailSend = CreateOtpEmailCreateAccount(user.Email, otp);
        await _emailService.Send(user.Email, "Create Card", emailSend);

        return "OTP sent to your email.";
    }

    public async Task<Models.Account?> VerifyOtpAndCommitCreateAccount(long userId, string otpInput)
    {
        string redisKey = $"create_account_otp_{userId}";
        var cacheValue = _redis.Get(redisKey);

        var data = JsonConvert.DeserializeObject<dynamic>(cacheValue.ToString());
        string otpStored = data.otp.ToString();

        if (otpStored != otpInput)
            return null;

        int pin = (int)data.tempAccount.Pin;
        string accountType = (string)data.tempAccount.AccountType;

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return null;

        _redis.Remove(redisKey);

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
            FromAccountId = account.Id,
            ToAccount = null,
            Amount = 0,
            TransactionType = "AccountCreation",
            Description = $"Created account #{accountNumber} for user {userId}",
            TransactionDate = DateTime.Now,
            FromAccount = account,
            ToAccountId = null,
        };

        await _context.Transactions.AddAsync(transaction);

        if (accountType == "CreditCard")
        {
            var creditCard = new CreditCard
            {
                AccountId = account.Id,
                CardNumber = GenerateCreditCardNumber(),
                CreditLimit = 20000000, // hạn mức mặc định 20 triệu
                CurrentDebt = 0,
                InterestRate = 18, // 18%/năm
                StatementDate = DateTime.Now, // ngày sao kê hôm nay
                DueDate = DateTime.Now.AddDays(20), // hạn thanh toán sau 20 ngày
                IsActive = true,
                Account = account
            };
            await  _context.CreditCards.AddAsync(creditCard);
        }
        
        await _context.SaveChangesAsync();

        return account;
    }
    


    private string GenerateAccountNumber()
    {
        var random = new Random();
        return string.Concat(Enumerable.Range(0, 12).Select(_ => random.Next(0, 10).ToString()));
    }
    
    private string GenerateCreditCardNumber()
    {
        Random rand = new();
        return $"{rand.Next(1000, 9999)}-{rand.Next(1000, 9999)}-{rand.Next(1000, 9999)}-{rand.Next(1000, 9999)}";
    }

    
    private static string CreateOtpEmailCreateAccount(string email, string otp)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Confirm OTP to Create Card</h2>
                <p>Your OTP is <strong>{otp}</strong></p>
                <p>This code is valid for 10 minutes.</p>
            </body>
            </html>";
    }
}