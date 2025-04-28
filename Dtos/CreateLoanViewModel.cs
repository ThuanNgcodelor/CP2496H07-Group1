using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Dtos
{
    public class CreateLoanViewModel
    {
        public decimal AmountBorrowed { get; set; }
        public long LoanName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public long LoanOptionId { get; set; }

        public List<AccountViewModel> Accounts { get; set; }


        public long? AccountId { get; set; }


        public List<LoanOptionViewModel> LoanOptions { get; set; }
    }

    public class AccountViewModel
    {
        public long AccountId { get; set; }
        public string AccountNumber { get; set; }
        public long? Point { get; set; }
        public decimal Balance { get; set; }
        public string DisplayText { get; set; }
        public int? TypeVip { get; set; }
        public List<AccountViewModel> Accounts { get; set; }
    }

    public class LoanOptionViewModel
    {
        public long LoanOptionId { get; set; }
        public string DisplayText { get; set; }
        public int LoanDate { get; set; }
        public decimal InterestRate { get; set; }

        public List<LoanOptionViewModel> LoanOptions { get; set; }
    }

    public class TopupInputModel
    {
        [Required]
        public long AccountId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal AmountTopup { get; set; }
        public IFormFile ImageUpload { get; set; }
        [Required]
        public string? Description { get; set; }
        public List<AccountViewModel> Accounts { get; set; } = new();
    }
    public class LoanPaymentDto
    {
        public long LoanId { get; set; }
        public string LoanName { get; set; }
        public decimal AmountBorrowed { get; set; }
        public decimal MonthlyPayment { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime FirstDueDate { get; set; }
        public decimal OweMoney { get; set; }
        public string Status { get; set; }
        public long LoanOptionId { get; set; }

        // Next payment information
        public DateTime? NextDueDate { get; set; }
        public decimal? NextAmount { get; set; }
    }


}