namespace CP2496H07Group1.Configs.Jwt;

public class JwtModel
{
    public string Secret { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int ExpiresIn { get; set; } = 3600;
}