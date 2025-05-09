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
    private readonly ILogger<AuthService> _logger;


    public AuthService(AppDataContext context, RedisService redis, IEmailService emailService,
        SpeedSmsService smsService, ILogger<AuthService> logger)
    {
        _context = context;
        _redis = redis;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task<Admin?> LoginAdmin(LoginViewModel model)
    {
        var admin = await _context.Admins
            .Include(a => a.Roles)
            .FirstOrDefaultAsync(a => a.Email == model.Email);

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
            throw new ArgumentException("User has been locked");
            ;
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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == oldMail);
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

    public async Task<User?> ConfirmOtpChangeEmail(string oldEmail, string newEmail, string inputOtp)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == oldEmail);
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

    public async Task ChangePassword(string? phoneNumber, string oldPassword, string newPassword,
        string confirmPassword)
    {
        if (string.IsNullOrEmpty(oldPassword) && string.IsNullOrEmpty(phoneNumber))
        {
            throw new ArgumentException("Old password is required.");
        }

        var oldPasswordHash = HashPassword(oldPassword);
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.PhoneNumber == phoneNumber && u.PasswordHash == oldPasswordHash);

        if (user == null)
        {
            throw new ArgumentException("Incorrect phone number or old password.");
        }

        user.PasswordHash = HashPassword(newPassword);
        await _context.SaveChangesAsync();
    }


    public async Task ResetPassword(string? phoneNumber, string newPassword, string confirmPassword)
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
        var existingUser = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber || u.Email == model.Email);

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

        if (existingUser != null)
        {
            if (existingUser.IsConfirm)
            {
                throw new ArgumentException("This phone number or email is already registered.");
            }

            existingUser.ConfirmationToken = otp;
            existingUser.IsConfirm = false;
            existingUser.PasswordHash = HashPassword(model.PasswordHash);

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();

            var emailBody = CreateOtpEmailForgot(existingUser.Email, otp);
            await _emailService.Send(existingUser.Email, "Confirm register", emailBody);
            return existingUser;
        }


        model.IsConfirm = false;
        model.ConfirmationToken = otp;
        // Gán mật khẩu đã hash
        model.PasswordHash = HashPassword(model.PasswordHash);

        model.Roles ??= new List<Role>();
        model.Roles.Add(userRole);

        _context.Users.Add(model);
        await _context.SaveChangesAsync();

        var newEmailBody = CreateOtpEmailForgot(model.Email, otp);
        await _emailService.Send(model.Email, "Confirm register", newEmailBody);

        return model;
    }

    public async Task ResendOtp(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        if (user.IsConfirm)
        {
            throw new InvalidOperationException("User already confirmed.");
        }

        var otp = new Random().Next(100000, 999999).ToString();
        user.ConfirmationToken = otp;
        await _context.SaveChangesAsync();

        var emailBody = CreateOtpEmailForgot(email, otp);
        await _emailService.Send(email, "Resend OTP", emailBody);
    }


    private static string CreateOtpEmailTemplate(string email, string otp)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Confirm your OTP register </h2>
                <p>Your OTP is <strong>{otp}</strong></p>
                <p>This code is valid for 15 minutes.</p>
            </body>
            </html>";
    }

    private static string CreateOtpChangeEmail(string email, string otp)
    {
         return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Confirm your OTP change mail </h2>
                <p>Your OTP is <strong>{otp}</strong></p>
                <p>This code is valid for 15 minutes.</p>
            </body>
            </html>";
    }

    private static string CreateOtpEmailForgot(string email, string otp)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Confirm your OTP</h2>
                <p>Your OTP is <strong>{otp}</strong></p>
                <p>This code is valid for 15 minutes.</p>
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


        public async Task SendLoanConfirmationEmail(User user, Loans loan)
    {
        if (user == null || string.IsNullOrEmpty(user.Email))
        {
            _logger.LogWarning("Invalid user or email when sending loan confirmation.");
            return;
        }

        var fullName = !string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName)
            ? $"{user.FirstName} {user.LastName}"
            : user.FirstName ?? user.LastName ?? "Valued Customer";

        var subject = "✅ Loan Confirmation - TeckBank";
        var body = $@"
    <html>
    <body style='font-family: Arial, sans-serif;'>
        <h2>Loan Successfully Created</h2>
        <p>Hi {fullName},</p>
        <p>Your loan has been successfully created with the following details:</p>
        <ul>
            <li><strong>Loan Name:</strong> {loan.LoanName}</li>
            <li><strong>Loan Amount:</strong> {loan.AmountBorrowed:N0} $</li>
            <li><strong>Start Date:</strong> {loan.StartDate:dd/MM/yyyy}</li>
            <li><strong>End Date:</strong> {loan.EndDate:dd/MM/yyyy}</li>
            <li><strong>Monthly Payment:</strong> {loan.MonthlyPayment:N0} $</li>
        </ul>
        <p>Thank you for using TeckBank! If you have any questions, feel free to contact us.</p>
        <p>Best regards,<br/>TeckBank Team 🏦</p>
    </body>
    </html>";

        try
        {
            await _emailService.Send(user.Email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send loan confirmation email to {user.Email}.");
        }
    }

    public async Task SendMonthlyRemindersAsync()
    {
        var currentDate = DateTime.Now.Date;

        // Lấy tất cả khoản vay còn hiệu lực, bao gồm PaymentSchedules
        var loans = await _context.Loans
            .Where(l => l.StartDate <= currentDate && l.EndDate >= currentDate)
            .Include(l => l.PaymentSchedules)
            .ToListAsync();

        foreach (var loan in loans)
        {
            var firstReminderDate = loan.StartDate.AddDays(30);
            int monthsRemaining = (loan.EndDate - loan.StartDate).Days / 30;

            for (int i = 0; i < monthsRemaining; i++)
            {
                var reminderDate = firstReminderDate.AddDays(i * 30);

                // Nếu hôm nay là ngày nhắc nhở
                if (currentDate == reminderDate.Date)
                {
                    var user = await _context.Users.FindAsync(loan.UserId);
                    if (user == null || string.IsNullOrEmpty(user.Email)) continue;

                    foreach (var paymentSchedule in loan.PaymentSchedules)
                    {
                        if (paymentSchedule.PaymentDueDate.Date == reminderDate.Date && !paymentSchedule.IsReminderSent)
                        {
                            var subject = $"💰 Loan Payment Reminder for {paymentSchedule.PaymentDueDate:MMMM yyyy}";
                            var body = $@"
                             <p>Hi {user.FirstName ?? "Valued Customer"},</p>
<p>This is a reminder that your loan payment is due on <b>{paymentSchedule.PaymentDueDate:dd/MM/yyyy}</b>.</p>
                             <ul>
                                 <li><strong>Loan ID:</strong> #{loan.Id}</li>
                                 <li><strong>Monthly Payment:</strong> {loan.MonthlyPayment:N0} VND</li>
                             </ul>
                             <p>Please make your payment as soon as possible to avoid penalties.</p>
                             <p>Thanks,<br/>Your Loan Service Team 🏦</p>";

                            try
                            {
                                await _emailService.Send(user.Email, subject, body);
                                paymentSchedule.IsReminderSent = true; // Đánh dấu đã gửi
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Failed to send reminder email to {user.Email} for schedule {paymentSchedule.Id}.");
                            }
                        }
                    }

                    // Sau mỗi loan => save changes để đảm bảo lưu trạng thái đã gửi
                    await _context.SaveChangesAsync();
                }
            }
        }
    }


    public async Task SendTopupConfirmationEmail(User user, Topup topup)
    {
        if (user == null || string.IsNullOrEmpty(user.Email))
        {
            _logger.LogWarning("Invalid user or email when sending top-up confirmation.");
            return;
        }

        var fullName = !string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName)
            ? $"{user.FirstName} {user.LastName}"
            : user.FirstName ?? user.LastName ?? "Valued Customer";

        var subject = "💰 Top-up Confirmation - TeckBank";
        var body = $@"




<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Your topup information is incorrect.</h2>
    <p>Hi {fullName},</p>
    <p>We’ve received your top-up request with the following details:</p>
    <ul>
        <li><strong>AmountTopup:</strong> {topup.AmountTopup:N0} $</li>
        <li><strong>Description:</strong> {topup.Description}</li>
        <li><strong>Created At:</strong> {topup.CreatedAt:dd/MM/yyyy HH:mm}</li>
    </ul>
    <p>Thank you for banking with TeckBank.</p>
    <p>Best regards,<br/>TeckBank Team 🏦</p>
</body>
</html>";

        try
        {
            await _emailService.Send(user.Email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send top-up confirmation email to {user.Email}.");
        }
    }
}