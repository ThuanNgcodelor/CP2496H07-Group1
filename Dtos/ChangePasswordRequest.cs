namespace CP2496H07Group1.Dtos;

public class ChangePasswordRequest
{
    public string PhoneNumber { get; set; }
    public string OldPassword { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmPassword { get; set; }
}