using CP2496H07Group1.Dtos;
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
    Task<string> SendOtpChangeEmail(string oldMail, string newEmail);
    Task <User?> ConfirmOtpChangeEmail(string oldMail, string newEmail, string inputOtp);
    Task ChangePassword(string? phoneNumber,string oldPassword ,string newPassword, string confirmPassword);
}