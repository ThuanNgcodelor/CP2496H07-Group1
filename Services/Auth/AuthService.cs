using System.Security.Cryptography;
using System.Text;
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Email;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;

namespace CP2496H07Group1.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDataContext _context;
    private readonly RedisService _redis;
    private readonly IEmailService _emailService;

    public AuthService(AppDataContext context, RedisService redis, IEmailService emailService)
    {
        _context = context;
        _redis = redis;
        _emailService = emailService;
    }


    public async Task<User?> Login(User model)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber && u.IsConfirm == true);

        if (user == null )
        {
            throw new ArgumentException("Phone number is invalid");
        }
        
        if (user.Status == "Off")
        {
            throw new ArgumentException("User has been locked");
        }
        
        string redisKey = $"fails_login_{user.PhoneNumber}";
        int failedLoginAttempts = int.TryParse(_redis.Get(redisKey),out int attempts) ? attempts : 0;

        if (failedLoginAttempts > 3)
        {
            user.Status = "Off";
            await _context.SaveChangesAsync();
            return null;
        }
        
        if (!VerifyPassword(model.PasswordHash, user.PasswordHash))
        {
            failedLoginAttempts++;
            _redis.Set(redisKey, failedLoginAttempts.ToString(), TimeSpan.FromDays(30));
            throw new ArgumentException("Password is invalid");
        }
        
        // test ok
        _redis.Remove(redisKey);
        return user;
    }


    public Task<User?> ConfirmSms(string phone, string token)
    {
        throw new NotImplementedException();
    }

    public async Task<User> Register(User model)
    {
        if (await _context.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber))
        {
            throw new ArgumentException("This phone number is already registered.");
        }

        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Client");
        if (userRole == null)
        {
            userRole = new Role
            {
                RoleName = "Client",
                Description = "Client",
                Status = true
            };
            _context.Roles.Add(userRole);
            await _context.SaveChangesAsync();
        }

        // Gán mật khẩu đã hash
        model.PasswordHash = HashPassword(model.PasswordHash);

        model.Roles ??= new List<Role>();
        model.Roles.Add(userRole);

        _context.Users.Add(model);
        await _context.SaveChangesAsync();

        return model;
    }
    
    public async Task<User> RegisterOpt(User model)
    {
        
        if (await _context.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber))
        {
            throw new ArgumentException("This phone number is already registered.");
        }
        
        var otp = new Random().Next(100000, 999999).ToString();
        
        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Client");
        if (userRole == null)
        {
            userRole = new Role
            {
                RoleName = "Client",
                Description = "Client",
                Status = true
            };
            _context.Roles.Add(userRole);
            await _context.SaveChangesAsync();
        }

        model.IsConfirm = false;
        model.ConfirmationToken = otp;
        // Gán mật khẩu đã hash
        model.PasswordHash = HashPassword(model.PasswordHash);

        model.Roles ??= new List<Role>();
        model.Roles.Add(userRole);

        _context.Users.Add(model);
        await _context.SaveChangesAsync();
        
        var emailBody = CreateOtpEmailTemplate(model.Email, otp);
        await _emailService.Send(model.Email, "Confirm regiser", emailBody);
        
        return model;
    }
    
    private static string CreateOtpEmailTemplate(string email, string otp)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Confirm registration</h2>
                <p>Hello, I'm David</p>
                <p>Below is the OTP code to confirm your account.</p>
                <h3 style='color: #4CAF50;'>{otp}</h3>
                <p>Plese enter this code on the confirmation page to complete your registrtion. This code is valid for 10 minutes</p>
                <p>Best regards,<br/>David Nguyen/p>
                <p>TeckBank</p>
            </body>
            </html>";
    }
    
    public async Task<User?> ConfirmEmail(string email, string token)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.ConfirmationToken == token);
        if (user == null)
        {
            return null;
        }
        user.IsConfirm = true;
        user.ConfirmationToken = null;
        await _context.SaveChangesAsync();
        return user;
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