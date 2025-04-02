namespace CP2496H07Group1.Models;

public class Transaction
{
    public required long Id { get; set; }
    public required long FromAccountId { get; set; }
    public required long? ToAccountId { get; set; }
    public required decimal Amount { get; set; } = 0;
    public required string TransactionType { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    public required string? Description { get; set; }
    public long? VipId { get; set; }
    
    public required Account FromAccount { get; set; } //n-1 với Account nguồn
    public required Account ToAccount { get; set; } //n-1 với Account đích

    public long? DiscountCodeId { get; set; }  // Khóa ngoại có thể null
    public DiscountCode? DiscountCode { get; set; } // Quan hệ với DiscountCode
    public Vip? Vip { get; set; } //quan he voi Vip
}