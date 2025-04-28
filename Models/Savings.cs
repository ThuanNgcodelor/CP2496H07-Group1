using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CP2496H07Group1.Models;

public class Savings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; } // Removed 'required' since DB generates it

    public long? AccountId { get; set; } // Nullable, as per your model

    public DateTime DateStart { get; set; }

    public DateTime DateEnd { get; set; }

    [Required(ErrorMessage = "TypeTk is required")]
    public string TypeTk { get; set; } = string.Empty; // Initialized to avoid nullable warning

    public string Status { get; set; } = "Unfinished";   //chưa hoàn thành

    public string Pay { get; set; } = "Not withdrawn yet";    //

    public long SavingCategoryId { get; set; } // Required by relationship

    [ForeignKey("SavingCategoryId")]
    public SavingCategory SavingCategory { get; set; } = null!; // Non-nullable, EF will ensure it's set

    public Account? Account { get; set; } // Nullable, as per your model
}