namespace CP2496H07Group1.Models;

public class TransferViewModel
{
    public long AccountType { get; set; } // Add this property for the selected account ID
    public string AccountNumber { get; set; } = string.Empty; // ng??i nh?n
    public decimal Monney { get; set; }
    public string TransferContent { get; set; } = string.Empty;
    public int Pin { get; set; }
}