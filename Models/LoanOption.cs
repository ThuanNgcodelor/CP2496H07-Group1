namespace CP2496H07Group1.Models;

public class LoanOption
{
    public required long Id { get; set; }
    public required DateTime LoanMonth { get; set; }
    public required double InterestRate { get; set; }

    public List<Loans> Loans { get; set; } = new();
}