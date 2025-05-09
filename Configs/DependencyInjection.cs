using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Configs.Sms;
using CP2496H07Group1.Services.Account;
using CP2496H07Group1.Services.Auth;
using CP2496H07Group1.Services.Hangfire;
using CP2496H07Group1.Services.Main;
using CP2496H07Group1.Services.Package;
using Microsoft.EntityFrameworkCore;

namespace CP2496H07Group1.Configs;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDataContext>(options => options.UseSqlServer(connectionString));

        // Đăng ký các dịch vụ
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMainService, MainService>();
        services.AddScoped<IPackageService, PackageService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IHangFile, HangFile>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddSingleton<RedisService>();
        services.AddSingleton<SpeedSmsService>();
        services.AddSignalR();
            
        return services;
    }
}