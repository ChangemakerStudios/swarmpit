using System.Text.Json;
using Swarmpit.Core.Application.Stacks;

namespace Swarmpit.Core.Infrastructure.CouchDb;

public class CouchDbStackFileRepository(CouchDbClient db) : IStackFileRepository
{
    public async Task<StackFileDoc?> GetByNameAsync(string name)
    {
        var selector = new Dictionary<string, object>
        {
            ["name"] = new Dictionary<string, string> { ["$eq"] = name }
        };

        var doc = await db.FindDocAsync("stackfile", selector);
        return doc != null ? MapStackFile(doc) : null;
    }

    public async Task<List<StackFileDoc>> GetAllAsync()
    {
        var docs = await db.FindDocsAsync("stackfile");
        return docs.Select(MapStackFile).ToList();
    }

    public async Task SaveAsync(string name, string compose)
    {
        var existing = await GetByNameAsync(name);

        if (existing != null)
        {
            await db.UpdateDocAsync(existing.Id, existing.Rev, new
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
            await db.CreateDocAsync(new
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
            await db.DeleteDocAsync(existing.Id, existing.Rev);
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
