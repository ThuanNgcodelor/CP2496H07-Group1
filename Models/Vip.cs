using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CP2496H07Group1.Models
{
    public class Vip
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public required long Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập loại VIP")]
        [Range(1, int.MaxValue, ErrorMessage = "Loại VIP phải lớn hơn 0")]
        public int TypeVip { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái NoPick")]
        public bool NoPick { get; set; } = false;

        [Range(0, int.MaxValue, ErrorMessage = "Số tiền hoàn lại phải lớn hơn hoặc bằng 0")]
        public int? MoneyBack { get; set; }

        public List<Loans> Loans { get; set; } = new(); // Liên kết 1-n với Loans
        public List<Transaction> Transactions { get; set; } = new(); // Liên kết 1-n với Transactions
    }
}