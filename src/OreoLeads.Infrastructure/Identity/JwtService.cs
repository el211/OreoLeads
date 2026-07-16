using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace OreoLeads.Infrastructure.Identity;

internal sealed class JwtService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;
    private readonly int _refreshTokenDays;

    public JwtService(IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt");
        _secretKey = jwt["SecretKey"]!;
        _issuer = jwt["Issuer"]!;
        _audience = jwt["Audience"]!;
        _expiryMinutes = jwt.GetValue<int?>("ExpiryMinutes") ?? 60;
        _refreshTokenDays = jwt.GetValue<int?>("RefreshTokenDays") ?? 7;
    }

    public (string token, DateTime expiresAt) GenerateAccessToken(
        string userId,
        string email,
        string? firstName,
        string? lastName,
        Guid? organizationId,
        IEnumerable<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, $"{firstName} {lastName}".Trim()),
        };

        if (organizationId.HasValue)
            claims.Add(new Claim("organization_id", organizationId.Value.ToString()));

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public (string token, DateTime expiresAt) GenerateRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return (token, DateTime.UtcNow.AddDays(_refreshTokenDays));
    }
}
