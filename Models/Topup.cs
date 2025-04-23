using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CP2496H07Group1.Models
{
    public class Topup
    {
        [Key]   
        public long Id { get; set; }
   
        public required long AccountId { get; set; }
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public required decimal AmountTopup { get; set; }
        public required DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Image { get; set; } = string.Empty;
        public required string? Description { get; set; }
        public bool Status { get; set; } = true;

        public Account? Account { get; set; }
    }
}