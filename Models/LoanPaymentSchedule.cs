using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models
{
    public class LoanPaymentSchedule
    {
        [Key]
        public long Id { get; set; }

        public long LoanId { get; set; }

        public DateTime PaymentDueDate { get; set; }

        public bool IsReminderSent { get; set; } = false;

        public bool Paymentstatus { get; set; } = false;
        public Loans Loan { get; set; }
    }
}