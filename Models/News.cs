using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models;

public class News
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    [RegularExpression(@"^(?!.*\d{4,}).*$", ErrorMessage = "Title must not contain a sequence of digits.")]
    public string Title { get; set; }

    [Required(ErrorMessage = "Content cannot be empty.")]
    [MinLength(20, ErrorMessage = "Content must be at least 20 characters")]
    public string Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Category cannot be empty.If there is no category, please create one first.")]
    public long CategoryId { get; set; }

    [Required(ErrorMessage = "Image cannot be blank.")]
    public string ImageUrl { get; set; }

    public bool IsConfirm { get; set; } = true;

    public virtual required Category Category { get; set; }
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}