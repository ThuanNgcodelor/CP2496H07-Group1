using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models;

public class Faq
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Question cannot be empty.")]
    [StringLength(200, ErrorMessage = "Question cannot exceed 200 characters")]
    [RegularExpression(@"^(?!.*\d{4,}).*\?\s*$", ErrorMessage = "Question must not contain a sequence of digits and must end with a question mark (?)")]
    [MinLength(10, ErrorMessage = "Question must be at least 10 characters")]
    public required string Question { get; set; }

    [Required(ErrorMessage = "Answer cannot be empty.")]
    [MinLength(10, ErrorMessage = "Answer must be at least 10 characters")]
    public required string Answer { get; set; }

    public bool IsConfirm { get; set; } = true;
}