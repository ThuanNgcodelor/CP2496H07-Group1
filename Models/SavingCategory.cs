using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CP2496H07Group1.Models
{
    public class SavingCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public required long Id { get; set; }

        public DateTime Date { get; set; } = DateTime.Now; // Không dùng [DatabaseGenerated]

        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn hoặc bằng 0")]
        public decimal Money { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập loại tiết kiệm")]
        [MaxLength(100, ErrorMessage = "Loại tiết kiệm không được vượt quá 100 ký tự")]
        public required string TypeTk { get; set; }

        [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string Description { get; set; } = string.Empty; // Không bắt buộc

        [Required(ErrorMessage = "Vui lòng nhập số tháng")]
        [Range(1, int.MaxValue, ErrorMessage = "Số tháng phải lớn hơn 0")]
        public int Month { get; set; }

        public List<Savings> Savings { get; set; } = new();
    }
}