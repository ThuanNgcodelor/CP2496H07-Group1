namespace CP2496H07Group1.Models;

public class Role
{
    public required long Id { get; set; }
    public string RoleName { get; set; }
    public required string Description { get; set; }
    public required bool Status { get; set; }
    
    public virtual List<User> Users { get; set; } 
}