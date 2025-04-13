namespace CP2496H07Group1.Models;

public class AccountDiscounts
{
    public long Id { get; set; }
    public required long AccountId { get; set; }
    public required long DiscountId { get; set; }
    public required DateTime SDateTime { get; set; } = DateTime.Now;
    public required DateTime STopDate { get; set; }
    public required int Status { get; set; }
    
    public required DiscountCode DiscountCode { get; set; }  // Mối quan hệ n-1 với DiscountCode
    public required Account Account { get; set; }  // Mối quan hệ n-1 với Account
}