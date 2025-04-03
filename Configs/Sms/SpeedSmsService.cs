namespace CP2496H07Group1.Configs.Sms;

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class SpeedSmsService
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public SpeedSmsService(IConfiguration configuration)
    {
        _apiKey = configuration["SpeedSMS:ApiKey"] ?? throw new ArgumentNullException("API key không được để trống!");
        _httpClient = new HttpClient();
    }

    public async Task<bool> SendOtpAsync(string phoneNumber, string otp)
    {
        try
        {
            string url = "https://api.speedsms.vn/index.php/sms/send";
            var payload = new
            {
                to = new string[] { phoneNumber },
                content = $"Mã OTP của bạn là: {otp}",
                type = 2
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(_apiKey + ":x")));

            var response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine(responseBody); // Debug response
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi khi gửi SMS: " + ex.Message);
            return false;
        }
    }
}