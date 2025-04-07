using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Configs.Sms;
using CP2496H07Group1.Services.Account;
using CP2496H07Group1.Services.Auth;
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
        services.AddScoped<IAccountService, AccountService>();
        services.AddSingleton<RedisService>();
        services.AddSingleton<SpeedSmsService>();


            
        return services;
    }
}