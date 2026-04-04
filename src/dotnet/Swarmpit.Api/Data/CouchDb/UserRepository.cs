using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Swarmpit.Api.Data.CouchDb;

public class UserRepository
{
    private readonly CouchDbClient _db;

    public UserRepository(CouchDbClient db)
    {
        _db = db;
    }

    public async Task<UserDoc?> GetByUsernameAsync(string username)
    {
        var selector = new Dictionary<string, object>
        {
            ["username"] = new Dictionary<string, string> { ["$eq"] = username }
        };

        var doc = await _db.FindDocAsync("user", selector);
        return doc != null ? MapUser(doc) : null;
    }

    public async Task<List<UserDoc>> GetAllAsync()
    {
        var docs = await _db.FindDocsAsync("user");
        return docs.Select(MapUser).ToList();
    }

    public async Task<string> CreateAsync(string username, string password, string role, string? email = null)
    {
        var hashed = HashPassword(password);
        var result = await _db.CreateDocAsync(new
        {
            type = "user",
            username,
            password = hashed,
            role,
            email
        });
        return result.Id;
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

public class UserDoc
{
    public string Id { get; set; } = "";
    public string Rev { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "user";
    public string? Email { get; set; }
    public ApiTokenDoc? ApiToken { get; set; }

    public bool IsAdmin => Role == "admin";
}

public class ApiTokenDoc
{
    public string? Jti { get; set; }
}
