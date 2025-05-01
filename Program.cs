using CP2496H07Group1.Configs;
using CP2496H07Group1.Configs.Email;
using CP2496H07Group1.Configs.Jwt;
using CP2496H07Group1.Hubs;
using CP2496H07Group1.Services.Auth;
using CP2496H07Group1.Services.Hangfire;
using Hangfire;
using Hangfire.MemoryStorage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.Configure<JwtModel>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<JwtHandler>();
var jwtSetting = builder.Configuration.GetSection("Jwt").Get<JwtModel>() ?? throw new ArgumentNullException("JWT settings cannot be null");
builder.Services.AddJwtAndGoogleAuthentication(builder.Configuration, jwtSetting);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage()
);
builder.Services.AddHangfireServer();

var app = builder.Build();

app.UseStatusCodePages(context =>
{
    var response = context.HttpContext.Response;

    if (response.StatusCode == 404 || response.StatusCode == 500 || response.StatusCode == 403 || response.StatusCode == 401 || response.StatusCode == 405 || response.StatusCode == 406)
    {
        response.Redirect("/Error");
    }

    return Task.CompletedTask;
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    using var scope = app.Services.CreateScope();
    var recurringJobs = scope.ServiceProvider
        .GetRequiredService<IRecurringJobManager>();

    // GỬI NHẮC HÀNG THÁNG (đã có)
    recurringJobs.AddOrUpdate<IAuthService>(
        "send-monthly-reminders",
        svc => svc.SendMonthlyRemindersAsync(),
        Cron.Daily());

    // XỬ LÝ KHOẢN THANH TOÁN HÀNG THÁNG (đã có)
    recurringJobs.AddOrUpdate<HangFile>(
        "process-monthly-payments",
        job => job.ProcessMonthlyPayments(),
        Cron.Minutely());

    // 🆕 TỰ ĐỘNG THANH TOÁN NỢ THẺ TÍN DỤNG – chạy mỗi đêm 02:00
    recurringJobs.AddOrUpdate<HangFile>(
        "auto-pay-credit-card-debts",
        job => job.AutoPayCreditCardDebts(),
        "0 2 * * *");            // Cron.Daily(2) cũng được

    // 🆕 TỰ ĐỘNG KHÓA THẺ QUÁ HẠN – chạy mỗi đêm 03:00
    recurringJobs.AddOrUpdate<HangFile>(
        "auto-block-credit-card",
        job => job.AutoBlockCreditCard(),
        "0 3 * * *");            // Cron.Daily(3)
});


app.UseHangfireDashboard();


app.UseCors("AllowAllOrigins");
app.MapHub<ChatHub>("/chatHub");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy();
app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();