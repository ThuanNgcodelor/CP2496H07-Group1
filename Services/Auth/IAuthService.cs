using CP2496H07Group1.Models;

namespace CP2496H07Group1.Services.Auth;

public interface IAuthService
{
    Task<User?> Login(User model);
    Task<User> RegisterOpt(User model);
    Task<User?> ConfirmEmail(string email, string token);
    Task<User> Register(User model);
    Task<string> ForgotPassword(string phoneNumber);
    Task ResetPassword(string? phoneNumber,string newPassword, string confirmPassword);
    Task ValidateOtp(string? phoneNumber, string otp);
}