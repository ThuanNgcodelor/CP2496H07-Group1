using System.Security.Cryptography;
using System.Text;
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Email;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Configs.Sms;
using CP2496H07Group1.Dtos;
using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;

namespace CP2496H07Group1.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDataContext _context;
    private readonly RedisService _redis;
    private readonly IEmailService _emailService;
    private readonly SpeedSmsService _smsService;

    public AuthService(AppDataContext context, RedisService redis, IEmailService emailService, SpeedSmsService smsService)
    {
        _context = context;
        _redis = redis;
        _emailService = emailService;
        _smsService = smsService;
    }
    
    public async Task<Admin?> LoginAdmin(Admin model)
    {
        var admin = await _context.Admins
            .Include(a=>a.Roles)
            .FirstOrDefaultAsync(a=>a.Email == model.Email);

        if (admin == null)
        {
            throw new ApplicationException("Invalid Email or password");
        }

        if (!VerifyPassword(model.Password, admin.Password))
        {
            throw new ApplicationException("Invalid Email or password");
        }
        
        return admin;
    }

    public async Task<User?> Login(User model)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber && u.IsConfirm == true);

        if (user == null)
        {
            throw new ArgumentException("Phone number is invalid");
        }

        if (user.Status == "Off")
        {
            throw new ArgumentException("User has been locked");
        }

        string redisKey = $"fails_login_{user.PhoneNumber}";
        int failedLoginAttempts = int.TryParse(_redis.Get(redisKey), out int attempts) ? attempts : 0;

        if (failedLoginAttempts >= 3)
        {
       
            throw new ArgumentException("User has been locked");;
        }

        if (!VerifyPassword(model.PasswordHash, user.PasswordHash))
        {
            failedLoginAttempts++;
            if (failedLoginAttempts >= 3)
            {
                user.FailedLoginAttempts = failedLoginAttempts;
                user.Status = "Off";
                await _context.SaveChangesAsync();
            }
            _redis.Set(redisKey, failedLoginAttempts.ToString(), TimeSpan.FromMinutes(30));
            throw new ArgumentException("Password is invalid" + failedLoginAttempts);
        }

        _redis.Remove(redisKey);
        return user;
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


    //Chuc nang forgot password
    // Dau tien se find Phone number 
    //1
    public async Task<string> ForgotPassword(string phoneNumber)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null)
        {
            throw new ArgumentException("This phone number does not exist.");
        }
     
        //Tao Opt sau do gui len Cache voi 30
        var otp = new Random().Next(100000, 999999).ToString();
        string redisKey = $"reset_password_{user.PhoneNumber}";
        _redis.Set(redisKey, otp, TimeSpan.FromMinutes(30));

        var sendMail = CreateOtpEmailTemplate(user.Email, otp);
        await _emailService.Send(user.Email, "Password reset", sendMail);

        return "OTP sent to your email. Please check your inbox.";
    }

    //2 ValidateOpt
    public async Task ValidateOtp(string? phoneNumber, string otp)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null)
        {
            throw new ArgumentException("No account found with this phone number.");
        }

        string redisKey = $"reset_password_{user.PhoneNumber}";
        string storedOtp = _redis.Get(redisKey);

        if (storedOtp != otp)
        {
            throw new ArgumentException("Invalid or expired OTP.");
        }
    }

    public async Task<string> SendOtpChangeEmail(string oldMail, string newEmail)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u=>u.Email == oldMail);
        if (user == null)
        {
            throw new ArgumentException("User does not exist.");
        }
        var opt = new Random().Next(100000, 999999).ToString();
        string redisKey = $"change_email_{newEmail}";
        _redis.Set(redisKey, opt, TimeSpan.FromMinutes(5));
        
        var sendMail = CreateOtpChangeEmail(user.Email, opt);
        await _emailService.Send(user.Email, "Change email", sendMail);
        return "OTP sent to your email. Please check your inbox.";
    }

    public async Task<User?> ConfirmOtpChangeEmail(string oldEmail,string newEmail, string inputOtp)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u=>u.Email == oldEmail);
        if (user == null)
        {
            throw new ArgumentException("User does not exist.");
        }
        
        string redisKey = $"change_email_{newEmail}";
        var storedOtp = _redis.Get(redisKey);
        
        if (storedOtp == null)
            throw new Exception("OTP is invalid.");

        if (storedOtp != inputOtp)
            throw new Exception("OTP entered wrong OTP.");
        
        user.Email = newEmail;
        await _context.SaveChangesAsync();
        _redis.Remove(redisKey);
        return user;
    }

    public async Task ChangePassword(string? phoneNumber, string oldPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrEmpty(oldPassword) && string.IsNullOrEmpty(phoneNumber))
        {
            throw new ArgumentException("Old password is required.");
        }

        var oldPasswordHash = HashPassword(oldPassword);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.PasswordHash == oldPasswordHash);
    
        if (user == null)
        {
            throw new ArgumentException("Incorrect phone number or old password.");
        }

        user.PasswordHash = HashPassword(newPassword);
        await _context.SaveChangesAsync();
    }



    public async Task ResetPassword(string? phoneNumber,string newPassword, string confirmPassword )
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null)
        {
            throw new ArgumentException("No account found with this phone number.");
        }

        user.PasswordHash = HashPassword(newPassword);
        await _context.SaveChangesAsync();
        string redisKey = $"reset_password_{user.PhoneNumber}";
        _redis.Remove(redisKey);
    }


    public async Task<User> RegisterOpt(User model)
    {
        if (await _context.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber))
        {
            throw new ArgumentException("This phone number is already registered.");
        }

        if (await _context.Users.AnyAsync(u => u.Email == model.Email))
        {
            throw new ArgumentException("This email is already registered.");
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

        var emailBody = CreateOtpEmailForgot(model.Email, otp);
        await _emailService.Send(model.Email, "Confirm register", emailBody);

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
    
    private static string CreateOtpChangeEmail(string email, string otp)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Confirm Change Email.</h2>
                <p>Hello, I'm David</p>
                <p>Below is the OTP code to confirm your email.</p>
                <h3 style='color: #4CAF50;'>{otp}</h3>
                <p>Plese enter this code on the confirmation page to complete your registrtion. This code is valid for 10 minutes</p>
                <p>Best regards,<br/>David Nguyen/p>
                <p>TeckBank</p>
            </body>
            </html>";
    }

    private static string CreateOtpEmailForgot(string email, string otp)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Confirm your password reset</h2>
                <p>Your OTP is <strong>{otp}</strong></p>
                <p>This code is valid for 10 minutes.</p>
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