using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CP2496H07Group1.Configs.Jwt;

public class JwtHandler
{
    private readonly JwtModel _jwtModel;
    private readonly RedisService _redisService;

    public JwtHandler(IOptions<JwtModel> jwtOptions, RedisService redis)
    {
        _jwtModel = jwtOptions.Value;
        _redisService = redis;
    }

    public Task<string> GenerateTokenAdmin(Admin admin)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtModel.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, admin.Username),
            new Claim(ClaimTypes.Email, admin.Email),
            new Claim("hashed_id", HashId(admin.Id)),
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
        };
        var roleClaims = admin.Roles.Select(role => new Claim(ClaimTypes.Role, role.RoleName));
        var allClaims = claims.Concat(roleClaims);

        var token = new JwtSecurityToken(
            issuer: _jwtModel.Issuer,
            audience: _jwtModel.Audience,
            claims: allClaims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult(tokenString);
    }

    public Task<string> GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtModel.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.PhoneNumber),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("hashed_id", HashId(user.Id)),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName)
        };

        var roleClaims = user.Roles.Select(role => new Claim(ClaimTypes.Role, "User"));

        var allClaims = claims.Concat(roleClaims);

        var token = new JwtSecurityToken(
            issuer: _jwtModel.Issuer,
            audience: _jwtModel.Audience,
            claims: allClaims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult(tokenString);
    }


    public Task<string> GenerateRefreshToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtModel.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.PhoneNumber),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("hashed_id", HashId(user.Id)),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName)
        };

        var roleClaims = user.Roles.Select(role => new Claim(ClaimTypes.Role, "User"));
        var allClaims = claims.Concat(roleClaims);

        var token = new JwtSecurityToken(
            issuer: _jwtModel.Issuer,
            audience: _jwtModel.Audience,
            claims: allClaims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult(tokenString);
    }


    public string HashId(long userId)
    {
        using (var sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(userId.ToString()));
            return Convert.ToBase64String(bytes);
        }
    }

    //Tra ve userId
    public long? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal == null) return null;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? long.Parse(userIdClaim.Value) : null;
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtModel.Secret);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtModel.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtModel.Audience,
                ValidateLifetime = false
            }, out var validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}