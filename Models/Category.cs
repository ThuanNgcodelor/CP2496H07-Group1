using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models;

public class Category
{
    public required long Id { get; set; }

    [Required(ErrorMessage = "Category name cannot be blank.")]
    public required string Name { get; set; }
    public required string Description { get; set; }

    public virtual ICollection<News> News { get; set; } = new List<News>();
}