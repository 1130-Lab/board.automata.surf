using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace board.automata.surf.services;

public sealed class SessionTokenService
{
    private readonly SessionTokenOptions _options;
    private readonly JwtSecurityTokenHandler _handler = new();

    public SessionTokenService(IOptions<SessionTokenOptions> options)
    {
        _options = options.Value;
    }

    public IssuedSessionToken IssueToken(string sessionId)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddSeconds(_options.LifetimeSeconds);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sid, sessionId),
            new Claim(JwtRegisteredClaimNames.Sub, sessionId),
            new Claim(JwtRegisteredClaimNames.Jti, jti)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials
        );

        return new IssuedSessionToken(_handler.WriteToken(token), jti, expires);
    }

    public bool TryValidate(string token, out ClaimsPrincipal? principal)
    {
        try
        {
            principal = _handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(5)
            }, out _);

            return true;
        }
        catch
        {
            principal = null;
            return false;
        }
    }

    public bool TryGetSessionId(ClaimsPrincipal principal, out string sessionId)
    {
        sessionId = principal.FindFirstValue(JwtRegisteredClaimNames.Sid)
                    ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? string.Empty;

        return !string.IsNullOrWhiteSpace(sessionId);
    }
}

public sealed class SessionTokenOptions
{
    public string Issuer { get; set; } = "board.automata.surf";
    public string Audience { get; set; } = "board.automata.surf.session";
    public string SigningKey { get; set; } = "dev-signing-key-change-this-value-32-bytes";
    public int LifetimeSeconds { get; set; } = 120;
}

public sealed record IssuedSessionToken(string Token, string Jti, DateTimeOffset ExpiresAtUtc);
