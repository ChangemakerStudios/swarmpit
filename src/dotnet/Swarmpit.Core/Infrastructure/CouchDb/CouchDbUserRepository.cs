using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Swarmpit.Core.Application.Users;

namespace Swarmpit.Core.Infrastructure.CouchDb;

public class CouchDbUserRepository(CouchDbClient db) : IUserRepository
{
    public async Task<UserDoc?> GetByUsernameAsync(string username)
    {
        var selector = new Dictionary<string, object>
        {
            ["username"] = new Dictionary<string, string> { ["$eq"] = username }
        };

        var doc = await db.FindDocAsync("user", selector);
        return doc != null ? MapUser(doc) : null;
    }

    public async Task<List<UserDoc>> GetAllAsync()
    {
        var docs = await db.FindDocsAsync("user");
        return docs.Select(MapUser).ToList();
    }

    public async Task<string> CreateAsync(string username, string password, string role, string? email = null)
    {
        var hashed = HashPassword(password);
        var result = await db.CreateDocAsync(new
        {
            type = "user",
            username,
            password = hashed,
            role,
            email
        });
        return result.Id;
    }

    public async Task UpdateAsync(string username, string? password, string? role, string? email)
    {
        var user = await GetByUsernameAsync(username)
            ?? throw new InvalidOperationException($"User '{username}' not found");

        var updatedPassword = password != null ? HashPassword(password) : user.Password;
        var updatedRole = role ?? user.Role;
        var updatedEmail = email ?? user.Email;

        await db.UpdateDocAsync(user.Id, user.Rev, new
        {
            type = "user",
            username = user.Username,
            password = updatedPassword,
            role = updatedRole,
            email = updatedEmail,
            apiToken = user.ApiToken != null ? new { jti = user.ApiToken.Jti } : null
        });
    }

    public async Task DeleteAsync(string username)
    {
        var user = await GetByUsernameAsync(username)
            ?? throw new InvalidOperationException($"User '{username}' not found");

        if (user.IsAdmin)
        {
            var allUsers = await GetAllAsync();
            var adminCount = allUsers.Count(u => u.IsAdmin);
            if (adminCount <= 1)
                throw new InvalidOperationException("Cannot delete the last admin user");
        }

        await db.DeleteDocAsync(user.Id, user.Rev);
    }

    public async Task<string> GenerateApiTokenAsync(string username)
    {
        var user = await GetByUsernameAsync(username)
            ?? throw new InvalidOperationException($"User '{username}' not found");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        await db.UpdateDocAsync(user.Id, user.Rev, new
        {
            type = "user",
            username = user.Username,
            password = user.Password,
            role = user.Role,
            email = user.Email,
            apiToken = new { jti = token }
        });

        return token;
    }

    public async Task RevokeApiTokenAsync(string username)
    {
        var user = await GetByUsernameAsync(username)
            ?? throw new InvalidOperationException($"User '{username}' not found");

        await db.UpdateDocAsync(user.Id, user.Rev, new
        {
            type = "user",
            username = user.Username,
            password = user.Password,
            role = user.Role,
            email = user.Email,
            apiToken = (object?)null
        });
    }

    public async Task<bool> VerifyPasswordAsync(string username, string password)
    {
        var user = await GetByUsernameAsync(username);
        if (user == null) return false;
        return VerifyPassword(password, user.Password);
    }

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA512, 200000, 32);
        return $"pbkdf2_sha512${200000}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4) return false;

        var iterations = int.Parse(parts[1]);
        var salt = Convert.FromBase64String(parts[2]);
        var hash = Convert.FromBase64String(parts[3]);

        var computedHash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA512, iterations, hash.Length);
        return CryptographicOperations.FixedTimeEquals(hash, computedHash);
    }

    private static UserDoc MapUser(JsonDocument doc)
    {
        var root = doc.RootElement;
        return new UserDoc
        {
            Id = root.GetProperty("_id").GetString() ?? "",
            Rev = root.GetProperty("_rev").GetString() ?? "",
            Username = root.GetProperty("username").GetString() ?? "",
            Password = root.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "",
            Role = root.TryGetProperty("role", out var r) ? r.GetString() ?? "user" : "user",
            Email = root.TryGetProperty("email", out var e) ? e.GetString() : null,
            ApiToken = root.TryGetProperty("apiToken", out var at) && at.ValueKind == JsonValueKind.Object
                ? new ApiTokenDoc
                {
                    Jti = at.TryGetProperty("jti", out var jti) ? jti.GetString() : null
                }
                : null
        };
    }
}
