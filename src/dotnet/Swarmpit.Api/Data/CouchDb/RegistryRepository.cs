using System.Text.Json;
using Swarmpit.Api.Models;

namespace Swarmpit.Api.Data.CouchDb;

public class RegistryRepository
{
    private readonly CouchDbClient _db;

    public RegistryRepository(CouchDbClient db)
    {
        _db = db;
    }

    public async Task<List<Registry>> GetAllAsync()
    {
        var docs = await _db.FindDocsAsync("registry");
        return docs.Select(MapRegistry).ToList();
    }

    public async Task<List<Registry>> GetByTypeAsync(string registryType)
    {
        var selector = new Dictionary<string, object>
        {
            ["registryType"] = new Dictionary<string, string> { ["$eq"] = registryType }
        };

        var docs = await _db.FindDocsAsync("registry", selector);
        return docs.Select(MapRegistry).ToList();
    }

    public async Task<Registry?> GetByIdAsync(string id)
    {
        var doc = await _db.GetDocAsync(id);
        return doc != null ? MapRegistry(doc) : null;
    }

    public async Task<CouchDbCreateResponse> CreateAsync(Registry registry)
    {
        return await _db.CreateDocAsync(new
        {
            type = "registry",
            name = registry.Name,
            registryType = registry.RegistryType,
            url = registry.Url,
            username = registry.Username,
            password = registry.Password,
            @public = registry.Public,
            owner = registry.Owner,
            region = registry.Region,
            accessKeyId = registry.AccessKeyId,
            accessKey = registry.AccessKey,
            spName = registry.SpName,
            spId = registry.SpId,
            spPassword = registry.SpPassword,
            token = registry.Token,
            hosted = registry.Hosted,
            gitlabUrl = registry.GitlabUrl
        });
    }

    public async Task UpdateAsync(string id, string rev, Registry registry)
    {
        await _db.UpdateDocAsync(id, rev, new
        {
            type = "registry",
            name = registry.Name,
            registryType = registry.RegistryType,
            url = registry.Url,
            username = registry.Username,
            password = registry.Password,
            @public = registry.Public,
            owner = registry.Owner,
            region = registry.Region,
            accessKeyId = registry.AccessKeyId,
            accessKey = registry.AccessKey,
            spName = registry.SpName,
            spId = registry.SpId,
            spPassword = registry.SpPassword,
            token = registry.Token,
            hosted = registry.Hosted,
            gitlabUrl = registry.GitlabUrl
        });
    }

    public async Task DeleteAsync(string id, string rev)
    {
        await _db.DeleteDocAsync(id, rev);
    }

    private static Registry MapRegistry(JsonDocument doc)
    {
        var root = doc.RootElement;
        return new Registry
        {
            Id = root.GetProperty("_id").GetString() ?? "",
            Rev = root.GetProperty("_rev").GetString() ?? "",
            Name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
            RegistryType = root.TryGetProperty("registryType", out var rt) ? rt.GetString() ?? "" : "",
            Url = root.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
            Username = root.TryGetProperty("username", out var un) ? un.GetString() : null,
            Password = root.TryGetProperty("password", out var pw) ? pw.GetString() : null,
            Public = root.TryGetProperty("public", out var pub) && pub.GetBoolean(),
            Owner = root.TryGetProperty("owner", out var o) ? o.GetString() ?? "" : "",
            Region = root.TryGetProperty("region", out var reg) ? reg.GetString() : null,
            AccessKeyId = root.TryGetProperty("accessKeyId", out var aki) ? aki.GetString() : null,
            AccessKey = root.TryGetProperty("accessKey", out var ak) ? ak.GetString() : null,
            SpName = root.TryGetProperty("spName", out var spn) ? spn.GetString() : null,
            SpId = root.TryGetProperty("spId", out var spi) ? spi.GetString() : null,
            SpPassword = root.TryGetProperty("spPassword", out var spp) ? spp.GetString() : null,
            Token = root.TryGetProperty("token", out var tok) ? tok.GetString() : null,
            Hosted = root.TryGetProperty("hosted", out var h) && h.ValueKind == JsonValueKind.True ? true :
                     root.TryGetProperty("hosted", out var h2) && h2.ValueKind == JsonValueKind.False ? false : null,
            GitlabUrl = root.TryGetProperty("gitlabUrl", out var gu) ? gu.GetString() : null
        };
    }
}
