using System.Security.Cryptography;
using System.Text.Json;

namespace Swarmpit.Api.Data.CouchDb;

public class SecretRepository
{
    private readonly CouchDbClient _db;

    public SecretRepository(CouchDbClient db)
    {
        _db = db;
    }

    public async Task<string> GetOrCreateSecretAsync()
    {
        var doc = await _db.FindDocAsync("secret");
        if (doc != null)
        {
            return doc.RootElement.GetProperty("secret").GetString()!;
        }

        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await _db.CreateDocAsync(new { type = "secret", secret });
        return secret;
    }
}
