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
                .Include(c => c.CreditCard)
                .Where(a => a.UserId == userId && a.Status == "Active")
                .ToListAsync();
        }
        catch
        {
            throw new ArgumentException("Invalid or expired OTP.");
        }
    }

    public Task<Models.Account?> GetAccountDetails(long userId, long accountId, string pin)
    {
        try
        {
            return Task.FromResult(_context.Accounts
                .Include(c => c.CreditCard)
                .FirstOrDefault(a => a.UserId == userId && a.Id == accountId && a.Pin == int.Parse(pin)));
        }
        catch
        {
            throw new ArgumentException("Invalid or expired OTP.");
        }
    }
    
    public Task<Models.Account?> GetAccountDetails(long userId, long accountId)
    {
        try
        {
            return Task.FromResult(_context.Accounts
                .Include(c => c.CreditCard)
                .FirstOrDefault(a => a.UserId == userId && a.Id == accountId ));
        }
        catch
        {
            throw new ArgumentException("Invalid or expired OTP.");
        }
    }

    public async Task<string> SendMailCreateAccount(long userId, string accountType, int pin)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new ApplicationException($"User with id {userId} not found");

        var otp = new Random().Next(100000, 999999).ToString();
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

    public async Task<string> ChangePin(long userId, long accountId)
    {   
        var account = _context.Accounts.
            Include(a => a.User)
            .FirstOrDefault(a => a.UserId == userId && a.Id == accountId);
        
        if (account == null)
            throw new ApplicationException("Invalid or expired OTP.");
        string redisKey = $"change_pin_{userId}_{accountId}";
        var pin = new Random().Next(100000, 999999).ToString();
        _redis.Set(redisKey, pin, TimeSpan.FromMinutes(10));
        var email = (string)account.User.Email;
        var emailContent = ChangePinCard(email, pin);
        await _emailService.Send(email, "Change Pin", emailContent);
        return "New OTP has been sent to your email.";
    }

    public async Task<Models.Account?> ConfirmChangePin(long userId, long accountId, string pin, string otp)
    {
        string redisKey = $"change_pin_{userId}_{accountId}";
        var data = _redis.Get(redisKey);
        if (string.IsNullOrEmpty(data) || data != otp) return null;
        
        var account = await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Id == accountId);

        if (account == null) return null;
        if(!int.TryParse(pin, out int newPin)) return null;
        account.Pin = newPin;
        await _context.SaveChangesAsync();
        _redis.Remove(redisKey);
        return account;
    }

    public async Task<string> SendMailResendCreateOtp(long userId)
    {
        string redisKey = $"create_account_otp_{userId}";

        var data = _redis.Get(redisKey);
        if (string.IsNullOrEmpty(data))
        {
            throw new ApplicationException("No pending card creation request found. Please start the process again.");
        }

        var parsedData = JsonConvert.DeserializeObject<dynamic>(data);
        var tempAccount = parsedData?.tempAccount;
        if (tempAccount == null)
        {
            throw new ApplicationException("Invalid OTP data.");
        }

        var otp = new Random().Next(100000, 999999).ToString();

        var updatedData = new
        {
            otp,
            tempAccount
        };

        _redis.Set(redisKey, JsonConvert.SerializeObject(updatedData), TimeSpan.FromMinutes(10));

        var email = (string)tempAccount.Email;
        var emailContent = CreateOtpEmailCreateAccount(email, otp);

        await _emailService.Send(email, "Resend OTP - Create Card", emailContent);

        return "New OTP has been sent to your email.";
    }

    public async Task<bool> PayFullDebtAsync(long userId)
    {
        var user = await _context.Users.FindAsync(userId);
        var account = _context.Accounts
            .Include(a => a.CreditCard)
            .FirstOrDefault(a => a.UserId == userId && a.AccountType == "Credit Card");

        if (account == null || account.CreditCard == null || !account.CreditCard.IsActive)
            return false;

        var debt = account.CreditCard.CurrentDebt;
        var now = DateTime.Now.Date;
        var dueDate = account.CreditCard.DueDate.Date;

        if (now > dueDate)
        {
            int overdueDays = (now - dueDate).Days;
            decimal interestRate = account.CreditCard.InterestRate / 100;
            decimal penalty = debt * interestRate * overdueDays;
            debt += penalty;
            account.CreditCard.CurrentDebt += penalty;
        }

        if (account.Balance < debt)
            return false;

        account.Balance -= debt;
        account.CreditCard.CurrentDebt = 0;


        var emailBody = SendMailPayment(user.Email, debt, account.Balance);
        await _emailService.Send(user.Email, "Payment Confirmation", emailBody);

        _context.Update(account);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PayPartialDebtAsync(long userId, decimal amount)
    {
        var user = await _context.Users.FindAsync(userId);
        var account = _context.Accounts
            .Include(a => a.CreditCard)
            .FirstOrDefault(a => a.UserId == userId && a.AccountType == "Credit Card");

        if (account == null || account.CreditCard == null || !account.CreditCard.IsActive)
            return false;

        var now = DateTime.Now.Date;
        var dueDate = account.CreditCard.DueDate.Date;

        decimal penalty = 0;

        // Nếu quá hạn thanh toán thì cộng lãi vào CurrentDebt
        if (now > dueDate)
        {
            int overdueDays = (now - dueDate).Days;
            decimal interestRate = account.CreditCard.InterestRate / 100;
            penalty = account.CreditCard.CurrentDebt * interestRate * overdueDays;

            account.CreditCard.CurrentDebt += penalty;
        }

        if (account.Balance < amount || amount <= 0 || amount > account.CreditCard.CurrentDebt)
            return false;

        account.Balance -= amount;
        account.CreditCard.CurrentDebt -= amount;

        string emailBody = SendMailPayment(user.Email, amount, account.Balance);
        if (penalty > 0)
        {
            emailBody += $"\n\n⚠️ Note: You were { (now - dueDate).Days } days overdue.";
            emailBody += $"\nInterest charged: {penalty:C}.";
        }

        await _emailService.Send(user.Email, "Partial Payment Confirmation", emailBody);

        _context.Update(account);
        await _context.SaveChangesAsync();
        return true;
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
            Status = "Active",
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

        if (accountType == "Credit Card")
        {
            var creditCard = new CreditCard
            {
                AccountId = account.Id,
                CardNumber = GenerateCreditCardNumber(),
                CreditLimit = 2000, // hạn mức mặc định 1 ngan do
                CurrentDebt = 0,
                InterestRate = 18, // 18%/năm
                BillingCycleStart = DateTime.Now,
                StatementDate = DateTime.Now.AddDays(30),
                DueDate = DateTime.Now.AddDays(35), 
                Cvv = new Random().Next(100, 1000),
                ExpirationDate = DateTime.Now.AddYears(3),
                IsActive = true,
                Account = account
            };
            await _context.CreditCards.AddAsync(creditCard);
        }

         await _redis.RemoveByPatternAsync("Card:Page:*");
            var cacheKey = $"Card{account.Id}";
            var accountJson = JsonConvert.SerializeObject(account, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            _redis.Set(cacheKey, accountJson, TimeSpan.FromDays(30));

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
        var random = new Random();
        return string.Concat(Enumerable.Range(0, 12).Select(_ => random.Next(0, 10).ToString()));
    }


    private static string CreateOtpEmailCreateAccount(string email, string otp)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Confirm OTP to Create Card</h2>
                <p>Your OTP is <strong>{otp}</strong></p>
                <p>This code is valid for 10 minutes.</p>
                <p>TeckBank</p>

            </body>
            </html>";
    }

    private static string ChangePinCard(string email, string otp)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Confirm OTP to Change Pin</h2>
                <p>Your OTP is <strong>{otp}</strong></p>
                <p>This code is valid for 10 minutes.</p>
                <p>TeckBank</p>

            </body>
            </html>";
    }
    
    private static string SendMailPayment(string email, decimal amountPaid, decimal newBalance)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif;'>
            <h2>Payment Confirmation</h2>
            <p>Dear {email},</p>
            <p>We have received your payment of <strong>{amountPaid:C}</strong>.</p>
            <p>Your new account balance is <strong>{newBalance:C}</strong>.</p>
            <p>This payment was successfully processed on {DateTime.Now:MMMM dd, yyyy hh:mm tt}.</p>
            <br/>
            <p>Thank you for using our services!</p>
                            <p>TeckBank</p>
        </body>
        </html>";
    }
}