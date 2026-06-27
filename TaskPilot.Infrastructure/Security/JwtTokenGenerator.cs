using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Domain.Entities;
using TaskPilot.Domain.Options;

namespace TaskPilot.Infrastructure.Security;

public class JwtTokenGenerator(
    IOptions<JwtOptions> options,
    IDateTimeProvider dateTimeProvider): IJwtTokenGenerator
{
    public AuthToken Generate(User user)
    {
        var jwtOptions = options.Value;
        var expiresAtUtc = dateTimeProvider.UtcNow.AddMinutes(jwtOptions.AccessTokenExpirationMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email)
        };
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials
        );
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthToken(accessToken, expiresAtUtc);
    }
}
