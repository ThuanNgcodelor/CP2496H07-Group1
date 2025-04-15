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
        public decimal Balance { get; set; }
        public string DisplayText { get; set; }
        public int? TypeVip { get; set; }
    }

    public class LoanOptionViewModel
    {
        public long LoanOptionId { get; set; }
        public string DisplayText { get; set; }
        public int LoanDate { get; set; }
        public decimal InterestRate { get; set; }
    }
}