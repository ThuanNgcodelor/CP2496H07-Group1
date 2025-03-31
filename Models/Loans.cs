namespace CP2496H07Group1.Models;

public class Loans
{
    public required long Id { get; set; }
    public required long AccountId { get; set; }
    public required decimal AmountBorrowed { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required long LoanOptionId { get; set; }
    public required int MonthlyPayment { get; set; }
    public long? VipId { get; set; }
    public required int OweMoney { get; set; }

    public required Account Account { get; set; }
    public required LoanOption LoanOption { get; set; }
    public Vip? Vip { get; set; }
}