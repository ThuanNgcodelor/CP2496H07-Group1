using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models;

public class Comment
{
    public long Id { get; set; }
    public long? NewsId { get; set; }

    public required long UserId { get; set; }

    [StringLength(500, ErrorMessage = "Comments cannot exceed 500 characters.")]
    public required string Content { get; set; }

    public long? ParentId { get; set; }
    public long? PackageId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual News News { get; set; }  // n-1 with News
    public required User User { get; set; }               // n-1 with User
    public InsurancePackage? InsurancePackage { get; set; }
    public virtual Comment Parent { get; set; } 
    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
}