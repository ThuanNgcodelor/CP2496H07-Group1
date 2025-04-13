namespace CP2496H07Group1.Models;

public class DiscountCode
{
    public long Id { get; set; }
    public required string DiscountCodes { get; set; }
    public required int Points { get; set; }
    public required int Percent { get; set; }
    public required int LongDate { get; set;}
    
    
    public List<AccountDiscounts> AccountDiscounts { get; set; } = new();
}