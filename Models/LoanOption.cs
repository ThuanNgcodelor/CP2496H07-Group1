using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models;

public class LoanOption
{
    public  long Id { get; set; }
    public required int LoanDate { get; set; }

    [Range(0.01, 1.0, ErrorMessage = "Interest rate must be between 0.01 and 1.0 (1 = 100%).")]
    public required double InterestRate { get; set; }

    public List<Loans> Loans { get; set; } = new();

    public class LoanOptionViewModel
    {
        public long LoanOptionId { get; set; }
        public string DisplayText { get; set; } = string.Empty;
        public int LoanDate { get; set; }
        public decimal InterestRate { get; set; }
    }
}