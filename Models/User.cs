namespace CP2496H07Group1.Models;

public class User
{
    public long Id { get; set; }
    public required string  PhoneNumber { get; set; }
    public required string  PasswordHash { get; set; }
    public required string  FirstName { get; set; }
    public required string  LastName { get; set; }
    public required string  Email { get; set; }
    public required DateTime Birthday { get; set; }
    public required string  Address { get; set; }
    public required string  Identity {get; set;}
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