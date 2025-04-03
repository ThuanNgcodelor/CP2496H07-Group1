namespace CP2496H07Group1.Configs.Email;

public interface IEmailService
{
    Task Send(string toEmail, string subject, string body);
}