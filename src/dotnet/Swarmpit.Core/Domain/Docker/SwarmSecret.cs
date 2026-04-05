namespace Swarmpit.Core.Domain.Docker;

public class SwarmSecret
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string SecretName { get; set; } = "";
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}
