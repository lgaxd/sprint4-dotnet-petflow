using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Domain.Entities;

namespace PetFlowAPI.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PetFlowAPI";
    public string Audience { get; set; } = "PetFlowClients";
    public int ExpirationMinutes { get; set; } = 60;
}

/// <summary>
/// Hash de senhas com BCrypt (mesmo formato dos usuários semeados no script SQL e da aplicação Java).
/// Mantém compatibilidade de leitura com o formato PBKDF2 emitido na Sprint 3.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const string LegacyPrefix = "PBKDF2-SHA256$";

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);

    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;

        try
        {
            return storedHash.StartsWith(LegacyPrefix, StringComparison.Ordinal)
                ? VerifyLegacy(password, storedHash)
                : BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    private static bool VerifyLegacy(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4) return false;

        var iterations = int.Parse(parts[1]);
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _time;

    public TokenService(IOptions<JwtOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time = time;
    }

    public AccessToken CreateToken(Tutor tutor)
    {
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, tutor.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, tutor.Id.ToString()),
            new Claim(ClaimTypes.Name, tutor.Name),
            new Claim(ClaimTypes.Email, tutor.Email),
            new Claim(ClaimTypes.Role, tutor.Role)
        };

        var expires = _time.GetUtcNow().UtcDateTime.AddMinutes(_options.ExpirationMinutes);
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, expires: expires, signingCredentials: credentials);
        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
