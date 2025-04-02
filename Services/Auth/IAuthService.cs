using CP2496H07Group1.Models;

namespace CP2496H07Group1.Services.Auth;

public interface IAuthService
{
    Task<User?> Login(User model);
    Task<User> RegisterOpt(User model);
    Task<User?> ConfirmEmail(string email, string token);
    Task<User?> ConfirmSms(string phone, string token);
    
    
    Task<User> Register(User model);
}