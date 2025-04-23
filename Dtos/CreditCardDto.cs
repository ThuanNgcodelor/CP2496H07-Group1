namespace CP2496H07Group1.Dtos;

public class CreditCardDto
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal CurrentDebt { get; set; }
    public decimal InterestRate { get; set; }
    public DateTime StatementDate { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsActive { get; set; }
}