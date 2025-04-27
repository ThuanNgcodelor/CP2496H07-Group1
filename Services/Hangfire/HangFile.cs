using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Email;
using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;

namespace CP2496H07Group1.Services.Hangfire;

public class HangFile : IHangFile
{
    private readonly AppDataContext _context;
    private readonly IEmailService _emailService;

    public HangFile(AppDataContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task ProcessMonthlyPayments()
    {
        var loans = await _context.Loans
            .Include(l => l.PaymentSchedules)
            .Where(l => l.OweMoney > 0 && l.EndDate != null && l.EndDate > DateTime.Now)
            .ToListAsync();

        foreach (var loan in loans)
        {
            // Nếu tất cả các kỳ đã thanh toán -> bỏ qua khoản vay này
            if (loan.PaymentSchedules.All(ps => ps.Paymentstatus))
            {
                continue;
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == loan.AccountId);
            if (account == null) continue;

            if (account.Balance >= loan.MonthlyPayment)
            {
                var nextUnpaid = loan.PaymentSchedules
                    .Where(ps => !ps.Paymentstatus)
                    .OrderBy(ps => ps.PaymentDueDate)
                    .FirstOrDefault();

                if (nextUnpaid == null)
                {
                    // Không có kỳ cần thanh toán nào (dù đã check ở trên, để an toàn)
                    continue;
                }

                account.Balance -= loan.MonthlyPayment;
                loan.OweMoney -= loan.MonthlyPayment;
                loan.OweMoney = Math.Max(0, loan.OweMoney); // Không để âm

                // Đánh dấu kỳ hiện tại là đã thanh toán
                nextUnpaid.Paymentstatus = true;
                nextUnpaid.IsReminderSent = true;
                string description;

                if (loan.OweMoney == 0)
                {
                    description = "Final auto-loan payment (Loan closed)";

                    // Xóa tất cả lịch thanh toán còn lại
                    _context.LoanPaymentSchedules.RemoveRange(loan.PaymentSchedules);

                    // Xoá khoản vay
                    _context.Loans.Remove(loan);
                }
                else
                {
                    description = "Monthly auto-loan payment";
                }

                _context.Transactions.Add(new Transaction
                {
                    FromAccountId = account.Id,
                    ToAccountId = null,
                    Amount = loan.MonthlyPayment,
                    TransactionDate = DateTime.Now,
                    TransactionType = "LoanPayment",
                    Description = description,
                    FromAccount = account,
                    ToAccount = null
                });
            }
            else
            {
                // Handle insufficient balance scenario (optional)
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task AutoPayCreditCardDebts()
    {
        var today = DateTime.Today;
        var creditCards = await _context.CreditCards
            .Include(c => c.Account)
            .ThenInclude(u => u.User)
            .Where(c => c.IsActive && c.CurrentDebt > 0)
            .ToListAsync();

        foreach (var card in creditCards)
        {
            var userEmail = card.Account.User.Email;
            var account = card.Account;


            //1 Chuyen tien tu Newdebt sang Currentdebt neu tra het no truoc
            if (card.CurrentDebt == 0 && card.NewDebt > 0 && today >= card.StatementDate)
            {
                card.CurrentDebt = card.NewDebt;
                card.NewDebt = 0;
                card.BillingCycleStart = today;

                card.BillingCycleStart = today;
                card.StatementDate = today.AddDays(30);
                card.DueDate = card.StatementDate.AddDays(5);

                await _context.Transactions.AddAsync(new Transaction
                {
                    FromAccountId = account.Id,
                    Amount = card.CurrentDebt,
                    TransactionType = "NewStatementCycle",
                    Description =
                        $"Transfer {card.CurrentDebt:C} from new debt to current debt for card{card.CardNumber}",
                    TransactionDate = DateTime.Now,
                    FromAccount = account
                });
                await _emailService.Send(userEmail, "Start new statement period",
                    $"New period starts for card {card.CardNumber}.\n" +
                    $"Current outstanding balance: {card.CurrentDebt:C}.\n" +
                    $"Payment deadline: {card.DueDate:yyyy-MM-dd}.");
            }
            // neu con no ky cu ma lai tieu tiep -> gop luon newdebt va currentdebt
            else if (card.CurrentDebt > 0 && card.NewDebt > 0 && today >= card.StatementDate)
            {
                card.CurrentDebt += card.NewDebt;
                card.NewDebt = 0;

                await _context.Transactions.AddAsync(new Transaction
                {
                    FromAccountId = account.Id,
                    Amount = card.CurrentDebt,
                    TransactionType = "NewDebtMergedDueToUnpaid",
                    Description =
                        $"Due to not paying the previous period, the additional amount is added. {card.CurrentDebt:C} vào CurrentDebt cho thẻ {card.CardNumber}",
                    TransactionDate = DateTime.Now,
                    FromAccount = account
                });

                await _emailService.Send(userEmail, "New period debt accumulation",
                    $"You have not paid the previous period, new transactions are added.\n" +
                    $"Current card balance {card.CardNumber}: {card.CurrentDebt:C}.");
            }

            //2. neu tra du tien -> tu dong tra het
            if (account.Balance >= card.CurrentDebt && card.CurrentDebt > 0)
            {
                decimal payment = card.CurrentDebt;
                account.Balance -= payment;
                card.CurrentDebt = 0;

                card.NewDebt = 0;
                card.BillingCycleStart = today;
                card.StatementDate = today.AddDays(30);
                card.DueDate = card.StatementDate.AddDays(5);

                await _context.Transactions.AddAsync(new Transaction
                {
                    FromAccountId = account.Id,
                    Amount = payment,
                    TransactionType = "CreditCardFullAutoPayment",
                    Description = $"Auto full payment {payment:C} from account {account.AccountNumber}",
                    TransactionDate = DateTime.Now,
                    FromAccount = account
                });

                await _emailService.Send(userEmail, "Pay off credit card debt in full",
                    $"You have paid in full {payment:C} for card {card.CardNumber}.\n" +
                    $"Remaining card: {account.Balance:C}.\n" +
                    $"New cycle: {card.BillingCycleStart:yyyy-MM-dd} → {card.StatementDate:yyyy-MM-dd}, term: {card.DueDate:yyyy-MM-dd}");

                continue;
            }

            //3 neu tra du 1 phan 
            if (account.Balance > 0 && card.CurrentDebt > 0)
            {
                decimal partial = account.Balance;
                card.CurrentDebt -= partial;
                account.Balance = 0;

                await _context.Transactions.AddAsync(new Transaction
                {
                    FromAccountId = account.Id,
                    Amount = partial,
                    TransactionType = "CreditCardPartialPayment",
                    Description = $"Partial payment {partial:C} for card {card.CardNumber}",
                    TransactionDate = DateTime.Now,
                    FromAccount = account
                });

                await _emailService.Send(userEmail, "Partial payment of credit card debt",
                    $"You paid {partial:C} for card {card.CardNumber}.\n" +
                    $"Still in debt: {card.CurrentDebt:C}.\n" +
                    $"Payment term: {card.DueDate:yyyy-MM-dd}.");
            }

            //4 qua han -> tinh lai + phat
            if (today > card.DueDate && card.CurrentDebt > 0)
            {
                int daysLate = (today - card.DueDate).Days;
                decimal interestPerDay = card.InterestRate / 100 / 365;
                decimal interest = card.CurrentDebt * interestPerDay * daysLate;
                decimal penalty = card.CurrentDebt * 0.015m;

                card.CurrentDebt += interest + penalty;

                await _context.Transactions.AddAsync(new Transaction
                {
                    FromAccountId = account.Id,
                    Amount = interest + penalty,
                    TransactionType = "CreditCardInterestPenalty",
                    Description =
                        $"Calculate interest + penalty {interest + penalty:C} for card {card.CardNumber} after {daysLate} days",
                    TransactionDate = DateTime.Now,
                    FromAccount = account
                });

                await _emailService.Send(userEmail, "Lãi & phạt do quá hạn thẻ tín dụng",
                    $"Thẻ {card.CardNumber} đã quá hạn {daysLate} ngày.\n" +
                    $"Lãi: {interest:C}, Phạt: {penalty:C}.\n" +
                    $"Tổng dư nợ mới: {card.CurrentDebt:C}.");
            }

            //5. neu ko con tien & van no -> canh bao that bai
            if (account.Balance == 0 && card.CurrentDebt > 0 && today > card.DueDate)
            {
                card.InterestRate += 1.5m;

                await _context.Transactions.AddAsync(new Transaction
                {
                    FromAccountId = account.Id,
                    Amount = 0,
                    TransactionType = "CreditCardAutoPaymentFailed",
                    Description = $"Automatic failed card payment {card.CardNumber}",
                    TransactionDate = DateTime.Now,
                    FromAccount = account
                });

                await _emailService.Send(userEmail, "Payment failed due to insufficient funds",
                    $"Not enough money to pay the card{card.CardNumber}.\n" +
                    $"Current balance: {card.CurrentDebt:C}.\n" +
                    $"New interest rate: {card.InterestRate}%/year.");
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task AutoBlockCreditCard()
    {
        var today = DateTime.Today;
        await _context.CreditCards
            .Where(c => c.IsActive && c.ExpirationDate < today)
            .ExecuteUpdateAsync(b => b
                .SetProperty(c => c.IsActive, false)
            );
    }
}