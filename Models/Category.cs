using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models;

public class Category
{
    public long Id { get; set; }
    [MinLength(3, ErrorMessage = "Category name must be at least 3 characters")]
    [Required(ErrorMessage = "Category name cannot be blank.")]
    [RegularExpression(@"^(?!.*\d{2,}).*$", ErrorMessage = " Category Name must not contain a sequence of digits.")]
    public required string Name { get; set; }
    [Required(ErrorMessage = "Description cannot be blank.")]
    [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
    public required string Description { get; set; }
    public bool IsConfirm { get; set; } = true;
    public virtual ICollection<News> News { get; set; } = new List<News>();
}