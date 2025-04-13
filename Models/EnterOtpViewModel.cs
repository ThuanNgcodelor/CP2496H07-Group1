using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models;

public class EnterOtpViewModel
{
    public long UserId { get; set; }

    [Required(ErrorMessage = "OTP is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
    public string Otp { get; set; }
}