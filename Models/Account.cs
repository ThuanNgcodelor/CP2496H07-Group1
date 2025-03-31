namespace CP2496H07Group1.Models;

public class Account
{
    public required long AccountId { get; set; }     
    public required long UserId { get; set; }            
    public required string AccountNumber { get; set; } 
    public required decimal Balance { get; set; }       
    public required string AccountType { get; set; }    
    public required DateTime CreatedAt { get; set; } = DateTime.Now;
    public long? VipId { get; set; } = null; // co the null
    
    public required User User { get; set; }               // n-1 with User
    public required List<Transaction> TransactionsFrom { get; set; } // Tu Tai khoan nay den tai khoan khac
    public required List<Transaction> TransactionsTo { get; set; }   // Tu tai khoan khac den tai khoan nay
    public List<AccountDiscounts> AccountDiscounts { get; set; } = new();
    public Vip? Vip { get; set; }
    public List<Loans> Loans { get; set; } = new();
}
