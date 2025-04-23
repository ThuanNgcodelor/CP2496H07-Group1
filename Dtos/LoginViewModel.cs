using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Dtos;

public class LoginViewModel
{
    [Required]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }
}
