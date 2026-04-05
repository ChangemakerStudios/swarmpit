using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarmpit.Core.Domain.Data;

namespace Swarmpit.Core.Infrastructure.CouchDb;

public class CouchDbClient(HttpClient http, ILogger<CouchDbClient> logger, IOptions<CouchDbOptions> options)
{
    private string DbPath => $"/{options.Value.DatabaseName}";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public async Task<bool> DatabaseExistsAsync()
    {
        var response = await http.SendAsync(new HttpRequestMessage(HttpMethod.Head, DbPath));
        return response.StatusCode == HttpStatusCode.OK;
    }

    public async Task CreateDatabaseAsync()
    {
        var response = await http.PutAsync(DbPath, null);
        response.EnsureSuccessStatusCode();
        logger.LogInformation("Created {DbName} database", options.Value.DatabaseName);
    }

    public async Task<JsonDocument?> GetDocAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        try
        {
            var response = await http.GetAsync($"{DbPath}/{Uri.EscapeDataString(id)}");
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to get doc {Id}", id);
            return null;
        }
    }

    public async Task<CouchDbCreateResponse> CreateDocAsync(object doc)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(doc, JsonOptions),
            new MediaTypeHeaderValue("application/json"));

        var response = await http.PostAsync(DbPath, content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CouchDbCreateResponse>(json, JsonOptions)!;
    }

    public async Task UpdateDocAsync(string id, string rev, object doc)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(doc, JsonOptions),
            new MediaTypeHeaderValue("application/json"));

        var response = await http.PutAsync($"{DbPath}/{Uri.EscapeDataString(id)}?rev={rev}", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteDocAsync(string id, string rev)
    {
        var response = await http.DeleteAsync(
            $"{DbPath}/{Uri.EscapeDataString(id)}?rev={Uri.EscapeDataString(rev)}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<JsonDocument>> FindDocsAsync(string type, object? additionalSelector = null)
    {
        var selector = new Dictionary<string, object>
        {
            ["type"] = new Dictionary<string, string> { ["$eq"] = type }
        };

        if (additionalSelector is Dictionary<string, object> extra)
        {
            foreach (var kvp in extra)
                selector[kvp.Key] = kvp.Value;
        }

        return await FindBySelectorAsync(selector);
    }

    public async Task<JsonDocument?> FindDocAsync(string type, object? additionalSelector = null)
    {
        var docs = await FindDocsAsync(type, additionalSelector);
        return docs.FirstOrDefault();
    }

    private async Task<List<JsonDocument>> FindBySelectorAsync(object selector)
    {
        var body = new { selector };
        var content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            new MediaTypeHeaderValue("application/json"));

        var response = await http.PostAsync($"{DbPath}/_find", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var docs = new List<JsonDocument>();
        if (doc.RootElement.TryGetProperty("docs", out var docsArray))
        {
            foreach (var item in docsArray.EnumerateArray())
            {
                docs.Add(JsonDocument.Parse(item.GetRawText()));
            }
        }

        return docs;
    }

    public async Task EnsureDatabaseAsync()
    {
        if (!await DatabaseExistsAsync())
        {
            await CreateDatabaseAsync();
        }
    }
}

public class CouchDbCreateResponse
{
    public bool Ok { get; set; }
    public string Id { get; set; } = "";
    public string Rev { get; set; } = "";
}
