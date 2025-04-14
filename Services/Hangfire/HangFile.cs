using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;

namespace CP2496H07Group1.Services.Hangfire;

public class HangFile : IHangFile
{
    private readonly AppDataContext _context;

    public HangFile(AppDataContext context)
    {
        _context = context;
    }
    
    // Han muc CreditLimit : 100000000
    // Lai xuat InterestRate 18 %
    
    // Ngay StatementDate:  1 4 2025
    // Ngay Due date : 21 4 2025
    
    // lai se xay ra khi qua ngay due date
    //Key test vi du: 
    // ddung 6 trieu tra tien vao ngay 21 thi ko bi tinh lai
    // dung 6 trieu ko tra tien vao ngay 21 ma tra tien vao ngay 25 thi la 4 ngay 
    // cach tinh : lai(18) / 100 / 365 0.0049
    // tien lai = So tien da dung * ngay lai * lai 
    // tra 6 cu va 11.883 tien lai
    // neu tra 3 trieu thi + 11.883 tien lai
    // con lai 3 trieu thi 3trieu + 5.918
    public async Task AutoPayCreditCardDebts()
    {
        var today = DateTime.Today;
        var creditCardsDue = await _context.CreditCards
            .Include(c=>c.Account)
            .Where(c=>c.IsActive && c.DueDate.Date == today && c.CurrentDebt > 0)
            .ToListAsync();
        
        // Get credit card
        // Cho chay vong for lap qua het, de tim ra card nao 

        foreach (var card in creditCardsDue)
        {
            var account = card.Account;
            if (account.Balance >= card.CurrentDebt)
            {
                account.Balance -= card.CurrentDebt;
                
                var transaction = new Transaction
                {
                    FromAccountId = account.Id,
                    Amount = card.CurrentDebt,
                    TransactionType = "CreditCardAutoPayment",
                    Description = $"Auto payment of {card.CurrentDebt} from account {account.AccountNumber} to credit card {card.CardNumber}",
                    TransactionDate = DateTime.Now,
                    FromAccount = account,
                    ToAccount = null,
                    ToAccountId = null,
                };
                await _context.Transactions.AddAsync(transaction);

                card.CurrentDebt = 0;
            
                // Thiet lap DueDate 1 thang 
                card.StatementDate = today;
                card.DueDate = today.AddDays(20);
            }
            else if (account.Balance > 0)
            {
                decimal partialPayment = account.Balance;
                card.CurrentDebt -= partialPayment;
                account.Balance = 0;
                
                var transaction = new Transaction
                {
                    FromAccountId = account.Id,
                    Amount = partialPayment,
                    TransactionType = "CreditCardPartialPayment",
                    Description = $"Partial payment of {partialPayment} from account {account.AccountNumber} to credit card {card.CardNumber}",
                    TransactionDate = DateTime.Now,
                    FromAccount = account,
                    ToAccountId = null,
                    ToAccount = null,
                };
                await _context.Transactions.AddAsync(transaction);

                if (card.DueDate < today)
                {
                    var daysLate = (today - card.DueDate).Days;
                    decimal interestRatePerDay = card.InterestRate / 100 / 365;
                    decimal interest = card.CurrentDebt * interestRatePerDay * daysLate;

                    card.CurrentDebt += interest;
                }
            }
            else
            {
                // Khong du tien 
                // card.IsActive = false; thich thi khoa the
                card.InterestRate += 1.5m; // 
    
                var failedTransaction = new Transaction
                {
                    FromAccountId = account.Id,
                    Amount = card.CurrentDebt,
                    TransactionType = "CreditCardAutoPaymentFailed",
                    Description = $"Auto payment failed due to insufficient funds for card {card.CardNumber}",
                    TransactionDate = DateTime.Now,
                    FromAccount = account,
                    ToAccountId = null,
                    ToAccount = null,
                };

                await _context.Transactions.AddAsync(failedTransaction);

                // Gửi email cảnh báo nếu muốn
            }
        }
        await _context.SaveChangesAsync();
    }
}