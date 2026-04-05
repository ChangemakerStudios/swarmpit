namespace Swarmpit.Core.Domain.Docker;

public class SwarmConfig
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string ConfigName { get; set; } = "";
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
    public string? Data { get; set; }
}
