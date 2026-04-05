using System.Text.Json;

namespace Swarmpit.Api.Data.CouchDb;

public class StackFileRepository
{
    private readonly CouchDbClient _db;

    public StackFileRepository(CouchDbClient db)
    {
        _db = db;
    }

    public async Task<StackFileDoc?> GetByNameAsync(string name)
    {
        var selector = new Dictionary<string, object>
        {
            ["name"] = new Dictionary<string, string> { ["$eq"] = name }
        };

        var doc = await _db.FindDocAsync("stackfile", selector);
        return doc != null ? MapStackFile(doc) : null;
    }

    public async Task<List<StackFileDoc>> GetAllAsync()
    {
        var docs = await _db.FindDocsAsync("stackfile");
        return docs.Select(MapStackFile).ToList();
    }

    public async Task SaveAsync(string name, string compose)
    {
        var existing = await GetByNameAsync(name);

        if (existing != null)
        {
            // Move current spec to previousSpec
            await _db.UpdateDocAsync(existing.Id, existing.Rev, new
            {
                type = "stackfile",
                name,
                spec = new { compose },
                previousSpec = existing.Spec != null
                    ? new { compose = existing.Spec.Compose }
                    : null
            });
        }
        else
        {
            await _db.CreateDocAsync(new
            {
                type = "stackfile",
                name,
                spec = new { compose }
            });
        }
    }

    public async Task DeleteAsync(string name)
    {
        var existing = await GetByNameAsync(name);
        if (existing != null)
        {
            await _db.DeleteDocAsync(existing.Id, existing.Rev);
        }
    }

    private static StackFileDoc MapStackFile(JsonDocument doc)
    {
        var root = doc.RootElement;
        var result = new StackFileDoc
        {
            Id = root.GetProperty("_id").GetString() ?? "",
            Rev = root.GetProperty("_rev").GetString() ?? "",
            Name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : ""
        };

        if (root.TryGetProperty("spec", out var spec) && spec.ValueKind == JsonValueKind.Object)
        {
            result.Spec = new StackFileSpec
            {
                Compose = spec.TryGetProperty("compose", out var c) ? c.GetString() ?? "" : ""
            };
        }

        if (root.TryGetProperty("previousSpec", out var prev) && prev.ValueKind == JsonValueKind.Object)
        {
            result.PreviousSpec = new StackFileSpec
            {
                Compose = prev.TryGetProperty("compose", out var c) ? c.GetString() ?? "" : ""
            };
        }

        return result;
    }
}

public class StackFileDoc
{
    public string Id { get; set; } = "";
    public string Rev { get; set; } = "";
    public string Name { get; set; } = "";
    public StackFileSpec? Spec { get; set; }
    public StackFileSpec? PreviousSpec { get; set; }
}

public class StackFileSpec
{
    public string Compose { get; set; } = "";
}
