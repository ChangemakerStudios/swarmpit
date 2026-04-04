using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Swarmpit.Api.Data.CouchDb;

namespace Swarmpit.Api.Auth;

public class JwtService
{
    private readonly SecretRepository _secretRepo;

    public JwtService(SecretRepository secretRepo)
    {
        _secretRepo = secretRepo;
    }

    public async Task<string> GenerateTokenAsync(UserDoc user, bool isApiToken = false)
    {
        var secret = await _secretRepo.GetOrCreateSecretAsync();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(PadKey(secret)));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var userClaim = JsonSerializer.Serialize(new { username = user.Username, role = user.Role });

        var claims = new List<Claim>
        {
            new("usr", userClaim),
            new("iss", isApiToken ? AppConstants.ApiIssuer : AppConstants.AppIssuer),
            new(JwtRegisteredClaimNames.Sub, user.Username)
        };

        if (isApiToken)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        }

        var expiry = isApiToken
            ? DateTime.UtcNow.AddYears(10)
            : DateTime.UtcNow.AddHours(24);

        var token = new JwtSecurityToken(
            issuer: isApiToken ? AppConstants.ApiIssuer : AppConstants.AppIssuer,
            claims: claims,
            expires: expiry,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<SecurityKey> GetSigningKeyAsync()
    {
        var secret = await _secretRepo.GetOrCreateSecretAsync();
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(PadKey(secret)));
    }

    private static string PadKey(string key)
    {
        // Ensure key is at least 32 bytes for HMAC-SHA256
        while (key.Length < 32)
            key += key;
        return key[..32];
    }
}
