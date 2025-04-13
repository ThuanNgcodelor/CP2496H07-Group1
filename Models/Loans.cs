using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CP2496H07Group1.Models;

public class Loans 
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
    public long Id { get; set; }
    public required long UserId { get; set; }
    [Range(10000000, long.MaxValue, ErrorMessage = "AmountBorrowed must be at least 10000000.")]
    [RegularExpression(@"^[1-9]\d*$", ErrorMessage = "AmountBorrowed must be a positive number.")]
  
    public required decimal AmountBorrowed { get; set; }
    public required long LoanName { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    [Required(ErrorMessage = "Please select a loan option.")]
    public required long LoanOptionId { get; set; }
    public required int MonthlyPayment { get; set; }
    public long? VipId { get; set; }
    public required int OweMoney { get; set; }

    public required User User { get; set; }
    public required LoanOption LoanOption { get; set; }

    public Vip? Vip { get; set; }
}