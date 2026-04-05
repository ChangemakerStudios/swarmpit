using System.Security.Cryptography;
using Swarmpit.Core.Application.Users;

namespace Swarmpit.Core.Infrastructure.CouchDb;

public class CouchDbSecretRepository(CouchDbClient db) : ISecretRepository
{
    public async Task<string> GetOrCreateSecretAsync()
    {
        var doc = await db.FindDocAsync("secret");
        if (doc != null)
        {
            return doc.RootElement.GetProperty("secret").GetString()!;
        }

        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await db.CreateDocAsync(new { type = "secret", secret });
        return secret;
    }
}
