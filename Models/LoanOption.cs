using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models;

public class LoanOption
{
    public long Id { get; set; }

    [Required]
    [Range(1, 60, ErrorMessage = "Loan duration must be between 1 and 60 months.")]
    [Display(Name = "Loan Duration (Months)")]
    public required int LoanDate { get; set; }

    [Range(0.01, 1.0, ErrorMessage = "Interest rate must be between 0.01% and 1%")]
    [Required]
    [Display(Name = "Interest Rate")]
    public required double InterestRate { get; set; }

    public List<Loans> Loans { get; set; } = new();
}