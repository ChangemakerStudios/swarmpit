namespace Swarmpit.Core.Domain.Data;

public class CouchDbOptions
{
    public const string SectionName = "CouchDb";
    public string Url { get; set; } = "http://localhost:5984";
    public string DatabaseName { get; set; } = "swarmpit";
    public int TimeoutSeconds { get; set; } = 30;
}
