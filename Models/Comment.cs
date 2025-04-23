using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models;

public class Comment
{
    public long Id { get; set; }
    public long? NewsId { get; set; }

    public long? UserId { get; set; }

    [StringLength(500, ErrorMessage = "Comments cannot exceed 500 characters.")]
    public required string Content { get; set; }
    public bool IsAdminReply { get; set; } = false;

    public long? AdminId { get; set; }
    public long? ParentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public News News { get; set; }  // n-1 with News
    public User User { get; set; }               // n-1 with User
    public Admin Admin { get; set; }
    public Comment Parent { get; set; } 
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}