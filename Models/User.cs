using System.ComponentModel.DataAnnotations;

namespace CP2496H07Group1.Models;

public class User
{
    public long Id { get; set; }
    [Phone]
    public required string  PhoneNumber { get; set; }
    public required string  PasswordHash { get; set; }
    [StringLength(10, ErrorMessage = "First name cannot be longer than 10 characters.")]
    public required string  FirstName { get; set; }
    [StringLength(10, ErrorMessage = "Last name cannot be longer than 10 characters.")]
    public required string  LastName { get; set; }
    [EmailAddress(ErrorMessage = "Email must contain '@' symbol.")]
    public required string  Email { get; set; }
    [PastDate(ErrorMessage = "Birthday must be before today.")]
    public required DateTime Birthday { get; set; }
    public required string  Address { get; set; }
    [RegularExpression(@"^\d{12}$", ErrorMessage = "Identity must be exactly 12 digits.")]
    public required string Identity { get; set; }
    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public bool IsConfirm { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public string? ConfirmationToken { get; set; }
    public string? Avatar { get; set; } = null;
    public required string Status { get; set; } = "On";
    
    public required ICollection<Role> Roles { get; set; } = new List<Role>();
    public required List<Account> Accounts { get; set; } = new List<Account>();
    public required List<Request> Requests { get; set; } = new List<Request>();
    public required List<UserInsurance> UserInsurances { get; set; } = new List<UserInsurance>();
}

public class PastDateAttribute : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        if (value is DateTime date)
        {
            return date < DateTime.Now;
        }
        return false;
    }
}