namespace CP2496H07Group1.Models;

public class DiscountCode
{
    public required long Id { get; set; }
    public required string DiscountCodes { get; set; }
    public required int Points { get; set; }
    public required int Percent { get; set; }
    public required DateTime LongMonth {get; set;}
    
    
    public List<AccountDiscounts> AccountDiscounts { get; set; } = new();
}