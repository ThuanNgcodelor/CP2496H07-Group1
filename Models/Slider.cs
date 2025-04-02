using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CP2496H07Group1.Models;

public class Slider
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required(ErrorMessage = "Plese enter Name")]
    [MaxLength(255)] // Gi?i h?n ?? dài chu?i
    public string Name { get; set; }

    [Required(ErrorMessage = "Plese enter Detail")]
    public string Detail { get; set; }

    [Required(ErrorMessage = "Plese enter Image")]
    public string Image { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Date { get; set; } = DateTime.Now;

    public bool Status { get; set; } = true;
}