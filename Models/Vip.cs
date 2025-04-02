using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models;

public class Vip

{
    public required long Id { get; set; }

    [Required(ErrorMessage = "Plese enter Type Vip")]

    public  int TypeVip { get; set; }
    [Required(ErrorMessage = "Plese enter price")]
    public decimal Price { get; set; }

    public required bool NoPick { get; set; } = false;

    public int MoneyBack { get; set; }
    
    public List<Loans> Loans { get; set; } = new();
    
    // Thêm danh sách Transaction liên kết với Vip
    public List<Transaction> Transactions { get; set; } = new();
    

}