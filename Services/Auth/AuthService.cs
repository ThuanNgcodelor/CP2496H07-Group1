using System.Security.Cryptography;
using System.Text;
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;

namespace CP2496H07Group1.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDataContext _context;
    private readonly RedisService _redis;

    public AuthService(AppDataContext context, RedisService redis)
    {
        _context = context;
        _redis = redis;
    }


    public async Task<User?> Login(User model)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber && u.IsConfirm == true);
        if (user == null && !VerifyPassword(model.PasswordHash, user.PasswordHash))
        {
            return null;
        }
        return user;
    }

    public Task<User> RegisterOpt(User model)
    {
        throw new NotImplementedException();
    }

    public Task<User?> ConfirmEmail(string email, string token)
    {
        throw new NotImplementedException();
    }

    public Task<User?> ConfirmSms(string phone, string token)
    {
        throw new NotImplementedException();
    }
    
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
    
    private static bool VerifyPassword(string inputPassword, string storedHash)
    {
        return HashPassword(inputPassword) == storedHash;
    }
}