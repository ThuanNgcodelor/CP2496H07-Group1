namespace CP2496H07Group1.Models;

public class Admin
{
    public long Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Description { get; set; }
    public required ICollection<Role> Roles { get; set; } = new List<Role>();
    
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}