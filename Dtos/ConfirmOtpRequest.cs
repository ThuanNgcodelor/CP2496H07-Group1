namespace CP2496H07Group1.Dtos;

public class ConfirmOtpRequest
{
    public string OldEmail { get; set; }
    public string NewEmail { get; set; }
    public string InputOtp { get; set; }
}