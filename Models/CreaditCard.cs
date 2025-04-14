namespace CP2496H07Group1.Models;

public class CreditCard
{
    public long Id { get; set; }
    public required long AccountId { get; set; }           // Liên kết với Account
    public required string CardNumber { get; set; }        // Số thẻ
    public required decimal CreditLimit { get; set; }      // Hạn mức
    public required decimal CurrentDebt { get; set; }      // Số tiền đã tiêu
    public required decimal InterestRate { get; set; }     // Lãi suất (%)
    public required DateTime StatementDate { get; set; }   // Ngày sao kê
    public required DateTime DueDate { get; set; }         // Ngày thanh toán tra truoc ngay nay thi ko bi tinh lai
    public bool IsActive { get; set; } = true;             // Trạng thái thẻ

    public required Account Account { get; set; }
}
