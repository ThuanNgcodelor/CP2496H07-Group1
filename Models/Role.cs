using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CP2496H07Group1.Models;

public class Role
{
    [Key] 
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public string RoleName { get; set; }
    public required string Description { get; set; }
    public required bool Status { get; set; }
    
    public virtual List<User> Users { get; set; } 
    public virtual List<Admin> Admins { get; set; } 

}