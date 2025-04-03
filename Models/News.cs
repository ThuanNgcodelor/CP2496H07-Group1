using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models;

public class News
{
    public required long Id { get; set; }

    [Required(ErrorMessage = "Title cannot be blank.")]
    public required string Title { get; set; }

    [Required(ErrorMessage = "Content cannot be empty.")]
    public required string Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public required long CategoryId { get; set; }

    [Required(ErrorMessage = "Image cannot be blank.")]
    public required string ImageUrl { get; set; }

    public bool IsConfirm { get; set; } = true;

    public  virtual required Category Category { get; set; }
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}