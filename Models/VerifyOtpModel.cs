using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models;

public class VerifyOtpModel
{
    public string? Email { get; set; }

    [Required(ErrorMessage = "Plese enter OTP code")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be 6 numbers.")]
    public string? Otp { get; set; }
}